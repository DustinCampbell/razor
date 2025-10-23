// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
///  Represents character content that can be stored either as a single value or as multiple parts.
///  This type is optimized for efficient storage and traversal of potentially nested content structures.
/// </summary>
/// <remarks>
///  <para>
///   <see cref="Content"/> can represent content in several forms:
///   <list type="bullet">
///    <item>A single <see cref="ReadOnlyMemory{T}"/> of characters</item>
///    <item>An array of nested <see cref="Content"/> objects (flattened during enumeration)</item>
///    <item>An array of <see cref="ReadOnlyMemory{T}"/> of characters</item>
///    <item>An array of strings</item>
///   </list>
///  </para>
///  <para>
///   The struct uses bit-packing to efficiently store metadata in a single 32-bit field,
///   keeping the overall struct size small to minimize copying overhead.
///  </para>
/// </remarks>
public readonly partial struct Content : IEquatable<Content>
{
    /// <summary>
    ///  Gets an empty <see cref="Content"/> instance.
    /// </summary>
    public static Content Empty => default;

    private readonly ReadOnlyMemory<char> _value;
    private readonly Array? _parts;
    private readonly ContentData _data;

    /// <summary>
    ///  Initializes a new instance of <see cref="Content"/> with the specified character memory.
    /// </summary>
    /// <param name="value">The character memory to store as content.</param>
    public Content(ReadOnlyMemory<char> value)
    {
        _value = value;
        _parts = null;
        _data = ContentData.Value(value.Length);
    }

    /// <summary>
    ///  Initializes a new instance of <see cref="Content"/> with the specified string value.
    /// </summary>
    /// <param name="value">The string to store as content. A null value creates empty content.</param>
    public Content(string? value)
        : this(value.AsMemory())
    {
    }

    [OverloadResolutionPriority(1)]
    public Content(ref ContentInterpolatedStringHandler handler)
    {
        this = handler.ToContent();
    }

    /// <summary>
    ///  Initializes a new instance of <see cref="Content"/> with multiple nested content parts.
    /// </summary>
    /// <param name="parts">An array of <see cref="Content"/> objects that will be flattened during enumeration.</param>
    /// <remarks>
    ///  If <paramref name="parts"/> contains a single element, it will be unwrapped.
    ///  Empty or null arrays create empty content.
    /// </remarks>
    [OverloadResolutionPriority(2)]
    public Content(ImmutableArray<Content> parts)
        : this(ImmutableCollectionsMarshal.AsArray(parts))
    {
    }

    /// <summary>
    ///  Initializes a new instance of <see cref="Content"/> with multiple character memory parts.
    /// </summary>
    /// <param name="parts">An array of <see cref="ReadOnlyMemory{T}"/> of characters.</param>
    /// <remarks>
    ///  If <paramref name="parts"/> contains a single element, it will be stored as a simple value.
    ///  Empty or null arrays create empty content.
    /// </remarks>
    [OverloadResolutionPriority(2)]
    public Content(ImmutableArray<ReadOnlyMemory<char>> parts)
        : this(ImmutableCollectionsMarshal.AsArray(parts))
    {
    }

    /// <summary>
    ///  Initializes a new instance of <see cref="Content"/> with multiple string parts.
    /// </summary>
    /// <param name="parts">An array of strings.</param>
    /// <remarks>
    ///  If <paramref name="parts"/> contains a single element, it will be stored as a simple value.
    ///  Empty or null arrays create empty content.
    /// </remarks>
    [OverloadResolutionPriority(2)]
    public Content(ImmutableArray<string> parts)
        : this(ImmutableCollectionsMarshal.AsArray(parts))
    {
    }

    private Content(Content[]? parts)
    {
        switch (parts)
        {
            case [] or null:
                this = Empty;
                break;

            case [var item]:
                this = item;
                break;

            default:
                _value = ReadOnlyMemory<char>.Empty;
                _parts = parts;

                var charLength = 0;
                var partsCount = 0;

                foreach (var part in parts)
                {
                    charLength += part.Length;

                    // Count at least one part for each nested content.
                    // Empty content can't contain parts, so it has a PartCount of 0,
                    // but it can still be used as a part.
                    partsCount += Math.Max(1, part._data.PartCount);
                }

                _data = ContentData.ContentArray(charLength, partsCount);
                break;
        }
    }

    private Content(ReadOnlyMemory<char>[]? parts)
    {
        switch (parts)
        {
            case [] or null:
                this = Empty;
                break;

            case [var item]:
                _value = item;
                _parts = null;
                _data = ContentData.Value(item.Length);
                break;

            default:
                _value = ReadOnlyMemory<char>.Empty;
                _parts = parts;

                var charLength = 0;

                foreach (var part in parts)
                {
                    charLength += part.Length;
                }

                _data = ContentData.MemoryArray(charLength, parts.Length);
                break;
        }
    }

    private Content(string[]? parts)
    {
        switch (parts)
        {
            case [] or null:
                this = Empty;
                break;

            case [var item]:
                _value = item.AsMemory();
                _parts = null;
                _data = ContentData.Value(item.Length);
                break;

            default:
                _value = ReadOnlyMemory<char>.Empty;
                _parts = parts;

                var charLength = 0;

                foreach (var part in parts)
                {
                    charLength += part.Length;
                }

                _data = ContentData.StringArray(charLength, parts.Length);
                break;
        }
    }

    /// <summary>
    ///  Gets a value indicating whether this content is empty (contains no characters).
    /// </summary>
    /// <returns>
    ///  <see langword="true"/> if the content is empty; otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsEmpty => _value.IsEmpty && _parts is null;

    /// <summary>
    ///  Gets the character memory when this content represents a single value.
    /// </summary>
    /// <returns>
    ///  The <see cref="ReadOnlyMemory{T}"/> of characters, or an empty memory if this is multi-part content.
    /// </returns>
    /// <remarks>
    ///  Check <see cref="IsSingleValue"/> to determine if this property contains meaningful data.
    /// </remarks>
    public ReadOnlyMemory<char> Value => _value;

    /// <summary>
    ///  Gets the total length of all characters in this content, including nested parts.
    /// </summary>
    /// <returns>
    ///  The total number of characters across all parts.
    /// </returns>
    public int Length => _data.CharLength;

    /// <summary>
    ///  Gets a value indicating whether this content represents a single value.
    /// </summary>
    /// <returns>
    ///  <see langword="true"/> if this content is stored as a single <see cref="ReadOnlyMemory{T}"/>;
    ///  otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsSingleValue => !_value.IsEmpty && _parts is null;

    /// <summary>
    ///  Gets a value indicating whether this content is stored as multiple parts.
    /// </summary>
    /// <returns>
    ///  <see langword="true"/> if this content contains multiple parts;
    ///  otherwise, <see langword="false"/>.
    /// </returns>
    [MemberNotNullWhen(true, nameof(_parts))]
    public bool IsMultiPart => _parts is not null && _value.IsEmpty;

    private bool HasNestedContent => _parts?.Length < _data.PartCount;

    private Content[] ContentParts
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Debug.Assert(IsMultiPart);
            Debug.Assert(_data.Kind == ContentKind.ContentArray);
            return Unsafe.As<Content[]>(_parts);
        }
    }

    private ReadOnlyMemory<char>[] MemoryParts
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Debug.Assert(IsMultiPart);
            Debug.Assert(_data.Kind == ContentKind.MemoryArray);
            return Unsafe.As<ReadOnlyMemory<char>[]>(_parts);
        }
    }

    private string[] StringParts
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Debug.Assert(IsMultiPart);
            Debug.Assert(_data.Kind == ContentKind.StringArray);
            return Unsafe.As<string[]>(_parts);
        }
    }

    /// <summary>
    ///  Gets a list of individual parts contained in this content.
    /// </summary>
    /// <returns>
    ///  A <see cref="PartList"/> that provides access to enumerate all flattened parts.
    ///  For single-value content, returns a list with one element.
    ///  For nested content, returns all leaf values in depth-first order.
    /// </returns>
    /// <remarks>
    ///  The parts are flattened during enumeration, meaning nested <see cref="Content"/> structures
    ///  are traversed to expose only the leaf character sequences.
    /// </remarks>
    public PartList Parts => new(this);

    /// <summary>
    ///  Returns an enumerator that iterates through all characters in the content.
    /// </summary>
    /// <returns>
    ///  A <see cref="CharEnumerator"/> for iterating through the characters.
    /// </returns>
    /// <remarks>
    ///  This method provides allocation-free enumeration of all characters across
    ///  all parts in the content, handling nested structures automatically.
    /// </remarks>
    public CharEnumerator GetEnumerator() => new(this);

    /// <summary>
    ///  Determines whether this <see cref="Content"/> represents the same character data as another <see cref="Content"/>.
    /// </summary>
    /// <param name="other">The <see cref="Content"/> to compare with.</param>
    /// <returns>
    ///  <see langword="true"/> if both instances represent the same sequence of characters;
    ///  otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///  This method performs value equality by comparing the actual character data,
    ///  regardless of how the content is internally structured (single value vs. multi-part).
    ///  Two <see cref="Content"/> instances are considered equal even if they have different
    ///  numbers of parts or different part boundaries, as long as they represent the same
    ///  character sequence.
    /// </remarks>
    public bool Equals(Content other)
    {
        // Fast path: If lengths differ, they can't be equal
        if (Length != other.Length)
        {
            return false;
        }

        // Fast path: Empty content
        if (Length == 0)
        {
            return true;
        }

        // Fast path: Both are single values - compare the memory directly
        if (IsSingleValue && other.IsSingleValue)
        {
            return _value.Span.SequenceEqual(other._value.Span);
        }

        // Slow path: Compare character-by-character, handling different part boundaries
        using var enumerator1 = Parts.NonEmpty.GetEnumerator();
        using var enumerator2 = other.Parts.NonEmpty.GetEnumerator();

        var part1 = ReadOnlyMemory<char>.Empty;
        var part2 = ReadOnlyMemory<char>.Empty;
        var offset1 = 0;
        var offset2 = 0;

        while (true)
        {
            // Get next part from enumerator1 if current part is exhausted
            if (offset1 >= part1.Length)
            {
                if (!enumerator1.MoveNext())
                {
                    // enumerator1 is exhausted, check if enumerator2 is also exhausted
                    return offset2 >= part2.Length && !enumerator2.MoveNext();
                }

                part1 = enumerator1.Current;
                offset1 = 0;
            }

            // Get next part from enumerator2 if current part is exhausted
            if (offset2 >= part2.Length)
            {
                if (!enumerator2.MoveNext())
                {
                    // enumerator2 is exhausted, enumerator1 is not
                    return false;
                }

                part2 = enumerator2.Current;
                offset2 = 0;
            }

            // Compare as many characters as possible from current parts
            var remaining1 = part1.Length - offset1;
            var remaining2 = part2.Length - offset2;
            var compareLength = Math.Min(remaining1, remaining2);

            if (!part1.Span.Slice(offset1, compareLength).SequenceEqual(part2.Span.Slice(offset2, compareLength)))
            {
                return false;
            }

            offset1 += compareLength;
            offset2 += compareLength;
        }
    }

    /// <summary>
    ///  Determines whether this <see cref="Content"/> represents the same character data as the specified object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>
    ///  <see langword="true"/> if <paramref name="obj"/> is a <see cref="Content"/> that represents
    ///  the same sequence of characters; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Equals(object? obj)
        => obj is Content other && Equals(other);

    /// <summary>
    ///  Returns the hash code for this <see cref="Content"/>.
    /// </summary>
    /// <returns>
    ///  A hash code based on the character data represented by this instance.
    /// </returns>
    /// <remarks>
    ///  The hash code is computed from the actual character data to ensure that
    ///  instances representing the same content produce the same hash code,
    ///  regardless of how the content is internally structured.
    /// </remarks>
    public override int GetHashCode()
    {
        if (Length == 0)
        {
            return 0;
        }

        // Always use character-by-character hashing to ensure consistency
        var hash = HashCodeCombiner.Start();

        foreach (var ch in this)
        {
            hash.Add(ch);
        }

        return hash.CombinedHash;
    }

    /// <summary>
    ///  Returns a string representation of the content.
    /// </summary>
    /// <returns>
    ///  A string containing all characters from the content, regardless of how it is
    ///  internally structured (single value or multi-part).
    /// </returns>
    /// <remarks>
    ///  <para>
    ///   This method materializes the entire content into a single string. For large
    ///   content or multi-part content, this allocates a new string on the heap.
    ///  </para>
    ///  <para>
    ///   For single-value content backed by a string, this method returns the
    ///   underlying string directly when possible to avoid allocation.
    ///  </para>
    /// </remarks>
    public override string ToString()
    {
        if (Length == 0)
        {
            return string.Empty;
        }

        // Fast path: single value that's backed by a string
        if (IsSingleValue)
        {
            return _value.ToString();
        }

        // Multi-part content: need to concatenate all parts
        return string.Create(Length, this, static (destination, content) =>
        {
            foreach (var part in content.Parts.NonEmpty)
            {
                var partSpan = part.Span;
                partSpan.CopyTo(destination);

                destination = destination[partSpan.Length..];
            }

            Debug.Assert(destination.IsEmpty);
        });
    }

    public bool IsWhiteSpace()
    {
        if (IsEmpty)
        {
            return true;
        }

        if (IsSingleValue)
        {
            return _value.Span.IsWhiteSpace();
        }

        foreach (var part in Parts.NonEmpty)
        {
            if (!part.Span.IsWhiteSpace())
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsNullOrEmpty(Content? content)
        => content is null or { IsEmpty: true };

    public static bool IsNullOrWhiteSpace(Content? content)
        => content is not Content value || value.IsWhiteSpace();

    /// <summary>
    ///  Determines whether two <see cref="Content"/> instances represent the same character data.
    /// </summary>
    public static bool operator ==(Content left, Content right)
        => left.Equals(right);

    /// <summary>
    ///  Determines whether two <see cref="Content"/> instances represent different character data.
    /// </summary>
    public static bool operator !=(Content left, Content right)
        => !left.Equals(right);

    /// <summary>
    ///  Implicitly converts a <see cref="ReadOnlyMemory{T}"/> of characters to <see cref="Content"/>.
    /// </summary>
    /// <param name="value">The character memory to convert.</param>
    public static implicit operator Content(ReadOnlyMemory<char> value) => new(value);

    /// <summary>
    ///  Implicitly converts a string to <see cref="Content"/>.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    public static implicit operator Content(string value) => new(value);

    /// <summary>
    ///  Implicitly converts an <see cref="ImmutableArray{T}"/> of <see cref="Content"/> to <see cref="Content"/>.
    /// </summary>
    /// <param name="value">The array of content parts to convert.</param>
    public static implicit operator Content(ImmutableArray<Content> value) => new(value);

    /// <summary>
    ///  Implicitly converts an <see cref="ImmutableArray{T}"/> of <see cref="ReadOnlyMemory{T}"/> to <see cref="Content"/>.
    /// </summary>
    /// <param name="value">The array of character memory to convert.</param>
    public static implicit operator Content(ImmutableArray<ReadOnlyMemory<char>> value) => new(value);

    /// <summary>
    ///  Implicitly converts an <see cref="ImmutableArray{T}"/> of strings to <see cref="Content"/>.
    /// </summary>
    /// <param name="value">The array of strings to convert.</param>
    public static implicit operator Content(ImmutableArray<string> value) => new(value);
}
