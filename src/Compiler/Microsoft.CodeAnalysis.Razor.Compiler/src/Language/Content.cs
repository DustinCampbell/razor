// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
public readonly partial struct Content
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

    /// <summary>
    ///  Initializes a new instance of <see cref="Content"/> with multiple nested content parts.
    /// </summary>
    /// <param name="parts">An array of <see cref="Content"/> objects that will be flattened during enumeration.</param>
    /// <remarks>
    ///  If <paramref name="parts"/> contains a single element, it will be unwrapped.
    ///  Empty or null arrays create empty content.
    /// </remarks>
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

                ref var partsRef = ref parts[0];

                for (var i = 0; i < parts.Length; i++)
                {
                    ref readonly var part = ref Unsafe.Add(ref partsRef, i);

                    charLength += part.Length;
                    partsCount += part._data.PartCount;
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
    ///  Check <see cref="HasValue"/> to determine if this property contains meaningful data.
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
    public bool HasValue => !_value.IsEmpty && _parts is null;

    /// <summary>
    ///  Gets a value indicating whether this content is stored as multiple parts.
    /// </summary>
    /// <returns>
    ///  <see langword="true"/> if this content contains multiple parts;
    ///  otherwise, <see langword="false"/>.
    /// </returns>
    [MemberNotNullWhen(true, nameof(_parts))]
    public bool IsMultiPart => _parts is not null && _value.IsEmpty;

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
