// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

#if NET
using System.Buffers;
#endif

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

internal static partial class CodeWriterExtensions
{
    private static readonly ReadOnlyMemory<char> s_true = "true".AsMemory();
    private static readonly ReadOnlyMemory<char> s_false = "false".AsMemory();

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
