// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

/// <summary>
///  This is used to represent content in Razor code generation. It can be a single string,
///  a <see cref="ReadOnlyMemory{T}"/>, or an array of <see cref="Content"/> parts.
/// </summary>
/// <remarks>
///  This can be used to avoid allocations caused by string concatenation.
/// </remarks>
public readonly partial record struct Content : IEnumerable<char>
{
    public static Content Empty => default;

    // If _parts is null, then _value is the only content.
    private readonly ReadOnlyMemory<char> _value;

    // Can be a Content[], a ReadOnlyMemory<char>[], or a string[].
    // If this is set, _value is empty.
    private readonly Array? _parts;

    private readonly int _partsLength;

    public int Length
        => HasParts ? _partsLength : _value.Length;

    public bool IsEmpty
        => Length == 0;

    public bool HasValue
        => _parts is null;

    public ReadOnlyMemory<char> Value
        => HasValue ? _value : ThrowHelper.ThrowInvalidOperationException<ReadOnlyMemory<char>>("Content does not have a value.");

    [MemberNotNullWhen(true, nameof(_parts))]
    public bool HasParts
        => _parts is not null;

    public Content(Content value)
    {
        _value = value._value;
        _parts = value._parts;
        _partsLength = value._partsLength;
    }

    public Content(ReadOnlyMemory<char> value)
    {
        _value = value;
        _parts = null;
        _partsLength = 0;
    }

    public Content(string? value)
    {
        _value = value.AsMemory();
        _parts = null;
        _partsLength = 0;
    }

    public Content(ImmutableArray<Content> parts)
        : this(ImmutableCollectionsMarshal.AsArray(parts).AssumeNotNull())
    {
    }

    private Content(Content[] parts)
    {
        Debug.Assert(parts is not null);

        (_value, _parts, _partsLength) = parts switch
        {
            [] => (ReadOnlyMemory<char>.Empty, null, 0),
            [var part] => (part._value, part._parts, part._partsLength),
            _ => (ReadOnlyMemory<char>.Empty, parts, ComputeLength(parts))
        };

        static int ComputeLength(Content[] parts)
        {
            var length = 0;

            foreach (var part in parts)
            {
                length += part.Length;
            }

            return length;
        }
    }

    public Content(ImmutableArray<ReadOnlyMemory<char>> parts)
    {
        var array = ImmutableCollectionsMarshal.AsArray(parts);

        Debug.Assert(array is not null);

        (_value, _parts, _partsLength) = array switch
        {
            [] => (ReadOnlyMemory<char>.Empty, null, 0),
            [var part] => (part, null, 0),
            _ => (ReadOnlyMemory<char>.Empty, array, ComputeLength(array))
        };

        static int ComputeLength(ReadOnlyMemory<char>[] parts)
        {
            var length = 0;

            foreach (var part in parts)
            {
                length += part.Length;
            }

            return length;
        }
    }

    public Content(ImmutableArray<string> parts)
    {
        var array = ImmutableCollectionsMarshal.AsArray(parts);

        Debug.Assert(array is not null);

        (_value, _parts, _partsLength) = array switch
        {
            [] => (ReadOnlyMemory<char>.Empty, null, 0),
            [var part] => (part.AsMemory(), null, 0),
            _ => (ReadOnlyMemory<char>.Empty, array, ComputeLength(array))
        };

        static int ComputeLength(string[] parts)
        {
            var length = 0;

            foreach (var part in parts)
            {
                length += part.AsSpan().Length;
            }

            return length;
        }
    }

    public Content(ref ContentInterpolatedStringHandler handler)
        : this(handler.ToArray())
    {
    }

    public static bool IsNullOrEmpty(Content? content)
        => content is not Content value || value.Length == 0;

    public static bool IsNullOrWhiteSpace(Content? content)
        => content is not Content value || value.IsWhiteSpace();

    public bool IsWhiteSpace()
    {
        if (IsEmpty)
        {
            return true;
        }

        switch (_parts)
        {
            case null:
                return _value.Span.IsWhiteSpace();

            case Content[] contentParts:
                foreach (var part in contentParts)
                {
                    if (!part.IsWhiteSpace())
                    {
                        return false;
                    }
                }

                break;

            case ReadOnlyMemory<char>[] memoryParts:
                foreach (var part in memoryParts)
                {
                    if (!part.Span.IsWhiteSpace())
                    {
                        return false;
                    }
                }

                break;

            case string[] stringParts:
                foreach (var part in stringParts)
                {
                    if (!part.AsSpan().IsWhiteSpace())
                    {
                        return false;
                    }
                }

                break;

            default:
                Assumed.Unreachable();
                break;
        }

        return true;
    }

    public void WriteTo(CodeWriter writer)
    {
        switch (_parts)
        {
            // If _parts is null, then _value is the only content.
            case null:
                if (!_value.IsEmpty)
                {
                    writer.Write(_value);
                }

                break;

            case Content[] contentParts:
                foreach (var part in contentParts)
                {
                    if (!part.IsEmpty)
                    {
                        part.WriteTo(writer);
                    }
                }

                break;

            case ReadOnlyMemory<char>[] memoryParts:
                foreach (var part in memoryParts)
                {
                    if (!part.IsEmpty)
                    {
                        writer.Write(part);
                    }
                }

                break;

            case string[] stringParts:
                foreach (var part in stringParts)
                {
                    if (part is not null)
                    {
                        writer.Write(part);
                    }
                }

                break;

            default:
                Assumed.Unreachable();
                break;
        }
    }

    public override string ToString()
    {
        switch (_parts)
        {
            case null:
                return _value.ToString();

            case ReadOnlyMemory<char>[] memoryParts:
                return memoryParts.ToJoinedString();

            case string[] stringParts:
                return stringParts.ToJoinedString();

            case Content[]:
                {
                    using var _ = ArrayBuilderPool<ReadOnlyMemory<char>>.GetPooledObject(out var parts);
                    CollectAllParts(parts);

                    return parts.ToJoinedString();
                }

            default:
                return Assumed.Unreachable<string>();
        }
    }

    public AllContentParts AllParts
        => new(this);

    public Enumerator GetEnumerator()
        => new(this);

    IEnumerator<char> IEnumerable<char>.GetEnumerator()
        => new EnumeratorImpl(this);

    IEnumerator IEnumerable.GetEnumerator()
        => new EnumeratorImpl(this);

    internal void CollectAllParts(ImmutableArray<ReadOnlyMemory<char>>.Builder collector)
    {
        switch (_parts)
        {
            case null:
                if (!_value.IsEmpty)
                {
                    collector.Add(_value);
                }

                break;

            case ReadOnlyMemory<char>[] memoryParts:
                collector.AddRange(memoryParts);
                break;

            case string[] stringParts:
                foreach (var part in stringParts)
                {
                    collector.Add(part.AsMemory());
                }

                break;

            case Content[] contentParts:
                foreach (var part in contentParts)
                {
                    part.CollectAllParts(collector);
                }

                break;

            default:
                Assumed.Unreachable();
                break;
        }
    }

    internal void CollectAllParts(ref PooledArrayBuilder<ReadOnlyMemory<char>> collector)
    {
        switch (_parts)
        {
            case null:
                if (!_value.IsEmpty)
                {
                    collector.Add(_value);
                }

                break;

            case ReadOnlyMemory<char>[] memoryParts:
                collector.AddRange(memoryParts);
                break;

            case string[] stringParts:
                foreach (var part in stringParts)
                {
                    collector.Add(part.AsMemory());
                }

                break;

            case Content[] contentParts:
                foreach (var part in contentParts)
                {
                    part.CollectAllParts(ref collector);
                }

                break;

            default:
                Assumed.Unreachable();
                break;
        }
    }

    /// <summary>
    ///  Determines whether this <see cref="Content"/> is equal to the specified string value.
    /// </summary>
    /// <param name="value">The string to compare with this content.</param>
    /// <returns>
    ///  <see langword="true"/> if the content matches the specified string; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///  This method performs a character-by-character comparison by iterating through all parts
    ///  of the content and comparing them sequentially with the provided value.
    /// </remarks>
    public bool Equals(string? other)
        => Equals(other.AsMemory());

    /// <summary>
    ///  Determines whether this <see cref="Content"/> is equal to the specified <see cref="ReadOnlyMemory{char}"/>.
    /// </summary>
    /// <param name="value">The <see cref="ReadOnlyMemory{char}"/> to compare with this content.</param>
    /// <returns>
    ///  <see langword="true"/> if the content matches the specified <see cref="ReadOnlyMemory{char}"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///  This method performs a character-by-character comparison by iterating through all parts
    ///  of the content and comparing them sequentially with the provided value.
    /// </remarks>
    public bool Equals(ReadOnlyMemory<char> other)
    {
        if (other.IsEmpty)
        {
            return IsEmpty;
        }

        // If the lengths are different, they can't be equal
        if (Length != other.Length)
        {
            return false;
        }

        if (HasValue)
        {
            return ValueComparer.Instance.Equals(_value, other);
        }

        var span = other.Span;

        foreach (var part in AllParts)
        {
            if (part.Length > span.Length)
            {
                return false;
            }

            if (!part.Span.Equals(span[..part.Length], StringComparison.Ordinal))
            {
                return false;
            }

            span = span[part.Length..];
        }

        // If we have consumed the entire span, then the content matches.
        return span.IsEmpty;
    }

    /// <summary>
    ///  Determines whether this <see cref="Content"/> is equal to another <see cref="Content"/> instance.
    /// </summary>
    /// <param name="other">The other content to compare with this content.</param>
    /// <returns>
    ///  <see langword="true"/> if both content instances match character-by-character; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///  This method performs a character-by-character comparison by iterating through all parts
    ///  of both content instances simultaneously. Empty parts are automatically skipped during comparison.
    /// </remarks>
    public bool Equals(Content other)
    {
        if (other.IsEmpty)
        {
            return IsEmpty;
        }

        if (IsEmpty)
        {
            return other.IsEmpty;
        }

        // If the lengths are different, they can't be equal
        if (Length != other.Length)
        {
            return false;
        }

        // If both have single values, use direct comparison
        if (HasValue && other.HasValue)
        {
            return ValueComparer.Instance.Equals(_value, other._value);
        }

        return AllPartsMatch(other);
    }

    private bool AllPartsMatch(Content other)
    {
        Debug.Assert(Length == other.Length);

        // Compare all parts sequentially
        using var thisEnumerator = AllParts.GetEnumerator();
        using var otherEnumerator = other.AllParts.GetEnumerator();

        var thisHasNext = thisEnumerator.MoveNext();
        var otherHasNext = otherEnumerator.MoveNext();

        var thisSpan = thisHasNext ? thisEnumerator.Current.Span : [];
        var otherSpan = otherHasNext ? otherEnumerator.Current.Span : [];

        var thisIndex = 0;
        var otherIndex = 0;

        while (thisHasNext || otherHasNext)
        {
            // Skip empty parts
            while (thisHasNext && thisIndex >= thisSpan.Length)
            {
                thisHasNext = thisEnumerator.MoveNext();
                if (thisHasNext)
                {
                    thisSpan = thisEnumerator.Current.Span;
                    thisIndex = 0;
                }
            }

            while (otherHasNext && otherIndex >= otherSpan.Length)
            {
                otherHasNext = otherEnumerator.MoveNext();
                if (otherHasNext)
                {
                    otherSpan = otherEnumerator.Current.Span;
                    otherIndex = 0;
                }
            }

            // If one has more content than the other
            if (thisHasNext != otherHasNext)
            {
                return false;
            }

            // If both are done, they match
            if (!thisHasNext && !otherHasNext)
            {
                return true;
            }

            // Compare characters
            if (thisSpan[thisIndex] != otherSpan[otherIndex])
            {
                return false;
            }

            thisIndex++;
            otherIndex++;
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hash = HashCodeCombiner.Start();

        if (HasValue)
        {
            hash.Add(_value, ValueComparer.Instance);
        }
        else if (HasParts)
        {
            foreach (var part in AllParts)
            {
                hash.Add(part, ValueComparer.Instance);
            }
        }

        return hash.CombinedHash;
    }

    public static implicit operator Content(ReadOnlyMemory<char> value)
        => new(value);

    public static implicit operator Content(string? value)
        => new(value);

    public static implicit operator Content(ImmutableArray<Content> parts)
        => new(parts);

    public static implicit operator Content(ImmutableArray<ReadOnlyMemory<char>> parts)
        => new(parts);

    public static implicit operator Content(ImmutableArray<string> parts)
        => new(parts);

    public static Content operator +(Content left, Content right)
        => new(new[] { left, right });
}
