// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;

namespace Microsoft.AspNetCore.Razor.Language;

public readonly partial struct Content
{
    /// <summary>
    ///  Reports the zero-based index of the first occurrence of the specified character.
    /// </summary>
    /// <param name="value">The character to search for.</param>
    /// <returns>
    ///  The zero-based index position of <paramref name="value"/> if found, or -1 if not found.
    /// </returns>
    public int IndexOf(char value)
    {
        if (Length == 0)
        {
            return -1;
        }

        if (HasValue)
        {
            return _value.Span.IndexOf(value);
        }

        var offset = 0;
        foreach (var part in Parts)
        {
            var index = part.Span.IndexOf(value);
            if (index >= 0)
            {
                return offset + index;
            }

            offset += part.Length;
        }

        return -1;
    }

    /// <summary>
    ///  Reports the zero-based index of the first occurrence of the specified substring.
    /// </summary>
    /// <param name="value">The substring to search for.</param>
    /// <param name="comparison">The string comparison type to use.</param>
    /// <returns>
    ///  The zero-based index position of <paramref name="value"/> if found, or -1 if not found.
    /// </returns>
    public int IndexOf(ReadOnlySpan<char> value, StringComparison comparison)
    {
        if (value.Length == 0)
        {
            return 0;
        }

        if (Length == 0 || value.Length > Length)
        {
            return -1;
        }

        // Fast path: single value
        if (HasValue)
        {
            return _value.Span.IndexOf(value, comparison);
        }

        // Multi-part case: Try to search within individual parts first (common case)
        var offset = 0;
        foreach (var part in Parts)
        {
            var index = part.Span.IndexOf(value, comparison);
            if (index >= 0)
            {
                return offset + index;
            }

            offset += part.Length;
        }

        // If we get here, the substring wasn't found in any single part
        // It might span parts, so we need to check boundaries
        if (Parts.Count <= 1)
        {
            return -1; // Already checked the single part above
        }

        // Check across part boundaries
        return IndexOfAcrossPartBoundaries(value, comparison);
    }

    private int IndexOfAcrossPartBoundaries(ReadOnlySpan<char> value, StringComparison comparison)
    {
        // First, gather up all of the parts
        using var _ = ArrayPool<ReadOnlyMemory<char>>.Shared.GetPooledArraySpan(
            minimumLength: _data.PartCount,
            clearOnReturn: true,
            out var parts);

        var index = 0;
        foreach (var part in Parts)
        {
            parts[index++] = part;
        }

        // Try to match at positions that span part boundaries
        // We only need to check the last (value.Length - 1) positions in each part
        // since matches fully contained in a part were already checked
        var offset = 0;
        for (var startPartIndex = 0; startPartIndex < parts.Length - 1; startPartIndex++)
        {
            var partSpan = parts[startPartIndex].Span;

            // Only check positions where the match would extend into the next part
            var startPos = Math.Max(0, partSpan.Length - value.Length + 1);

            for (var pos = startPos; pos < partSpan.Length; pos++)
            {
                if (TryMatchAcrossParts(parts[startPartIndex..], pos, value, comparison))
                {
                    return offset + pos;
                }
            }

            offset += partSpan.Length;
        }

        return -1;
    }

    private static bool TryMatchAcrossParts(
        ReadOnlySpan<ReadOnlyMemory<char>> parts,
        int startPos,
        ReadOnlySpan<char> value,
        StringComparison comparison)
    {
        // Start with the first part, sliced to begin at startPos
        var currentPart = parts[0].Span[startPos..];
        var remainingParts = parts[1..];
        var remainingValue = value;

        while (true)
        {
            var chunkSize = Math.Min(currentPart.Length, remainingValue.Length);

            if (!currentPart[..chunkSize].Equals(remainingValue[..chunkSize], comparison))
            {
                return false;
            }

            // Advance the value
            remainingValue = remainingValue[chunkSize..];

            // Check if we've matched everything
            if (remainingValue.Length == 0)
            {
                return true;
            }

            // Move to the next part
            if (remainingParts.Length == 0)
            {
                // No more parts, but we still have value left to match
                return false;
            }

            currentPart = remainingParts[0].Span;
            remainingParts = remainingParts[1..];
        }
    }

    /// <summary>
    ///  Reports the zero-based index of the first occurrence of any of the two specified characters.
    /// </summary>
    /// <param name="value0">The first character to search for.</param>
    /// <param name="value1">The second character to search for.</param>
    /// <returns>
    ///  The zero-based index position of the first occurrence of any character, or -1 if not found.
    /// </returns>
    public int IndexOfAny(char value0, char value1)
    {
        if (Length == 0)
        {
            return -1;
        }

        if (HasValue)
        {
            return _value.Span.IndexOfAny(value0, value1);
        }

        var offset = 0;
        foreach (var part in Parts)
        {
            var index = part.Span.IndexOfAny(value0, value1);
            if (index >= 0)
            {
                return offset + index;
            }

            offset += part.Length;
        }

        return -1;
    }

    /// <summary>
    ///  Reports the zero-based index of the first occurrence of any of the three specified characters.
    /// </summary>
    /// <param name="value0">The first character to search for.</param>
    /// <param name="value1">The second character to search for.</param>
    /// <param name="value2">The third character to search for.</param>
    /// <returns>
    ///  The zero-based index position of the first occurrence of any character, or -1 if not found.
    /// </returns>
    public int IndexOfAny(char value0, char value1, char value2)
    {
        if (Length == 0)
        {
            return -1;
        }

        if (HasValue)
        {
            return _value.Span.IndexOfAny(value0, value1, value2);
        }

        var offset = 0;
        foreach (var part in Parts)
        {
            var index = part.Span.IndexOfAny(value0, value1, value2);
            if (index >= 0)
            {
                return offset + index;
            }

            offset += part.Length;
        }

        return -1;
    }

    /// <summary>
    ///  Reports the zero-based index of the first occurrence of any of the specified characters.
    /// </summary>
    /// <param name="values">The characters to search for.</param>
    /// <returns>
    ///  The zero-based index position of the first occurrence of any character, or -1 if not found.
    /// </returns>
    public int IndexOfAny(ReadOnlySpan<char> values)
    {
        if (Length == 0 || values.Length == 0)
        {
            return -1;
        }

        if (HasValue)
        {
            return _value.Span.IndexOfAny(values);
        }

        var offset = 0;
        foreach (var part in Parts)
        {
            var index = part.Span.IndexOfAny(values);
            if (index >= 0)
            {
                return offset + index;
            }

            offset += part.Length;
        }

        return -1;
    }

    /// <summary>
    ///  Reports the zero-based index of the first occurrence of any character other than the specified character.
    /// </summary>
    /// <param name="value">The character to exclude from the search.</param>
    /// <returns>
    ///  The zero-based index position of the first occurrence of any other character, or -1 if not found.
    /// </returns>
    public int IndexOfAnyExcept(char value)
    {
        if (Length == 0)
        {
            return -1;
        }

        if (HasValue)
        {
#if NET
            return _value.Span.IndexOfAnyExcept(value);
#else
            return IndexOfAnyExceptCore(_value.Span, value);
#endif
        }

        var offset = 0;
        foreach (var part in Parts)
        {
#if NET
            var index = part.Span.IndexOfAnyExcept(value);
#else
            var index = IndexOfAnyExceptCore(part.Span, value);
#endif
            if (index >= 0)
            {
                return offset + index;
            }

            offset += part.Length;
        }

        return -1;
    }

#if !NET
    private static int IndexOfAnyExceptCore(ReadOnlySpan<char> span, char value)
    {
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] != value)
            {
                return i;
            }
        }

        return -1;
    }
#endif

    /// <summary>
    ///  Reports the zero-based index of the first occurrence of any character other than the two specified characters.
    /// </summary>
    /// <param name="value0">The first character to exclude from the search.</param>
    /// <param name="value1">The second character to exclude from the search.</param>
    /// <returns>
    ///  The zero-based index position of the first occurrence of any other character, or -1 if not found.
    /// </returns>
    public int IndexOfAnyExcept(char value0, char value1)
    {
        if (Length == 0)
        {
            return -1;
        }

        if (HasValue)
        {
#if NET
            return _value.Span.IndexOfAnyExcept(value0, value1);
#else
            return IndexOfAnyExceptCore(_value.Span, value0, value1);
#endif
        }

        var offset = 0;
        foreach (var part in Parts)
        {
#if NET
            var index = part.Span.IndexOfAnyExcept(value0, value1);
#else
            var index = IndexOfAnyExceptCore(part.Span, value0, value1);
#endif
            if (index >= 0)
            {
                return offset + index;
            }

            offset += part.Length;
        }

        return -1;
    }

#if !NET
    private static int IndexOfAnyExceptCore(ReadOnlySpan<char> span, char value0, char value1)
    {
        for (var i = 0; i < span.Length; i++)
        {
            var ch = span[i];
            if (ch != value0 && ch != value1)
            {
                return i;
            }
        }

        return -1;
    }
#endif

    /// <summary>
    ///  Reports the zero-based index of the first occurrence of any character other than the three specified characters.
    /// </summary>
    /// <param name="value0">The first character to exclude from the search.</param>
    /// <param name="value1">The second character to exclude from the search.</param>
    /// <param name="value2">The third character to exclude from the search.</param>
    /// <returns>
    ///  The zero-based index position of the first occurrence of any other character, or -1 if not found.
    /// </returns>
    public int IndexOfAnyExcept(char value0, char value1, char value2)
    {
        if (Length == 0)
        {
            return -1;
        }

        if (HasValue)
        {
#if NET
            return _value.Span.IndexOfAnyExcept(value0, value1, value2);
#else
            return IndexOfAnyExceptCore(_value.Span, value0, value1, value2);
#endif
        }

        var offset = 0;
        foreach (var part in Parts)
        {
#if NET
            var index = part.Span.IndexOfAnyExcept(value0, value1, value2);
#else
            var index = IndexOfAnyExceptCore(part.Span, value0, value1, value2);
#endif
            if (index >= 0)
            {
                return offset + index;
            }

            offset += part.Length;
        }

        return -1;
    }

#if !NET
    private static int IndexOfAnyExceptCore(ReadOnlySpan<char> span, char value0, char value1, char value2)
    {
        for (var i = 0; i < span.Length; i++)
        {
            var ch = span[i];
            if (ch != value0 && ch != value1 && ch != value2)
            {
                return i;
            }
        }

        return -1;
    }
#endif

    /// <summary>
    ///  Reports the zero-based index of the first occurrence of any character other than the specified characters.
    /// </summary>
    /// <param name="values">The characters to exclude from the search.</param>
    /// <returns>
    ///  The zero-based index position of the first occurrence of any other character, or -1 if not found.
    /// </returns>
    public int IndexOfAnyExcept(ReadOnlySpan<char> values)
    {
        if (Length == 0)
        {
            return -1;
        }

        if (values.Length == 0)
        {
            return Length > 0 ? 0 : -1;
        }

        if (HasValue)
        {
#if NET
            return _value.Span.IndexOfAnyExcept(values);
#else
            return IndexOfAnyExceptCore(_value.Span, values);
#endif
        }

        var offset = 0;
        foreach (var part in Parts)
        {
#if NET
            var index = part.Span.IndexOfAnyExcept(values);
#else
            var index = IndexOfAnyExceptCore(part.Span, values);
#endif
            if (index >= 0)
            {
                return offset + index;
            }

            offset += part.Length;
        }

        return -1;
    }

#if !NET
    private static int IndexOfAnyExceptCore(ReadOnlySpan<char> span, ReadOnlySpan<char> values)
    {
        for (var i = 0; i < span.Length; i++)
        {
            if (values.IndexOf(span[i]) < 0)
            {
                return i;
            }
        }

        return -1;
    }
#endif
}
