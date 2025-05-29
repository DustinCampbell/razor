// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

/// <summary>
///  Wraps a pooled <see cref="HashSet{T}"/> but doesn't allocate it until
///  it's needed. Note: Dispose this to ensure that the pooled set is returned
///  to the pool.
///
///  There is significant effort to avoid retrieving the <see cref="HashSet{T}"/>.
///  For very small sets of length 2 or less, the elements will be stored inline.
///  If the set grows larger than 2 elements, a pooled set will be employed.
/// </summary>
[NonCopyable]
[CollectionBuilder(typeof(PooledHashSet), nameof(PooledHashSet.Create))]
internal partial struct PooledHashSet<T>(ObjectPool<HashSet<T>>? pool, int? capacity, IEqualityComparer<T>? comparer = null) : IDisposable
{
    /// <summary>
    ///  The number of items that can be stored inline.
    /// </summary>
    private const int InlineCapacity = 2;

    private readonly ObjectPool<HashSet<T>> _pool = pool ?? HashSetPool<T>.Default;
    private readonly int? _capacity = capacity;
    private readonly IEqualityComparer<T> _comparer = comparer ?? EqualityComparer<T>.Default;
    private HashSet<T>? _set = null;

    // Inline storage for small sets
    private T _element0 = default!;
    private T _element1 = default!;
    private int _inlineCount = 0;

    public PooledHashSet()
        : this(pool: null, capacity: null, comparer: null)
    {
    }

    public PooledHashSet(ObjectPool<HashSet<T>> pool)
        : this(pool, capacity: null, comparer: null)
    {
    }

    public PooledHashSet(int capacity)
        : this(pool: null, capacity, comparer: null)
    {
    }

    public void Dispose()
    {
        if (_set is { } set)
        {
            _pool.Return(set);
            _set = null;
        }

        _inlineCount = 0;
        _element0 = default!;
        _element1 = default!;
    }

    private readonly bool TryGetSet([NotNullWhen(true)] out HashSet<T>? set)
    {
        set = _set;
        return set is not null;
    }

    public readonly int Count
        => _set?.Count ?? _inlineCount;

    public bool Add(T item)
    {
        if (TryGetSet(out var set))
        {
            return set.Add(item);
        }

        // Check if item already exists in inline storage
        for (var i = 0; i < _inlineCount; i++)
        {
            if (_comparer.Equals(GetInlineElement(i), item))
            {
                return false;
            }
        }

        // If we have room, add to inline storage
        if (_inlineCount < InlineCapacity)
        {
            SetInlineElement(_inlineCount++, item);
            return true;
        }

        // Otherwise, move to HashSet
        MoveInlineItemsToSet();
        return _set.Add(item);
    }

    public bool Remove(T item)
    {
        if (TryGetSet(out var set))
        {
            return set.Remove(item);
        }

        if (_inlineCount == 0)
        {
            return false;
        }

        Debug.Assert(_inlineCount is >= 1 and <= InlineCapacity);

        int? indexToClear = null;

        switch (_inlineCount)
        {
            case 1:
                if (_comparer.Equals(_element0, item))
                {
                    indexToClear = 0;
                }

                break;

            case 2:
                if (_comparer.Equals(_element0, item))
                {
                    _element0 = _element1;
                    indexToClear = 1;
                }
                else if (_comparer.Equals(_element1, item))
                {
                    indexToClear = 1;
                }

                break;
        }

        if (indexToClear is int index)
        {
            ClearInlineElement(index);
            _inlineCount--;
            return true;
        }

        return false;
    }

    public readonly bool Contains(T item)
    {
        if (TryGetSet(out var set))
        {
            return set.Contains(item);
        }

        return _inlineCount switch
        {
            0 => false,
            1 => _comparer.Equals(_element0, item),
            2 => _comparer.Equals(_element0, item) || _comparer.Equals(_element1, item),
            _ => Assumed.Unreachable<bool>()
        };
    }

    public readonly T[] ToArray()
    {
        if (TryGetSet(out var set))
        {
            var count = set.Count;

            if (count > 0)
            {
                var array = new T[count];
                set.CopyTo(array);

                return array;
            }

            return [];
        }

        return _inlineCount switch
        {
            0 => [],
            1 => [_element0],
            2 => [_element0, _element1],
            _ => Assumed.Unreachable<T[]>()
        };
    }

    public readonly ImmutableArray<T> ToImmutableArray()
    {
        var array = ToArray();

        return ImmutableCollectionsMarshal.AsImmutableArray(array);
    }

    public readonly ImmutableArray<T> OrderByAsArray<TKey>(Func<T, TKey> keySelector)
    {
        if (TryGetSet(out var set))
        {
            return set.OrderByAsArray(keySelector);
        }

        return _inlineCount switch
        {
            0 => [],
            1 => [_element0],
            2 => GetOrderedPair(_element0, _element1, keySelector),
            _ => Assumed.Unreachable<ImmutableArray<T>>()
        };

        static ImmutableArray<T> GetOrderedPair(T element0, T element1, Func<T, TKey> keySelector)
        {
            return Comparer<TKey>.Default.Compare(keySelector(element0), keySelector(element1)) > 0
                ? [element1, element0]
                : [element0, element1];
        }
    }

    public void UnionWith(IEnumerable<T>? other)
    {
        if (other == null)
        {
            return;
        }

        // If we're already using a HashSet, delegate to it
        if (TryGetSet(out var set))
        {
            set.UnionWith(other);
            return;
        }

        // If adding all items would exceed inline capacity, move to HashSet immediately
        if (other.TryGetCount(out var count) &&
            _inlineCount + count > InlineCapacity)
        {
            MoveInlineItemsToSet();
            _set.UnionWith(other);
            return;
        }

        // Otherwise, add each item to inline storage
        foreach (var item in other)
        {
            Add(item);
        }
    }

    public readonly Enumerator GetEnumerator()
        => new(in this);

    [MemberNotNull(nameof(_set))]
    private void MoveInlineItemsToSet()
    {
        _set = _pool.Get();

#if NET
        // Calculate expected capacity - use either the explicit capacity or the inline count,
        // possibly with some extra space for growth
        var initialCapacity = _capacity ?? Math.Max(_inlineCount, InlineCapacity) * 2;
        if (initialCapacity > 0)
        {
            _set.EnsureCapacity(initialCapacity);
        }
#endif

        for (var i = 0; i < _inlineCount; i++)
        {
            _set.Add(GetInlineElement(i));
            ClearInlineElement(i);
        }

        _inlineCount = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly T GetInlineElement(int index)
    {
        return index switch
        {
            0 => _element0,
            1 => _element1,
            _ => ThrowIndexOutOfRangeException()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetInlineElement(int index, T value)
    {
        switch (index)
        {
            case 0:
                _element0 = value;
                break;

            case 1:
                _element1 = value;
                break;

            default:
                Assumed.Unreachable();
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ClearInlineElement(int index)
    {
        Debug.Assert(_inlineCount <= InlineCapacity);

        // Clearing out an item makes it potentially available for garbage collection.
        // Note: On .NET Core, we can be a bit more judicious and only zero-out
        // fields that contain references to heap-allocated objects.

#if NETCOREAPP
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
#endif
        {
            SetInlineElement(index, default!);
        }
    }

    [DoesNotReturn]
    private static T ThrowIndexOutOfRangeException()
    {
        throw new IndexOutOfRangeException();
    }
}
