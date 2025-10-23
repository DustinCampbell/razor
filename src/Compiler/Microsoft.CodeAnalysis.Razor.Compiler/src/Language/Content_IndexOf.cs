// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

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

        if (IsSingleValue)
        {
            return _value.Span.IndexOf(value);
        }

        var partStart = 0;

        foreach (var part in Parts.NonEmpty)
        {
            var index = part.Span.IndexOf(value);
            if (index >= 0)
            {
                return partStart + index;
            }

            partStart += part.Length;
        }

        return -1;
    }

    /// <summary>
    ///  Reports the zero-based index of the first occurrence of the specified substring.
    /// </summary>
    /// <param name="value">The substring to search for.</param>
    /// <param name="comparisonType">The string comparison type to use.</param>
    /// <returns>
    ///  The zero-based index position of <paramref name="value"/> if found, or -1 if not found.
    /// </returns>
    public int IndexOf(ReadOnlySpan<char> value, StringComparison comparisonType)
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
        if (IsSingleValue)
        {
            return _value.Span.IndexOf(value, comparisonType);
        }

        using var _ = Parts.NonEmpty.AsPooledSpan(out var parts);

        var partStart = 0;

        while (!parts.IsEmpty)
        {
            var part = parts[0];

            // If value is shorter than or the same length the part,
            // try to find it within the part.
            if (part.Length >= value.Length)
            {
                var index = part.Span.IndexOf(value, comparisonType);
                if (index >= 0)
                {
                    return index;
                }
            }

            // OK. It didn't match within the part, but can we make it partially
            // match the end of the part?

            var maxPrefixLength = Math.Min(part.Length, value.Length - 1);
            var remainingValue = value[..maxPrefixLength];

            while (!remainingValue.IsEmpty)
            {
                if (part.Span.EndsWith(remainingValue, comparisonType))
                {
                    // It matches the end of this part. See if we can match the rest
                    var index = partStart + part.Length - remainingValue.Length;
                    remainingValue = value[remainingValue.Length..];

                    if (TryMatchAcrossParts(remainingValue, parts[1..], comparisonType))
                    {
                        return index;
                    }
                }

                remainingValue = remainingValue[..^1];
            }

            partStart += part.Length;
            parts = parts[1..];
        }

        return -1;
    }

    private static bool TryMatchAcrossParts(ReadOnlySpan<char> value, ReadOnlySpan<ReadOnlyMemory<char>> parts, StringComparison comparisonType)
    {
        foreach (var part in parts)
        {
            if (part.IsEmpty)
            {
                continue;
            }

            // If value is shorter than or the same length the part,
            // try to find it within the part.
            if (value.Length <= part.Length)
            {
                return part.Span.StartsWith(value, comparisonType);
            }

            // Value is longer than part. Try to match it completely.
            if (!part.Span.Equals(value[..part.Length], comparisonType))
            {
                return false;
            }

            value = value[part.Length..];
        }

        return false;
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

        if (IsSingleValue)
        {
            return _value.Span.IndexOfAny(value0, value1);
        }

        var partStart = 0;

        foreach (var part in Parts.NonEmpty)
        {
            var index = part.Span.IndexOfAny(value0, value1);
            if (index >= 0)
            {
                return partStart + index;
            }

            partStart += part.Length;
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

        if (IsSingleValue)
        {
            return _value.Span.IndexOfAny(value0, value1, value2);
        }

        var partStart = 0;

        foreach (var part in Parts.NonEmpty)
        {
            var index = part.Span.IndexOfAny(value0, value1, value2);
            if (index >= 0)
            {
                return partStart + index;
            }

            partStart += part.Length;
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

        if (IsSingleValue)
        {
            return _value.Span.IndexOfAny(values);
        }

        var partOffset = 0;

        foreach (var part in Parts.NonEmpty)
        {
            var index = part.Span.IndexOfAny(values);
            if (index >= 0)
            {
                return partOffset + index;
            }

            partOffset += part.Length;
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

        if (IsSingleValue)
        {
#if NET
            return _value.Span.IndexOfAnyExcept(value);
#else
            return IndexOfAnyExceptCore(_value.Span, value);
#endif
        }

        var partStart = 0;

        foreach (var part in Parts.NonEmpty)
        {
#if NET
            var index = part.Span.IndexOfAnyExcept(value);
#else
            var index = IndexOfAnyExceptCore(part.Span, value);
#endif
            if (index >= 0)
            {
                return partStart + index;
            }

            partStart += part.Length;
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

        if (IsSingleValue)
        {
#if NET
            return _value.Span.IndexOfAnyExcept(value0, value1);
#else
            return IndexOfAnyExceptCore(_value.Span, value0, value1);
#endif
        }

        var partStart = 0;

        foreach (var part in Parts.NonEmpty)
        {
#if NET
            var index = part.Span.IndexOfAnyExcept(value0, value1);
#else
            var index = IndexOfAnyExceptCore(part.Span, value0, value1);
#endif
            if (index >= 0)
            {
                return partStart + index;
            }

            partStart += part.Length;
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

        if (IsSingleValue)
        {
#if NET
            return _value.Span.IndexOfAnyExcept(value0, value1, value2);
#else
            return IndexOfAnyExceptCore(_value.Span, value0, value1, value2);
#endif
        }

        var partStart = 0;

        foreach (var part in Parts.NonEmpty)
        {
#if NET
            var index = part.Span.IndexOfAnyExcept(value0, value1, value2);
#else
            var index = IndexOfAnyExceptCore(part.Span, value0, value1, value2);
#endif
            if (index >= 0)
            {
                return partStart + index;
            }

            partStart += part.Length;
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

        if (IsSingleValue)
        {
#if NET
            return _value.Span.IndexOfAnyExcept(values);
#else
            return IndexOfAnyExceptCore(_value.Span, values);
#endif
        }

        var partStart = 0;

        foreach (var part in Parts.NonEmpty)
        {
#if NET
            var index = part.Span.IndexOfAnyExcept(values);
#else
            var index = IndexOfAnyExceptCore(part.Span, values);
#endif
            if (index >= 0)
            {
                return partStart + index;
            }

            partStart += part.Length;
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
