// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

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
    ///  Creates a new <see cref="Content"/> that represents a slice of this content starting from the specified index.
    /// </summary>
    /// <param name="start">The zero-based starting index of the slice.</param>
    /// <returns>
    ///  A new <see cref="Content"/> instance containing the content from the specified start index to the end.
    ///  If <paramref name="start"/> is greater than or equal to <see cref="Length"/>, returns <see cref="Empty"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  Thrown when <paramref name="start"/> is negative.
    /// </exception>
    public Content Slice(int start)
    {
        ArgHelper.ThrowIfNegative(start);

        if (start >= Length)
        {
            return Empty;
        }

        if (start == 0)
        {
            return this;
        }

        if (HasValue)
        {
            return new Content(_value[start..]);
        }

        return SliceFromParts(start, Length - start);
    }

    /// <summary>
    ///  Creates a new <see cref="Content"/> that represents a slice of this content with the specified start index and length.
    /// </summary>
    /// <param name="start">The zero-based starting index of the slice.</param>
    /// <param name="length">The number of characters to include in the slice.</param>
    /// <returns>
    ///  A new <see cref="Content"/> instance containing the specified slice of content.
    ///  If <paramref name="length"/> is zero, returns <see cref="Empty"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  Thrown when <paramref name="start"/> or <paramref name="length"/> is negative,
    ///  or when <paramref name="start"/> + <paramref name="length"/> exceeds <see cref="Length"/>.
    /// </exception>
    public Content Slice(int start, int length)
    {
        ArgHelper.ThrowIfNegative(start);
        ArgHelper.ThrowIfNegative(length);
        ArgHelper.ThrowIfGreaterThan(start + length, Length);

        if (length == 0)
        {
            return Empty;
        }

        if (start == 0 && length == Length)
        {
            return this;
        }

        if (HasValue)
        {
            return new Content(_value.Slice(start, length));
        }

        return SliceFromParts(start, length);
    }

    private Content SliceFromParts(int start, int length)
    {
        Debug.Assert(HasParts);
        Debug.Assert(start >= 0);
        Debug.Assert(length > 0);
        Debug.Assert(start + length <= Length);

        // Find the starting position and slice the collected parts
        var currentPosition = 0;
        var remaining = length;

        using var resultParts = new PooledArrayBuilder<ReadOnlyMemory<char>>();

        foreach (var part in AllParts)
        {
            if (currentPosition + part.Length <= start)
            {
                // This part is entirely before our slice
                currentPosition += part.Length;
                continue;
            }

            if (remaining <= 0)
            {
                // We've collected all the content we need
                break;
            }

            var partStart = Math.Max(0, start - currentPosition);
            var partLength = Math.Min(part.Length - partStart, remaining);

            if (partLength > 0)
            {
                var slicedPart = part.Slice(partStart, partLength);
                resultParts.Add(slicedPart);
                remaining -= partLength;
            }

            currentPosition += part.Length;
        }

        return resultParts.ToContent();
    }

    /// <summary>
    ///  Copies the entire content to the specified destination span.
    /// </summary>
    /// <param name="destination">
    ///  The span to copy the content to. Must have sufficient capacity to hold all characters.
    /// </param>
    /// <exception cref="ArgumentException">
    ///  Thrown when <paramref name="destination"/> is too short to contain the entire content.
    /// </exception>
    /// <remarks>
    ///  This method copies all characters from the content to the destination span sequentially.
    ///  If the content has multiple parts, they are concatenated in order during the copy operation.
    /// </remarks>
    public void CopyTo(Span<char> destination)
    {
        ArgHelper.ThrowIfDestinationTooShort(destination, Length);

        if (IsEmpty)
        {
            return;
        }

        if (HasValue)
        {
            _value.Span.CopyTo(destination);
            return;
        }

        var currentIndex = 0;
        foreach (var part in AllParts)
        {
            if (!part.IsEmpty)
            {
                part.Span.CopyTo(destination[currentIndex..]);
                currentIndex += part.Length;
            }
        }
    }

    /// <summary>
    ///  Copies a specified portion of the content to the destination span.
    /// </summary>
    /// <param name="sourceIndex">
    ///  The zero-based starting index in the content from which to begin copying.
    /// </param>
    /// <param name="destination">
    ///  The span to copy the content to. Must have sufficient capacity to hold the specified number of characters.
    /// </param>
    /// <param name="count">
    ///  The number of characters to copy from the content.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  Thrown when <paramref name="sourceIndex"/> or <paramref name="count"/> is negative,
    ///  or when <paramref name="sourceIndex"/> + <paramref name="count"/> exceeds <see cref="Length"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///  Thrown when <paramref name="destination"/> is too short to contain <paramref name="count"/> characters.
    /// </exception>
    /// <remarks>
    ///  This method extracts a substring from the content starting at the specified index
    ///  and copies it to the destination span. If the content has multiple parts, the method
    ///  navigates through them to find and copy the requested range of characters.
    /// </remarks>
    public void CopyTo(int sourceIndex, Span<char> destination, int count)
    {
        ArgHelper.ThrowIfNegative(sourceIndex);
        ArgHelper.ThrowIfNegative(count);
        ArgHelper.ThrowIfGreaterThan(sourceIndex + count, Length);
        ArgHelper.ThrowIfDestinationTooShort(destination, count);

        if (count == 0)
        {
            return;
        }

        if (HasValue)
        {
            _value.Span.Slice(sourceIndex, count).CopyTo(destination);
            return;
        }

        var currentPosition = 0;
        var copied = 0;

        foreach (var part in AllParts)
        {
            if (copied >= count)
            {
                break;
            }

            if (currentPosition + part.Length <= sourceIndex)
            {
                // This part is entirely before our slice
                currentPosition += part.Length;
                continue;
            }

            var partStart = Math.Max(0, sourceIndex - currentPosition);
            var partLength = Math.Min(part.Length - partStart, count - copied);

            if (partLength > 0)
            {
                part.Span.Slice(partStart, partLength).CopyTo(destination[copied..]);
                copied += partLength;
            }

            currentPosition += part.Length;
        }
    }

    /// <summary>
    ///  Attempts to copy the entire content to the specified destination span.
    /// </summary>
    /// <param name="destination">
    ///  The span to copy the content to.
    /// </param>
    /// <returns>
    ///  <see langword="true"/> if the content was successfully copied to the destination;
    ///  <see langword="false"/> if the destination span was too small to contain the entire content.
    /// </returns>
    /// <remarks>
    ///  This method provides a non-throwing alternative to <see cref="CopyTo(Span{char})"/>.
    ///  If the destination span is large enough, the content is copied and the method returns <see langword="true"/>.
    ///  If the destination span is too small, no copying occurs and the method returns <see langword="false"/>.
    /// </remarks>
    public bool TryCopyTo(Span<char> destination)
    {
        if (destination.Length < Length)
        {
            return false;
        }

        CopyTo(destination);
        return true;
    }

    /// <summary>
    ///  Attempts to copy a specified portion of the content to the destination span.
    /// </summary>
    /// <param name="startIndex">
    ///  The zero-based starting index in the content from which to begin copying.
    /// </param>
    /// <param name="destination">
    ///  The span to copy the content to.
    /// </param>
    /// <param name="count">
    ///  The number of characters to copy from the content.
    /// </param>
    /// <returns>
    ///  <see langword="true"/> if the specified portion of content was successfully copied to the destination;
    ///  <see langword="false"/> if the destination span was too small to contain the specified number of characters.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  Thrown when <paramref name="startIndex"/> or <paramref name="count"/> is negative,
    ///  or when <paramref name="startIndex"/> + <paramref name="count"/> exceeds <see cref="Length"/>.
    /// </exception>
    /// <remarks>
    ///  This method provides a non-throwing alternative to <see cref="CopyTo(int, Span{char}, int)"/> for destination capacity issues.
    ///  If the destination span is large enough, the specified portion of content is copied and the method returns <see langword="true"/>.
    ///  If the destination span is too small, no copying occurs and the method returns <see langword="false"/>.
    ///  Invalid parameter values will still throw exceptions as documented.
    /// </remarks>
    public bool TryCopyTo(int startIndex, Span<char> destination, int count)
    {
        ArgHelper.ThrowIfNegative(startIndex);
        ArgHelper.ThrowIfNegative(count);
        ArgHelper.ThrowIfGreaterThan(startIndex + count, Length);

        if (destination.Length < count)
        {
            return false;
        }

        CopyTo(startIndex, destination, count);
        return true;
    }

    /// <summary>
    ///  Reports the zero-based index of the first occurrence of the specified character in this content.
    /// </summary>
    /// <param name="value">The character to seek.</param>
    /// <returns>
    ///  The zero-based index position of <paramref name="value"/> if that character is found, or -1 if it is not.
    ///  Returns -1 if the content is empty.
    /// </returns>
    /// <remarks>
    ///  This method searches through all parts of the content sequentially when the content has multiple parts.
    ///  The returned index represents the position within the entire logical content, not within individual parts.
    /// </remarks>
    public int IndexOf(char value)
    {
        if (IsEmpty)
        {
            return -1;
        }

        if (HasValue)
        {
            return Value.Span.IndexOf(value);
        }

        Debug.Assert(HasParts);

        var currentIndex = 0;

        foreach (var part in AllParts)
        {
            var span = part.Span;
            var index = span.IndexOf(value);

            if (index >= 0)
            {
                return currentIndex + index;
            }

            currentIndex += span.Length;
        }

        return -1;
    }

    /// <summary>
    ///  Determines whether the specified character occurs within this content.
    /// </summary>
    /// <param name="value">The character to seek.</param>
    /// <returns>
    ///  <see langword="true"/> if the <paramref name="value"/> character is found in the content; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///  This method is equivalent to calling <see cref="IndexOf(char)"/> and checking if the result is greater than or equal to zero.
    /// </remarks>
    public bool Contains(char value)
        => IndexOf(value) >= 0;

    /// <summary>
    ///  Returns a new <see cref="Content"/> with all occurrences of a specified character replaced with another character.
    /// </summary>
    /// <param name="oldChar">The character to be replaced.</param>
    /// <param name="newChar">The character to replace all occurrences of <paramref name="oldChar"/>.</param>
    /// <returns>
    ///  A new <see cref="Content"/> with the replacements made, or the current instance if no replacements were necessary.
    ///  Returns the same instance if the content is empty or if <paramref name="oldChar"/> equals <paramref name="newChar"/>.
    /// </returns>
    /// <remarks>
    ///  This method processes all parts of the content and creates a new instance only if replacements are made.
    ///  If no occurrences of <paramref name="oldChar"/> are found, the original instance is returned for efficiency.
    /// </remarks>
    public Content Replace(char oldChar, char newChar)
    {
        if (IsEmpty || oldChar == newChar)
        {
            return this;
        }

        using var parts = new PooledArrayBuilder<ReadOnlyMemory<char>>();

        var hasReplacements = false;
        var replacementChar = newChar.ToString().AsMemory();

        if (HasValue)
        {
            hasReplacements = ProcessPart(_value, oldChar, replacementChar, ref parts.AsRef());
        }
        else
        {
            foreach (var part in AllParts)
            {
                hasReplacements |= ProcessPart(part, oldChar, replacementChar, ref parts.AsRef());
            }
        }

        return hasReplacements ? parts.ToContent() : this;

        static bool ProcessPart(
            ReadOnlyMemory<char> part, char oldChar, ReadOnlyMemory<char> newChar, ref PooledArrayBuilder<ReadOnlyMemory<char>> parts)
        {
            var indexOfOld = part.Span.IndexOf(oldChar);

            if (indexOfOld < 0)
            {
                parts.Add(part);
                return false;
            }

            do
            {
                if (indexOfOld > 0)
                {
                    // Add segment before the found character
                    parts.Add(part[..indexOfOld]);
                }

                // Add the replacement character
                parts.Add(newChar);

                // Skip past the found character
                part = part[(indexOfOld + 1)..];
                indexOfOld = part.Span.IndexOf(oldChar);
            }
            while (indexOfOld >= 0);

            if (!part.IsEmpty)
            {
                // Add any remaining segment after the last found character
                parts.Add(part);
            }

            return true;
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
