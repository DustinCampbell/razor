﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public abstract class TokenizerTestBase<TTokenizerArg> where TTokenizerArg : class
{
    internal abstract object IgnoreRemaining { get; }
    internal abstract object CreateTokenizer(SeekableTextReader source, TTokenizerArg tokenizerArg);
    internal abstract TTokenizerArg DefaultTokenizerArg { get; }

    internal void TestTokenizer(string input, params SyntaxToken[] expectedSymbols)
    {
        TestTokenizer(input, DefaultTokenizerArg, expectedSymbols);
    }

    internal void TestTokenizer(string input, TTokenizerArg tokenizerArg, params SyntaxToken[] expectedSymbols)
    {
        // Arrange
        var success = true;
        var output = new StringBuilder();
        using (var source = new SeekableTextReader(input, filePath: null))
        {
            var tokenizer = (Tokenizer)CreateTokenizer(source, tokenizerArg);
            var counter = 0;
            SyntaxToken current = null;
            while ((current = tokenizer.NextToken()) != null)
            {
                if (counter >= expectedSymbols.Length)
                {
                    output.AppendLine(string.Format(CultureInfo.InvariantCulture, "F: Expected: << Nothing >>; Actual: {0}", current));
                    success = false;
                }
                else if (ReferenceEquals(expectedSymbols[counter], IgnoreRemaining))
                {
                    output.AppendLine(string.Format(CultureInfo.InvariantCulture, "P: Ignored |{0}|", current));
                }
                else
                {
                    if (!expectedSymbols[counter].IsEquivalentTo(current))
                    {
                        output.AppendLine(string.Format(CultureInfo.InvariantCulture, "F: Expected: {0}; Actual: {1}", expectedSymbols[counter], current));
                        success = false;
                    }
                    else
                    {
                        output.AppendLine(string.Format(CultureInfo.InvariantCulture, "P: Expected: {0}", expectedSymbols[counter]));
                    }
                    counter++;
                }
            }
            if (counter < expectedSymbols.Length && !ReferenceEquals(expectedSymbols[counter], IgnoreRemaining))
            {
                success = false;
                for (; counter < expectedSymbols.Length; counter++)
                {
                    output.AppendLine(string.Format(CultureInfo.InvariantCulture, "F: Expected: {0}; Actual: << None >>", expectedSymbols[counter]));
                }
            }
        }
        Assert.True(success, Environment.NewLine + output.ToString());
        WriteTraceLine(output.Replace("{", "{{").Replace("}", "}}").ToString());
    }

    [Conditional("PARSER_TRACE")]
    private static void WriteTraceLine(string format, params object[] args)
    {
        Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, format, args));
    }
}
