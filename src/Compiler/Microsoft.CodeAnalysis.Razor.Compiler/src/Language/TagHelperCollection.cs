// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language;

[CollectionBuilder(typeof(TagHelperCollection), methodName: "Create")]
public abstract partial class TagHelperCollection : IEquatable<TagHelperCollection>, IReadOnlyList<TagHelperDescriptor>
{
    // Create new pooled builders and sets with a larger initial capacity to limit growth.
    private const int InitialCapacity = 256;

    // Builders and sets are typically large, so allow them to stay larger when returned to their pool.
    private const int MaximumObjectSize = 2048;

    private static readonly ArrayBuilderPool<TagHelperDescriptor> s_arrayBuilderPool =
        ArrayBuilderPool<TagHelperDescriptor>.Create(InitialCapacity, MaximumObjectSize);

    private static readonly HashSetPool<TagHelperDescriptor> s_setPool =
        HashSetPool<TagHelperDescriptor>.Create(maximumObjectSize: MaximumObjectSize);

    public static TagHelperCollection Empty => EmptyCollection.Instance;

    public abstract int Count { get; }

    public bool IsEmpty => Count == 0;

    public abstract TagHelperDescriptor this[int index] { get; }

    public override bool Equals(object? obj)
        => obj is TagHelperCollection other &&
        Equals(other);

    public bool Equals(TagHelperCollection? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (Count != other.Count)
        {
            return false;
        }

        // FAST PATH: If both collections are the same concrete type, use optimized comparison
        if (GetType() == other.GetType())
        {
            return FastEquals(other);
        }

        // SLOW PATH: Fall back to checksum comparison for large collections of different types
        var thisEnumerator = GetEnumerator();
        var otherEnumerator = other.GetEnumerator();

        while (thisEnumerator.MoveNext())
        {
            if (!otherEnumerator.MoveNext())
            {
                return false; // Other collection ended prematurely
            }

            if (!thisEnumerator.Current.Equals(otherEnumerator.Current))
            {
                return false; // Elements don't match
            }
        }

        // Ensure the other enumerator is also exhausted
        return !otherEnumerator.MoveNext();
    }

    // Add virtual method to allow optimized equality in concrete types
    protected virtual bool FastEquals(TagHelperCollection other) => false;

    public abstract override int GetHashCode();

    // Virtual method each collection overrides with its optimal enumeration logic
    private protected virtual bool TryMoveNext(ref EnumerationState state, [NotNullWhen(true)] out TagHelperDescriptor? current)
    {
        // Default implementation - just use indexer
        state.Index++;
        if (state.Index < Count)
        {
            current = this[state.Index];
            return true;
        }

        current = default!;
        return false;
    }

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

        // Build segments of unique consecutive elements
        using var segments = new PooledArrayBuilder<ReadOnlyMemory<TagHelperDescriptor>>();
        using var seenItems = new PooledHashSet<TagHelperDescriptor>(s_setPool);

        var segmentStart = 0;
        var arrayMemory = array.AsMemory();

        for (var i = 0; i < array.Length; i++)
        {
            var item = array[i];

            if (seenItems.Add(item))
            {
                // Item is unique, continue building current segment  
                continue;
            }

            // Found duplicate - close current segment if it has items
            if (i > segmentStart)
            {
                segments.Add(arrayMemory[segmentStart..i]);
            }

            // Start new segment after this duplicate
            segmentStart = i + 1;
        }

        // Close final segment
        if (segmentStart < array.Length)
        {
            segments.Add(arrayMemory[segmentStart..]);
        }

        if (seenItems.Count == array.Length)
        {
            // No duplicates found - use original array
            return new ArrayBackedCollection(arrayMemory);
        }

        if (segments.Count == 0)
        {
            // All duplicates
            return Empty;
        }

        if (segments.Count == 1)
        {
            // Single contiguous segment - use optimized single-segment approach
            return new ArrayBackedCollection(segments[0]);
        }

        // Multiple segments - use multi-array approach
        return new MultiArrayBackedCollection(segments.ToArrayAndClear());
    }

    [OverloadResolutionPriority(1)]
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
        using var firstSet = new PooledHashSet<TagHelperDescriptor>(s_setPool);

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
            return new TwoItemMergedCollection(first, second);
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

    public static TagHelperCollection Merge(ImmutableArray<TagHelperCollection> collections)
        => Merge(collections.AsSpan());

    public static TagHelperCollection Merge(ReadOnlySpan<TagHelperCollection> collections)
    {
        // Handle empty input
        if (collections.Length == 0)
        {
            return Empty;
        }

        // Handle single collection
        if (collections.Length == 1)
        {
            return collections[0];
        }

        // Handle two collections
        if (collections.Length == 2)
        {
            return Merge(collections[0], collections[1]);
        }

        // Filter out empty collections and calculate total capacity
        using var nonEmptyCollections = new PooledArrayBuilder<TagHelperCollection>();
        var totalCapacity = 0;

        foreach (var collection in collections)
        {
            if (!collection.IsEmpty)
            {
                nonEmptyCollections.Add(collection);
                totalCapacity += collection.Count;
            }
        }

        // After filtering, check again for simple cases
        if (nonEmptyCollections.Count == 0)
        {
            return Empty;
        }

        if (nonEmptyCollections.Count == 1)
        {
            return nonEmptyCollections[0];
        }

        if (nonEmptyCollections.Count == 2)
        {
            return Merge(nonEmptyCollections[0], nonEmptyCollections[1]);
        }

        // Check if there are any duplicates across all collections
        using var allItemsSet = new PooledHashSet<TagHelperDescriptor>(s_setPool);
        var hasDuplicates = false;

        foreach (var collection in nonEmptyCollections)
        {
            foreach (var item in collection)
            {
                if (!allItemsSet.Add(item))
                {
                    hasDuplicates = true;
                    break;
                }
            }

            if (hasDuplicates)
            {
                break;
            }
        }

        if (!hasDuplicates)
        {
            // No duplicates found across all collections
            // Use the efficient merged collection
            return new MergedCollection(nonEmptyCollections.ToImmutableAndClear());
        }

        // Duplicates found, use builder to create deduplicated collection
        using var builder = new RefBuilder(capacity: totalCapacity);

        foreach (var collection in nonEmptyCollections)
        {
            foreach (var item in collection)
            {
                builder.Add(item);
            }
        }

        return builder.ToCollection();
    }

    public static TagHelperCollection Merge(IEnumerable<TagHelperCollection> collections)
    {
        if (collections.TryGetCount(out var count))
        {
            if (count == 0)
            {
                return Empty;
            }

            if (count == 1)
            {
                using var enumerator = collections.GetEnumerator();
                Assumed.True(enumerator.MoveNext());

                return enumerator.Current;
            }

            if (count == 2)
            {
                using var enumerator = collections.GetEnumerator();

                Assumed.True(enumerator.MoveNext());
                var first = enumerator.Current;

                Assumed.True(enumerator.MoveNext());
                var second = enumerator.Current;

                return Merge(first, second);
            }

            var array = new TagHelperCollection[count];
            collections.CopyTo(array);

            return Merge(array);
        }

        // Fallback for arbitrary IEnumerable
        using var builder = new PooledArrayBuilder<TagHelperCollection>();

        foreach (var item in collections)
        {
            builder.Add(item);
        }

        return Merge(builder.ToArrayAndClear());
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

        public override int GetHashCode() => 0;

        public override int IndexOf(TagHelperDescriptor item) => -1;

        public override void CopyTo(Span<TagHelperDescriptor> destination)
        {
            // Nothing to copy.
        }

        private protected override bool TryMoveNext(ref EnumerationState state, [NotNullWhen(true)] out TagHelperDescriptor? current)
        {
            current = null;
            return false;
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

        public override int GetHashCode()
            => _item.GetHashCode();

        public override int IndexOf(TagHelperDescriptor item)
            => _item.Equals(item) ? 0 : -1;

        public override void CopyTo(Span<TagHelperDescriptor> destination)
        {
            ArgHelper.ThrowIfDestinationTooShort(destination, 1);

            destination[0] = _item;
        }

        private protected override bool TryMoveNext(ref EnumerationState state, [NotNullWhen(true)] out TagHelperDescriptor? current)
        {
            state.Index++;

            if (state.Index == 0)
            {
                current = _item;
                return true;
            }

            current = null;
            return false;
        }
    }

    private abstract class NonTrivialCollection : TagHelperCollection
    {
        private int? _hashCode;

        public override int GetHashCode()
        {
            return _hashCode ??= ComputeHashCode();
        }

        protected abstract int ComputeHashCode();
    }

    private sealed class ArrayBackedCollection : NonTrivialCollection
    {
        private readonly ReadOnlyMemory<TagHelperDescriptor> _array;
        private readonly ChecksumLookupTable? _lookupTable;

        public ArrayBackedCollection(ReadOnlyMemory<TagHelperDescriptor> array)
        {
            _array = array;

            // Only build lookup table for collections large enough to benefit
            if (array.Length > 8)
            {
                _lookupTable = new ChecksumLookupTable(array);
            }
        }

        public override int Count => _array.Length;

        public override TagHelperDescriptor this[int index]
            => index >= 0 && index < _array.Length
                ? _array.Span[index]
                : throw new IndexOutOfRangeException();

        protected override bool FastEquals(TagHelperCollection other)
        {
            return other is ArrayBackedCollection otherArray &&
                   _array.Span.SequenceEqual(otherArray._array.Span);
        }

        protected override int ComputeHashCode()
        {
            var hash = HashCodeCombiner.Start();

            foreach (var item in _array.Span)
            {
                hash.Add(item);
            }

            return hash.CombinedHash;
        }

        public override int IndexOf(TagHelperDescriptor item)
        {
            // Use fast lookup table for large collections
            if (_lookupTable.HasValue)
            {
                return _lookupTable.Value.IndexOf(item);
            }

            // Linear search for small collections (still faster than hash overhead)
            return _array.Span.IndexOf(item);
        }

        public override void CopyTo(Span<TagHelperDescriptor> destination)
        {
            ArgHelper.ThrowIfDestinationTooShort(destination, Count);

            _array.Span.CopyTo(destination);
        }

        private protected override bool TryMoveNext(ref EnumerationState state, [NotNullWhen(true)] out TagHelperDescriptor? current)
        {
            state.Index++;

            if (state.Index < _array.Length)
            {
                current = _array.Span[state.Index];
                return true;
            }

            current = null;
            return false;
        }
    }

    private sealed class MultiArrayBackedCollection : NonTrivialCollection
    {
        private readonly ReadOnlyMemory<TagHelperDescriptor>[] _segments;
        private readonly int[] _segmentStartIndices; // Pre-calculated for O(1) indexing
        private readonly int _count;

        public MultiArrayBackedCollection(ReadOnlyMemory<TagHelperDescriptor>[] segments)
        {
            _segments = segments;

            // Pre-calculate segment boundaries for efficient indexing
            _segmentStartIndices = new int[segments.Length];
            var totalCount = 0;

            for (var i = 0; i < segments.Length; i++)
            {
                _segmentStartIndices[i] = totalCount;
                totalCount += segments[i].Length;
            }

            _count = totalCount;
        }

        public override int Count => _count;

        public override TagHelperDescriptor this[int index]
        {
            get
            {
                ArgHelper.ThrowIfNegative(index);
                ArgHelper.ThrowIfGreaterThanOrEqual(index, Count);

                // Binary search to find the segment containing this index
                var segmentIndex = FindSegmentIndex(index);
                var localIndex = index - _segmentStartIndices[segmentIndex];

                return _segments[segmentIndex].Span[localIndex];
            }
        }

        public override void CopyTo(Span<TagHelperDescriptor> destination)
        {
            ArgHelper.ThrowIfDestinationTooShort(destination, Count);

            var currentOffset = 0;
            foreach (var segment in _segments)
            {
                segment.Span.CopyTo(destination[currentOffset..]);
                currentOffset += segment.Length;
            }
        }

        private int FindSegmentIndex(int globalIndex)
        {
            var searchResult = _segmentStartIndices.BinarySearch(globalIndex);

            if (searchResult >= 0)
            {
                return searchResult;
            }

            var insertionPoint = ~searchResult;
            return insertionPoint - 1;
        }

        protected override int ComputeHashCode()
        {
            var hash = HashCodeCombiner.Start();

            foreach (var segment in _segments)
            {
                foreach (var item in segment.Span)
                {
                    hash.Add(item);
                }
            }

            return hash.CombinedHash;
        }

        public override int IndexOf(TagHelperDescriptor item)
        {
            var currentOffset = 0;

            foreach (var segment in _segments)
            {
                var localIndex = segment.Span.IndexOf(item);
                if (localIndex >= 0)
                {
                    return currentOffset + localIndex;
                }

                currentOffset += segment.Length;
            }

            return -1;
        }

        private protected override bool TryMoveNext(ref EnumerationState state, [NotNullWhen(true)] out TagHelperDescriptor? current)
        {
            state.Index++;

            // Check if we're still in the current segment
            if (state.SegmentIndex < _segments.Length)
            {
                var currentSegment = _segments[state.SegmentIndex];
                state.LocalIndex++;

                if (state.LocalIndex < currentSegment.Length)
                {
                    current = currentSegment.Span[state.LocalIndex];
                    return true;
                }

                // Move to next segment
                state.SegmentIndex++;
                state.LocalIndex = -1; // Will be incremented to 0 on recursive call
                state.Offset += currentSegment.Length;

                // Try again with next segment
                return TryMoveNext(ref state, out current);
            }

            current = null;
            return false;
        }
    }

    private sealed class TwoItemMergedCollection(TagHelperCollection first, TagHelperCollection second) : NonTrivialCollection
    {
        private readonly TagHelperCollection _first = first;
        private readonly TagHelperCollection _second = second;

        public override int Count => _first.Count + _second.Count;

        public override TagHelperDescriptor this[int index]
        {
            get
            {
                ArgHelper.ThrowIfNegative(index);
                ArgHelper.ThrowIfGreaterThanOrEqual(index, Count);

                if (index < _first.Count)
                {
                    return _first[index];
                }

                return _second[index - _first.Count];
            }
        }

        protected override bool FastEquals(TagHelperCollection other)
        {
            return other is TwoItemMergedCollection otherMerged &&
                   _first.Equals(otherMerged._first) &&
                   _second.Equals(otherMerged._second);
        }

        protected override int ComputeHashCode()
        {
            var hash = HashCodeCombiner.Start();

            foreach (var item in _first)
            {
                hash.Add(item);
            }

            foreach (var item in _second)
            {
                hash.Add(item);
            }

            return hash.CombinedHash;
        }

        public override int IndexOf(TagHelperDescriptor item)
        {
            var firstIndex = _first.IndexOf(item);
            if (firstIndex >= 0)
            {
                return firstIndex;
            }

            var secondIndex = _second.IndexOf(item);
            if (secondIndex >= 0)
            {
                return _first.Count + secondIndex;
            }

            return -1;
        }

        public override void CopyTo(Span<TagHelperDescriptor> destination)
        {
            ArgHelper.ThrowIfDestinationTooShort(destination, Count);

            _first.CopyTo(destination);
            _second.CopyTo(destination[_first.Count..]);
        }

        private protected override bool TryMoveNext(ref EnumerationState state, [NotNullWhen(true)] out TagHelperDescriptor? current)
        {
            state.Index++;

            if (state.SegmentIndex == 0)
            {
                // Still in first collection
                if (state.Index < _first.Count)
                {
                    current = _first[state.Index];
                    return true;
                }

                // Move to second collection
                state.SegmentIndex = 1;
                state.LocalIndex = -1; // Reset for second collection
            }

            if (state.SegmentIndex == 1)
            {
                var secondIndex = state.Index - _first.Count;
                if (secondIndex < _second.Count)
                {
                    current = _second[secondIndex];
                    return true;
                }
            }

            current = null;
            return false;
        }
    }

    private sealed class MergedCollection : NonTrivialCollection
    {
        private readonly ImmutableArray<TagHelperCollection> _collections;
        private readonly int[] _startIndices;

        public MergedCollection(ImmutableArray<TagHelperCollection> collections)
        {
            Debug.Assert(collections.Length > 0);
            Debug.Assert(collections.All(static c => !c.IsEmpty));

            _collections = collections;

            // Pre-calculate start indices for efficient indexer access
            var startIndices = new int[collections.Length];
            var totalCount = 0;

            for (var i = 0; i < collections.Length; i++)
            {
                startIndices[i] = totalCount;
                totalCount += collections[i].Count;
            }

            _startIndices = startIndices;
            Count = totalCount;
        }

        public override int Count { get; }

        public override TagHelperDescriptor this[int index]
        {
            get
            {
                ArgHelper.ThrowIfNegative(index);
                ArgHelper.ThrowIfGreaterThanOrEqual(index, Count);

                // Binary search to find the collection that contains this index
                var collectionIndex = FindCollectionIndex(index);
                var localIndex = index - _startIndices[collectionIndex];

                return _collections[collectionIndex][localIndex];
            }
        }

        protected override bool FastEquals(TagHelperCollection other)
        {
            return other is MergedCollection otherMerged &&
                   _collections.SequenceEqual(otherMerged._collections);
        }

        protected override int ComputeHashCode()
        {
            var hash = HashCodeCombiner.Start();

            foreach (var collection in _collections)
            {
                foreach (var item in collection)
                {
                    hash.Add(item);
                }
            }

            return hash.CombinedHash;
        }

        public override int IndexOf(TagHelperDescriptor item)
        {
            var currentOffset = 0;

            foreach (var collection in _collections)
            {
                var localIndex = collection.IndexOf(item);
                if (localIndex >= 0)
                {
                    return currentOffset + localIndex;
                }

                currentOffset += collection.Count;
            }

            return -1;
        }

        public override void CopyTo(Span<TagHelperDescriptor> destination)
        {
            ArgHelper.ThrowIfDestinationTooShort(destination, Count);

            var currentOffset = 0;

            foreach (var collection in _collections)
            {
                collection.CopyTo(destination[currentOffset..]);
                currentOffset += collection.Count;
            }
        }

        private protected override bool TryMoveNext(ref EnumerationState state, [NotNullWhen(true)] out TagHelperDescriptor? current)
        {
            state.Index++;

            while (state.SegmentIndex < _collections.Length)
            {
                var currentCollection = _collections[state.SegmentIndex];
                var localIndex = state.Index - state.Offset;

                if (localIndex < currentCollection.Count)
                {
                    current = currentCollection[localIndex];
                    return true;
                }

                // Move to next collection
                state.Offset += currentCollection.Count;
                state.SegmentIndex++;
            }

            current = null;
            return false;
        }

        private int FindCollectionIndex(int globalIndex)
        {
            // Use BCL's binary search to find the insertion point
            // We want to find the largest start index that is <= globalIndex
            var searchResult = _startIndices.BinarySearch(globalIndex);

            if (searchResult >= 0)
            {
                // Exact match found - this is the collection index
                return searchResult;
            }
            else
            {
                // No exact match - BinarySearch returns the bitwise complement of the next larger element
                // We want the element just before that (the largest element <= globalIndex)
                var insertionPoint = ~searchResult;
                return insertionPoint - 1;
            }
        }
    }

    // Add this as a nested class in TagHelperCollection
    private readonly struct ChecksumLookupTable
    {
        private readonly (Checksum checksum, int index)[] _entries;
        private readonly int _mask;

        public ChecksumLookupTable(ReadOnlyMemory<TagHelperDescriptor> items)
        {
            // Use power-of-2 sizing for fast modulo via bitwise AND
            var size = GetNextPowerOfTwo(Math.Max(4, items.Length * 2)); // 2x load factor
            _mask = size - 1;
            _entries = new (Checksum, int)[size];

            var span = items.Span;

            // Build the hash table
            for (var i = 0; i < span.Length; i++)
            {
                var checksum = span[i].Checksum;
                var bucket = GetBucket(checksum);

                // Linear probing for collision resolution
                while (_entries[bucket].checksum != null)
                {
                    bucket = (bucket + 1) & _mask;
                }

                _entries[bucket] = (checksum, i);
            }
        }

        public int IndexOf(TagHelperDescriptor item)
        {
            var targetChecksum = item.Checksum;
            var bucket = GetBucket(targetChecksum);

            // Linear probing to find the item
            while (_entries[bucket].checksum != default)
            {
                if (_entries[bucket].checksum.Equals(targetChecksum))
                {
                    return _entries[bucket].index;
                }
                bucket = (bucket + 1) & _mask;
            }

            return -1; // Not found
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly int GetBucket(Checksum checksum)
            => checksum.GetHashCode() & _mask;

        private static int GetNextPowerOfTwo(int value)
        {
            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            return value + 1;
        }
    }
}
