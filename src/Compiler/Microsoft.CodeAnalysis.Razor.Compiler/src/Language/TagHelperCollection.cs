// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

[CollectionBuilder(typeof(TagHelperCollection), methodName: "Create")]
public abstract partial class TagHelperCollection : IEquatable<TagHelperCollection>, IReadOnlyList<TagHelperDescriptor>
{
    public static TagHelperCollection Empty => EmptyCollection.Instance;

    public abstract int Count { get; }

    public bool IsEmpty => Count == 0;

    public abstract TagHelperDescriptor this[int index] { get; }

    internal abstract Checksum Checksum { get; }

    public override bool Equals(object? obj)
        => obj is TagHelperCollection other && Equals(other);

    public bool Equals(TagHelperCollection? other)
        => other is not null &&
           Count == other.Count &&
          (ReferenceEquals(this, other) || Checksum.Equals(other.Checksum));

    public override int GetHashCode()
        => Checksum.GetHashCode();

    public Enumerator GetEnumerator()
        => new(this);

    IEnumerator<TagHelperDescriptor> IEnumerable<TagHelperDescriptor>.GetEnumerator()
        => new EnumeratorImpl(this);

    IEnumerator IEnumerable.GetEnumerator()
        => new EnumeratorImpl(this);

    public abstract int IndexOf(TagHelperDescriptor item);

    public bool Contains(TagHelperDescriptor item)
        => IndexOf(item) >= 0;

    public abstract void CopyTo(Span<TagHelperDescriptor> destination);

    public static TagHelperCollection Create(ImmutableArray<TagHelperDescriptor> array)
    {
        if (array.Length == 0)
        {
            return Empty;
        }
        else if (array.Length == 1)
        {
            return new SingleItemCollection(array[0]);
        }

        using var builder = new RefBuilder(capacity: array.Length);

        foreach (var item in array)
        {
            builder.Add(item);
        }

        if (builder.Count == array.Length)
        {
            // All items were unique, wrap the original memory.
            return new ArrayBackedCollection(array);
        }

        return builder.ToCollection();
    }

    public static TagHelperCollection Create(ReadOnlySpan<TagHelperDescriptor> span)
    {
        if (span.Length == 0)
        {
            return Empty;
        }
        else if (span.Length == 1)
        {
            return new SingleItemCollection(span[0]);
        }

        using var builder = new RefBuilder(capacity: span.Length);

        foreach (var item in span)
        {
            builder.Add(item);
        }

        return builder.ToCollection();
    }

    public static TagHelperCollection Create(IEnumerable<TagHelperDescriptor> items)
    {
        if (items.TryGetCount(out var count))
        {
            if (count == 0)
            {
                return Empty;
            }
            else if (count == 1)
            {
                using var enumerator = items.GetEnumerator();
                enumerator.MoveNext();

                return new SingleItemCollection(enumerator.Current);
            }

            return BuildCollection(items, capacity: count);
        }

        return BuildCollection(items);

        static TagHelperCollection BuildCollection(IEnumerable<TagHelperDescriptor> items, int? capacity = null)
        {
            using var builder = new RefBuilder(capacity);

            foreach (var item in items)
            {
                builder.Add(item);
            }

            return builder.ToCollection();
        }
    }
    public static TagHelperCollection Build<TState>(TState state, BuildAction<TState> action)
    {
        var builder = new RefBuilder();

        return BuildCore(ref builder, state, action);
    }

    public static TagHelperCollection Build<TState>(TState state, int capacity, BuildAction<TState> action)
    {
        var builder = new RefBuilder(capacity);

        return BuildCore(ref builder, state, action);
    }

    private static TagHelperCollection BuildCore<TState>(ref RefBuilder builder, TState state, BuildAction<TState> action)
    {
        try
        {
            action(ref builder, state);
            return builder.ToCollection();
        }
        finally
        {
            builder.Dispose();
        }
    }

    public static TagHelperCollection Merge(TagHelperCollection first, TagHelperCollection second)
    {
        // Handle empty collections
        if (first.IsEmpty)
        {
            return second;
        }

        if (second.IsEmpty)
        {
            return first;
        }

        // Check for duplicates by building a set from the first collection
        // and checking if any items from the second collection are already present
        using var firstSet = new PooledHashSet<TagHelperDescriptor>();

        foreach (var item in first)
        {
            firstSet.Add(item);
        }

        var hasDuplicates = false;

        foreach (var item in second)
        {
            if (firstSet.Contains(item))
            {
                hasDuplicates = true;
                break;
            }
        }

        if (!hasDuplicates)
        {
            // No duplicates found, create an efficient merged collection
            return new MergedTagHelperCollection(first, second);
        }

        // Duplicates found, use builder to create deduplicated collection
        using var builder = new RefBuilder(capacity: first.Count + second.Count);

        foreach (var item in first)
        {
            builder.Add(item);
        }

        foreach (var item in second)
        {
            builder.Add(item);
        }

        return builder.ToCollection();
    }

    public TagHelperCollection Where(Predicate<TagHelperDescriptor> predicate)
    {
        return Build(state: (this, predicate), capacity: Count, static (ref builder, state) =>
        {
            var (collection, predicate) = state;

            foreach (var item in collection)
            {
                if (predicate(item))
                {
                    builder.Add(item);
                }
            }
        });
    }
    private sealed class EmptyCollection : TagHelperCollection
    {
        public static readonly EmptyCollection Instance = new();

        private EmptyCollection()
        {
        }

        public override int Count => 0;

        public override TagHelperDescriptor this[int index]
            => throw new IndexOutOfRangeException();

        internal override Checksum Checksum => Checksum.Null;

        public override int IndexOf(TagHelperDescriptor item) => -1;

        public override void CopyTo(Span<TagHelperDescriptor> destination)
        {
            // Nothing to copy.
        }
    }

    private sealed class SingleItemCollection(TagHelperDescriptor item) : TagHelperCollection
    {
        private readonly TagHelperDescriptor _item = item;

        public override int Count => 1;

        public override TagHelperDescriptor this[int index]
        {
            get
            {
                ArgHelper.ThrowIfNegative(index);
                ArgHelper.ThrowIfGreaterThanOrEqual(index, Count);

                Debug.Assert(index == 0);

                return _item;
            }
        }

        internal override Checksum Checksum => _item.Checksum;

        public override int IndexOf(TagHelperDescriptor item)
            => _item.Equals(item) ? 0 : -1;

        public override void CopyTo(Span<TagHelperDescriptor> destination)
        {
            ArgHelper.ThrowIfDestinationTooShort(destination, 1);

            destination[0] = _item;
        }
    }

    private sealed class ArrayBackedCollection(ImmutableArray<TagHelperDescriptor> array) : TagHelperCollection
    {
        public override int Count => array.Length;

        public override TagHelperDescriptor this[int index]
            => index >= 0 && index < array.Length
                ? array[index]
                : throw new IndexOutOfRangeException();

        internal override Checksum Checksum
        {
            get
            {
                return field ?? InterlockedOperations.Initialize(ref field, ComputeChecksum());

                Checksum ComputeChecksum()
                {
                    var builder = new Checksum.Builder();

                    foreach (var item in array)
                    {
                        builder.AppendData(item.Checksum);
                    }

                    return builder.FreeAndGetChecksum();
                }
            }
        }

        public override int IndexOf(TagHelperDescriptor item)
            => array.IndexOf(item);

        public override void CopyTo(Span<TagHelperDescriptor> destination)
        {
            ArgHelper.ThrowIfDestinationTooShort(destination, Count);

            array.CopyTo(destination);
        }
    }

    private sealed class MergedTagHelperCollection(TagHelperCollection first, TagHelperCollection second) : TagHelperCollection
    {
        public override int Count => first.Count + second.Count;

        public override TagHelperDescriptor this[int index]
        {
            get
            {
                ArgHelper.ThrowIfNegative(index);
                ArgHelper.ThrowIfGreaterThanOrEqual(index, Count);

                if (index < first.Count)
                {
                    return first[index];
                }

                return second[index - first.Count];
            }
        }

        internal override Checksum Checksum
        {
            get
            {
                return field ?? InterlockedOperations.Initialize(ref field, ComputeChecksum());

                Checksum ComputeChecksum()
                {
                    var builder = new Checksum.Builder();

                    // Append checksums from both collections in order
                    builder.AppendData(first.Checksum);
                    builder.AppendData(second.Checksum);

                    return builder.FreeAndGetChecksum();
                }
            }
        }

        public override int IndexOf(TagHelperDescriptor item)
        {
            var firstIndex = first.IndexOf(item);
            if (firstIndex >= 0)
            {
                return firstIndex;
            }

            var secondIndex = second.IndexOf(item);
            if (secondIndex >= 0)
            {
                return first.Count + secondIndex;
            }

            return -1;
        }

        public override void CopyTo(Span<TagHelperDescriptor> destination)
        {
            ArgHelper.ThrowIfDestinationTooShort(destination, Count);

            first.CopyTo(destination);
            second.CopyTo(destination[first.Count..]);
        }
    }
}
