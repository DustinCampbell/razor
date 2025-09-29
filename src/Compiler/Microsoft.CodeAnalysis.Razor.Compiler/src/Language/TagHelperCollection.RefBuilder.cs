// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperCollection
{
    public delegate void BuildAction<in TState>(ref RefBuilder builder, TState state);

    public ref partial struct RefBuilder
    {
        private TagHelperDescriptor? _item;

#pragma warning disable CA2213 // Disposable fields should be disposed
        private MemoryBuilder<TagHelperDescriptor> _builder;
        private PooledHashSet<TagHelperDescriptor> _set;
#pragma warning restore CA2213

        public RefBuilder()
        {
            _builder = new(clearArray: true);
            _set = new();
        }

        public RefBuilder(int? capacity = null)
        {
            if (capacity is int value)
            {
                _builder = new(value, clearArray: true);
                _set = new(value);
            }
            else
            {
                _builder = new(clearArray: true);
                _set = new();
            }
        }

        public void Dispose()
        {
            _item = null;

            _builder.Dispose();
            _set.Dispose();
        }

        public readonly bool IsEmpty => Count == 0;

        public readonly int Count
            => _item is null
                ? _builder.Length
                : 1;

        public readonly TagHelperDescriptor this[int index]
        {
            get
            {
                ArgHelper.ThrowIfNegative(index);
                ArgHelper.ThrowIfGreaterThanOrEqual(index, Count);

                if (_item is not null && index == 0)
                {
                    return _item;
                }

                return _builder[index];
            }
        }

        public bool Add(TagHelperDescriptor item)
        {
            if (_builder.Length == 0)
            {
                // Optimized for the single item case.
                if (_item is null)
                {
                    _item = item;
                    return true;
                }

                // Transition to many items.
                _builder.Append(_item);
                _set.Add(_item);
                _item = null;
            }

            // Ensure uniqueness.
            if (_set.Add(item))
            {
                _builder.Append(item);
                return true;
            }

            return false;
        }

        public void AddRange(IEnumerable<TagHelperDescriptor> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public readonly Enumerator GetEnumerator()
            => new(this);

        public readonly TagHelperCollection ToCollection()
        {
            if (_item is not null)
            {
                return new SingleItemCollection(_item);
            }
            else if (_builder.Length == 0)
            {
                return Empty;
            }

            // We need to copy the final array out since MemoryBuilder<T>
            // uses ArrayPool<T> internally.
            var array = _builder.AsMemory().ToArray();

            return new ArrayBackedCollection(ImmutableCollectionsMarshal.AsImmutableArray(array));
        }
    }
}
