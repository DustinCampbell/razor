// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Globalization;

#if NET
using System.Buffers;
#endif

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

internal static partial class CodeWriterExtensions
{
    private static readonly ReadOnlyMemory<char> s_true = "true".AsMemory();
    private static readonly ReadOnlyMemory<char> s_false = "false".AsMemory();

    private static readonly ReadOnlyMemory<char> s_zeroes = "0000000000".AsMemory(); // 10 zeros

    // This table contains string representations of numbers from 0 to 999.
    private static readonly ImmutableArray<ReadOnlyMemory<char>> s_integerTable = InitializeIntegerTable();

    private static ImmutableArray<ReadOnlyMemory<char>> InitializeIntegerTable()
    {
        var array = new ReadOnlyMemory<char>[1000];

        // Fill entries 100 to 999.
        for (var i = 100; i < 1000; i++)
        {
            array[i] = i.ToString(CultureInfo.InvariantCulture).AsMemory();
        }

        // Fill entries 10 to 99 with two-digit strings sliced from entries 110 to 199.
        for (var i = 10; i < 100; i++)
        {
            array[i] = array[i + 100][^2..];
        }

        // Fill 1 to 9 with slices of the last character from entries 11 to 19.
        for (var i = 1; i < 10; i++)
        {
            array[i] = array[i + 10][^1..];
        }

        // Finally, fill the entry for 0 with a slice from s_zeroes.
        array[0] = s_zeroes[..1];

        return ImmutableCollectionsMarshal.AsImmutableArray(array);
    }

    private static readonly char[] s_stringLiteralEscapeChars =
    [
        '\r',
        '\t',
        '\"',
        '\'',
        '\\',
        '\0',
        '\n',
        '\u2028',
        '\u2029',
    ];

#if NET
    private static readonly SearchValues<char> s_searchValues = SearchValues.Create(s_stringLiteralEscapeChars);
#endif

    private static int IndexOfEscapeChar(ReadOnlySpan<char> span)
    {
#if NET
        return span.IndexOfAny(s_searchValues);
#else
        return span.IndexOfAny(s_stringLiteralEscapeChars);
#endif
    }

    private static string EscapeCharToString(char ch) => ch switch
    {
        '\r' => "\\r",
        '\t' => "\\t",
        '\"' => "\\\"",
        '\'' => "\\\'",
        '\\' => "\\\\",
        '\0' => "\\\0",
        '\n' => "\\n",
        '\u2028' => "\\u2028",
        '\u2029' => "\\u2029",
        _ => Assumed.Unreachable<string>($"Unknown escape character: {(ushort)ch:x4} '{ch}'")
    };

    public static CodeWriter WriteBooleanLiteral(this CodeWriter writer, bool value)
        => writer.Write(value ? s_true : s_false);

    /// <summary>
    ///  Writes an integer literal to the code writer using optimized precomputed lookup tables
    ///  and efficient grouping for large numbers. This avoids string allocation and formatting overhead.
    /// </summary>
    /// <param name="writer">The code writer to write to.</param>
    /// <param name="value">The integer value to write as a literal.</param>
    /// <returns>
    ///  The code writer for method chaining.
    /// </returns>
    /// <remarks>
    ///  Performance optimizations:
    ///  <list type="bullet">
    ///   <item>Zero is written directly from a precomputed slice</item>
    ///   <item>Numbers -999 to 999 use a precomputed lookup table</item>
    ///   <item>Larger numbers are decomposed into groups of 3 digits, each using the lookup table</item>
    ///   <item>Uses long arithmetic to handle int.MinValue correctly (avoids overflow when negating)</item>
    ///  </list>
    /// </remarks>
    public static CodeWriter WriteIntegerLiteral(this CodeWriter writer, int value)
    {
        // Handle zero as a special case
        if (value == 0)
        {
            return writer.Write(s_integerTable[0]);
        }

        var isNegative = value < 0;
        if (isNegative)
        {
            // For negative numbers, write the minus sign first
            writer.Write("-");
        }

        // Fast path: For small numbers (-999 to 999), use the precomputed lookup table directly
        if (value is > -1000 and < 1000)
        {
            var index = isNegative ? -value : value;
            return writer.Write(s_integerTable[index]);
        }

        // Slow path: For larger numbers, decompose into groups of three digits using the precomputed table.
        // This approach avoids string formatting while maintaining readability of the output.

        // Extract digits and write groups from most significant to least significant.
        // Note: Use long to handle int.MinValue correctly. Math.Abs(int.MinValue) would throw.
        var remaining = isNegative ? -(long)value : value;
        long divisor = 1;

        // Find the highest power of 1000 needed (1, 1000, 1000000, 1000000000)
        // This determines how many 3-digit groups we need
        while (remaining >= divisor * 1000)
        {
            divisor *= 1000;
        }

        // Process each group of 3 digits from most significant to least significant
        var first = true;
        while (divisor > 0)
        {
            var group = (int)(remaining / divisor);
            remaining %= divisor;
            divisor /= 1000;

            Debug.Assert(group >= 0 && group < 1000, "Digit group should be in the range [0, 999]");

            if (group == 0)
            {
                Debug.Assert(!first, "The first group should never be 0.");

                // Entire group is zero: add "000" for proper place value
                writer.Write(s_zeroes[..3]);
                continue;
            }

            if (first)
            {
                // First group: no leading zeros needed (e.g., "123" not "0123")
                writer.Write(s_integerTable[group]);
                first = false;
                continue;
            }

            // Groups after the first one with values 1-99 need leading zeros for proper formatting
            // Example: 1234567 becomes "1" + "234" + "567", but 1000067 becomes "1" + "000" + "067"
            var leadingZeros = group switch
            {
                < 10 => 2,  // 1-9: needs "00" prefix (e.g., "007")
                < 100 => 1, // 10-99: needs "0" prefix (e.g., "067") 
                _ => 0      // 100-999: no leading zeros needed
            };

            if (leadingZeros > 0)
            {
                writer.Write(s_zeroes[..leadingZeros]); // Add "00" or "0"
            }

            writer.Write(s_integerTable[group]); // Add the actual digit group
        }

        return writer;
    }

    public static CodeWriter WriteStringLiteral(this CodeWriter writer, Content value)
    {
        if (value.Length is >= 256 and <= 1500 && !value.Contains('\0'))
        {
            writer.WriteVerbatimStringLiteral(value);
        }
        else
        {
            writer.WriteCStyleStringLiteral(value);
        }

        return writer;
    }

    public static CodeWriter WriteStringLiteral(this CodeWriter writer, ref readonly PooledArrayBuilder<ReadOnlyMemory<char>> parts)
    {
        var length = 0;
        var containsNullChar = false;

        foreach (var part in parts)
        {
            length += part.Length;

            if (!containsNullChar && part.Span.IndexOf('\0') >= 0)
            {
                containsNullChar = true;
            }
        }

        return length is >= 256 and <= 1500
            ? writer.WriteVerbatimStringLiteral(in parts)
            : writer.WriteCStyleStringLiteral(in parts);
    }

    private static void WriteVerbatimStringLiteral(this CodeWriter writer, Content value)
    {
        writer.Write("@\"");

        // We need to suppress indenting during the writing of the string's content. A
        // verbatim string literal could contain newlines that don't get escaped.
        var oldIndent = writer.CurrentIndent;
        writer.CurrentIndent = 0;

        foreach (var part in value.AllParts)
        {
            writer.WriteVerbatimStringLiteralPart(part);
        }

        writer.Write("\"");

        writer.CurrentIndent = oldIndent;
    }

    private static CodeWriter WriteVerbatimStringLiteral(this CodeWriter writer, ref readonly PooledArrayBuilder<ReadOnlyMemory<char>> parts)
    {
        writer.Write("@\"");

        // We need to suppress indenting during the writing of the string's content. A
        // verbatim string literal could contain newlines that don't get escaped.
        var oldIndent = writer.CurrentIndent;
        writer.CurrentIndent = 0;

        foreach (var part in parts)
        {
            writer.WriteVerbatimStringLiteralPart(part);
        }

        writer.Write("\"");

        writer.CurrentIndent = oldIndent;

        return writer;
    }

    private static void WriteVerbatimStringLiteralPart(this CodeWriter writer, ReadOnlyMemory<char> part)
    {
        var literal = part;

        // We need to find the index of each '"' (double-quote) to escape it.
        int index;
        while ((index = literal.Span.IndexOf('"')) >= 0)
        {
            writer.Write(literal[..index]);
            writer.Write("\"\"");

            literal = literal[(index + 1)..];
        }

        Debug.Assert(index == -1); // We've hit all of the double-quotes.

        // Write the remainder after the last double-quote.
        writer.Write(literal);
    }

    private static void WriteCStyleStringLiteral(this CodeWriter writer, Content value)
    {
        // From CSharpCodeGenerator.QuoteSnippetStringCStyle in CodeDOM
        writer.Write("\"");

        foreach (var part in value.AllParts)
        {
            writer.WriteCStyleStringLiteralPart(part);
        }

        writer.Write("\"");
    }

    private static CodeWriter WriteCStyleStringLiteral(this CodeWriter writer, ref readonly PooledArrayBuilder<ReadOnlyMemory<char>> parts)
    {
        // From CSharpCodeGenerator.QuoteSnippetStringCStyle in CodeDOM
        writer.Write("\"");

        foreach (var part in parts)
        {
            writer.WriteCStyleStringLiteralPart(part);
        }

        writer.Write("\"");

        return writer;
    }

    private static void WriteCStyleStringLiteralPart(this CodeWriter writer, ReadOnlyMemory<char> part)
    {
        var literal = part;

        // We need to find the index of each escapable character to escape it.
        int index;
        while ((index = IndexOfEscapeChar(literal.Span)) >= 0)
        {
            writer.Write(literal[..index]);
            writer.Write(EscapeCharToString(literal.Span[index]));

            literal = literal[(index + 1)..];
        }

        Debug.Assert(index == -1); // We've hit all of chars that need escaping.

        // Write the remainder after the last escaped char.
        writer.Write(literal);
    }
}
