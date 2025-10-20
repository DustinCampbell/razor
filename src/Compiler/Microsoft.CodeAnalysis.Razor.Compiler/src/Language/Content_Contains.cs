// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;


#if !NET
using System.Runtime.CompilerServices;
#endif

namespace Microsoft.AspNetCore.Razor.Language;

public readonly partial struct Content
{
    /// <summary>
    ///  Determines whether the content contains the specified character.
    /// </summary>
    /// <param name="value">The character to search for.</param>
    /// <returns>
    ///  <see langword="true"/> if the character is found; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Contains(char value)
    {
        if (Length == 0)
        {
            return false;
        }

        if (HasValue)
        {
            return _value.Span.IndexOf(value) >= 0;
        }

        foreach (var part in Parts)
        {
            if (part.Span.IndexOf(value) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///  Determines whether the content contains the specified substring.
    /// </summary>
    /// <param name="value">The substring to search for.</param>
    /// <param name="comparison">The string comparison type to use.</param>
    /// <returns>
    ///  <see langword="true"/> if the substring is found; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Contains(ReadOnlySpan<char> value, StringComparison comparison)
    {
        if (value.Length == 0)
        {
            return true;
        }

        if (Length == 0 || value.Length > Length)
        {
            return false;
        }

        // Fast path: single value
        if (HasValue)
        {
            return _value.Span.Contains(value, comparison);
        }

        // Multi-part case: Try to search within individual parts first (common case)
        // Only materialize if the search value might span part boundaries
        foreach (var part in Parts)
        {
            if (part.Span.Contains(value, comparison))
            {
                return true;
            }
        }

        // If we get here, the substring wasn't found in any single part
        // It might span parts, so we need to check boundaries
        // Only materialize the minimum necessary - around part boundaries
        if (Parts.Count <= 1)
        {
            return false; // Already checked the single part above
        }

        // Check across part boundaries
        return ContainsAcrossPartBoundaries(value, comparison);
    }

    private bool ContainsAcrossPartBoundaries(ReadOnlySpan<char> value, StringComparison comparison)
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
        for (var startPartIndex = 0; startPartIndex < parts.Length - 1; startPartIndex++)
        {
            var partSpan = parts[startPartIndex].Span;

            // Only check positions where the match would extend into the next part
            var startPos = Math.Max(0, partSpan.Length - value.Length + 1);

            for (var pos = startPos; pos < partSpan.Length; pos++)
            {
                if (TryMatchAcrossParts(parts[startPartIndex..], pos, value, comparison))
                {
                    return true;
                }
            }
        }

        return false;
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
            if (remainingValue.IsEmpty)
            {
                return true;
            }

            // Move to the next part
            if (remainingParts.IsEmpty)
            {
                // No more parts, but we still have value left to match
                return false;
            }

            currentPart = remainingParts[0].Span;
            remainingParts = remainingParts[1..];
        }
    }

    /// <summary>
    ///  Determines whether the content contains any of the two specified characters.
    /// </summary>
    /// <param name="value0">The first character to search for.</param>
    /// <param name="value1">The second character to search for.</param>
    /// <returns>
    ///  <see langword="true"/> if any of the characters are found; otherwise, <see langword="false"/>.
    /// </returns>
    public bool ContainsAny(char value0, char value1)
    {
        if (Length == 0)
        {
            return false;
        }

        if (HasValue)
        {
            return _value.Span.IndexOfAny(value0, value1) >= 0;
        }

        foreach (var part in Parts)
        {
            if (part.Span.IndexOfAny(value0, value1) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///  Determines whether the content contains any of the three specified characters.
    /// </summary>
    /// <param name="value0">The first character to search for.</param>
    /// <param name="value1">The second character to search for.</param>
    /// <param name="value2">The third character to search for.</param>
    /// <returns>
    ///  <see langword="true"/> if any of the characters are found; otherwise, <see langword="false"/>.
    /// </returns>
    public bool ContainsAny(char value0, char value1, char value2)
    {
        if (Length == 0)
        {
            return false;
        }

        if (HasValue)
        {
            return _value.Span.IndexOfAny(value0, value1, value2) >= 0;
        }

        foreach (var part in Parts)
        {
            if (part.Span.IndexOfAny(value0, value1, value2) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///  Determines whether the content contains any of the specified characters.
    /// </summary>
    /// <param name="values">The characters to search for.</param>
    /// <returns>
    ///  <see langword="true"/> if any of the characters are found; otherwise, <see langword="false"/>.
    /// </returns>
    public bool ContainsAny(ReadOnlySpan<char> values)
    {
        if (Length == 0 || values.Length == 0)
        {
            return false;
        }

        if (HasValue)
        {
            return _value.Span.IndexOfAny(values) >= 0;
        }

        foreach (var part in Parts)
        {
            if (part.Span.IndexOfAny(values) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///  Determines whether the content contains any character other than the specified character.
    /// </summary>
    /// <param name="value">The character to exclude from the search.</param>
    /// <returns>
    ///  <see langword="true"/> if any character other than the specified character is found;
    ///  otherwise, <see langword="false"/>.
    /// </returns>
    public bool ContainsAnyExcept(char value)
    {
        if (Length == 0)
        {
            return false;
        }

        if (HasValue)
        {
#if NET
            return _value.Span.IndexOfAnyExcept(value) >= 0;
#else
            return ContainsAnyExceptCore(_value.Span, value);
#endif
        }

        foreach (var part in Parts)
        {
#if NET
            if (part.Span.IndexOfAnyExcept(value) >= 0)
#else
            if (ContainsAnyExceptCore(part.Span, value))
#endif
            {
                return true;
            }
        }

        return false;
    }

#if !NET
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsAnyExceptCore(ReadOnlySpan<char> span, char value)
    {
        foreach (var ch in span)
        {
            if (ch != value)
            {
                return true;
            }
        }

        return false;
    }
#endif

    /// <summary>
    ///  Determines whether the content contains any character other than the two specified characters.
    /// </summary>
    /// <param name="value0">The first character to exclude from the search.</param>
    /// <param name="value1">The second character to exclude from the search.</param>
    /// <returns>
    ///  <see langword="true"/> if any character other than the specified characters is found;
    ///  otherwise, <see langword="false"/>.
    /// </returns>
    public bool ContainsAnyExcept(char value0, char value1)
    {
        if (Length == 0)
        {
            return false;
        }

        if (HasValue)
        {
#if NET
            return _value.Span.IndexOfAnyExcept(value0, value1) >= 0;
#else
            return ContainsAnyExceptCore(_value.Span, value0, value1);
#endif
        }

        foreach (var part in Parts)
        {
#if NET
            if (part.Span.IndexOfAnyExcept(value0, value1) >= 0)
#else
            if (ContainsAnyExceptCore(part.Span, value0, value1))
#endif
            {
                return true;
            }
        }

        return false;
    }

#if !NET
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsAnyExceptCore(ReadOnlySpan<char> span, char value0, char value1)
    {
        foreach (var ch in span)
        {
            if (ch != value0 && ch != value1)
            {
                return true;
            }
        }

        return false;
    }
#endif

    /// <summary>
    ///  Determines whether the content contains any character other than the three specified characters.
    /// </summary>
    /// <param name="value0">The first character to exclude from the search.</param>
    /// <param name="value1">The second character to exclude from the search.</param>
    /// <param name="value2">The third character to exclude from the search.</param>
    /// <returns>
    ///  <see langword="true"/> if any character other than the specified characters is found;
    ///  otherwise, <see langword="false"/>.
    /// </returns>
    public bool ContainsAnyExcept(char value0, char value1, char value2)
    {
        if (Length == 0)
        {
            return false;
        }

        if (HasValue)
        {
#if NET
            return _value.Span.IndexOfAnyExcept(value0, value1, value2) >= 0;
#else
            return ContainsAnyExceptCore(_value.Span, value0, value1, value2);
#endif
        }

        foreach (var part in Parts)
        {
#if NET
            if (part.Span.IndexOfAnyExcept(value0, value1, value2) >= 0)
#else
            if (ContainsAnyExceptCore(part.Span, value0, value1, value2))
#endif
            {
                return true;
            }
        }

        return false;
    }

#if !NET
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsAnyExceptCore(ReadOnlySpan<char> span, char value0, char value1, char value2)
    {
        foreach (var ch in span)
        {
            if (ch != value0 && ch != value1 && ch != value2)
            {
                return true;
            }
        }

        return false;
    }
#endif

    /// <summary>
    ///  Determines whether the content contains any character other than the specified characters.
    /// </summary>
    /// <param name="values">The characters to exclude from the search.</param>
    /// <returns>
    ///  <see langword="true"/> if any character other than the specified characters is found;
    ///  otherwise, <see langword="false"/>.
    /// </returns>
    public bool ContainsAnyExcept(ReadOnlySpan<char> values)
    {
        if (Length == 0)
        {
            return false;
        }

        if (values.Length == 0)
        {
            return Length > 0;
        }

        if (HasValue)
        {
#if NET
            return _value.Span.IndexOfAnyExcept(values) >= 0;
#else
            return ContainsAnyExceptCore(_value.Span, values);
#endif
        }

        foreach (var part in Parts)
        {
#if NET
            if (part.Span.IndexOfAnyExcept(values) >= 0)
#else
            if (ContainsAnyExceptCore(part.Span, values))
#endif
            {
                return true;
            }
        }

        return false;
    }

#if !NET
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsAnyExceptCore(ReadOnlySpan<char> span, ReadOnlySpan<char> values)
    {
        foreach (var ch in span)
        {
            if (values.IndexOf(ch) < 0)
            {
                return true;
            }
        }

        return false;
    }
#endif
}
