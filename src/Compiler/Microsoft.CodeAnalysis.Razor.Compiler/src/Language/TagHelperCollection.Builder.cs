// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperCollection
{
    public sealed partial class Builder : ICollection<TagHelperDescriptor>, IReadOnlyList<TagHelperDescriptor>, IDisposable
    {
        // Create new pooled builders and sets with a larger initial capacity to limit growth.
        private const int InitialCapacity = 256;

        // Builders and sets are typically large, so allow them to stay larger when returned to their pool.
        private const int MaximumObjectSize = 2048;

        private static readonly LargeBuilderPool s_builderPool = LargeBuilderPool.Default;
        private static readonly LargeSetPool s_setPool = LargeSetPool.Default;

        private readonly int? _capacity;
        private TagHelperDescriptor? _item;
        private ImmutableArray<TagHelperDescriptor>.Builder? _builder;
        private HashSet<TagHelperDescriptor>? _set;

        public Builder()
        {
            _capacity = null;
        }

        public Builder(int capacity)
        {
            _capacity = capacity;
        }

        public void Dispose()
        {
            if (_builder is { } builder)
            {
                s_builderPool.Return(builder);
                _builder = null;
            }

            if (_set is { } set)
            {
                s_setPool.Return(set);
                _set = null;
            }
        }

        [MemberNotNullWhen(true, nameof(_item))]
        private bool HasSingleItem => _item is not null;

        [MemberNotNullWhen(true, nameof(_builder), nameof(_set))]
        private bool HasBuilderAndSet => _builder is not null && _set is not null;

        public bool IsEmpty => Count == 0;

        public int Count
            => _item is null
                ? _builder?.Count ?? 0
                : 1;

        public bool IsReadOnly => false;

        public TagHelperDescriptor this[int index]
        {
            get
            {
                ArgHelper.ThrowIfNegative(index);
                ArgHelper.ThrowIfGreaterThanOrEqual(index, Count);

                if (_item is not null && index == 0)
                {
                    return _item;
                }

                Debug.Assert(_builder is not null);

                return _builder[index];
            }
        }

        public bool Add(TagHelperDescriptor item)
        {
            if (_builder is null)
            {
                // Optimized for the single item case.
                if (_item is null)
                {
                    _item = item;
                    return true;
                }

                if (_item.Equals(item))
                {
                    return false;
                }

                // Transition to using a builder and set.
                _builder = s_builderPool.Get();
                _set = s_setPool.Get();

                if (_capacity is int capacity)
                {
                    _builder.SetCapacityIfLarger(capacity);

#if NET
                    _set.EnsureCapacity(capacity);
#endif
                }

                _builder.Add(_item);
                _set.Add(_item);
                _item = null;
            }

            Debug.Assert(_set is not null);

            if (_set.Add(item))
            {
                _builder.Add(item);
                return true;
            }

            return false;
        }

        void ICollection<TagHelperDescriptor>.Add(TagHelperDescriptor item)
            => Add(item);

        public void AddRange(IEnumerable<TagHelperDescriptor> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public void Clear()
        {
            if (HasBuilderAndSet)
            {
                _builder.Clear();
                _set.Clear();
            }
            else if (HasSingleItem)
            {
                _item = null;
            }
        }

        public bool Contains(TagHelperDescriptor item)
        {
            if (HasBuilderAndSet)
            {
                return _set.Contains(item);
            }
            else if (HasSingleItem)
            {
                return _item.Equals(item);
            }

            return false;
        }

        public void CopyTo(TagHelperDescriptor[] array, int arrayIndex)
        {
            if (HasBuilderAndSet)
            {
                _builder.CopyTo(array, arrayIndex);
            }
            else if (HasSingleItem)
            {
                array[arrayIndex] = _item;
            }
        }

        public bool Remove(TagHelperDescriptor item)
        {
            if (HasBuilderAndSet)
            {
                if (_set.Remove(item))
                {
                    return _builder.Remove(item);
                }
            }
            else if (HasSingleItem)
            {
                if (_item.Equals(item))
                {
                    _item = null;
                    return true;
                }
            }

            return false;
        }

        public TagHelperCollection ToCollection()
        {
            if (Count == 0)
            {
                return EmptyCollection.Instance;
            }

            if (Count == 1)
            {
                Debug.Assert(_item is not null);
                return new SingleItemCollection(_item);
            }

            Debug.Assert(_builder is not null);
            var array = _builder.ToImmutableAndClear();

            return new ArrayBackedCollection(array);
        }

        public Enumerator GetEnumerator()
            => new(this);

        IEnumerator<TagHelperDescriptor> IEnumerable<TagHelperDescriptor>.GetEnumerator()
            => new EnumeratorImpl(this);

        IEnumerator IEnumerable.GetEnumerator()
            => new EnumeratorImpl(this);
    }
}
