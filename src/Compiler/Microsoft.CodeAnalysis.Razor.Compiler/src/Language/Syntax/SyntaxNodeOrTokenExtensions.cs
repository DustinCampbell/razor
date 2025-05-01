// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class SyntaxNodeOrTokenExtensions
{
    public static SourceLocation GetSourceLocation(this SyntaxNodeOrToken nodeOrToken, RazorSourceDocument source)
    {
        try
        {
            if (source.Text.Length == 0)
            {
                // Just a marker symbol
                return new SourceLocation(source.FilePath, 0, 0, 0);
            }
            if (nodeOrToken.Position == source.Text.Length)
            {
                // E.g. Marker symbol at the end of the document
                var lastPosition = source.Text.Length - 1;
                var endsWithLineBreak = SyntaxFacts.IsNewLine(source.Text[lastPosition]);
                var lastLocation = source.Text.Lines.GetLinePosition(lastPosition);
                return new SourceLocation(
                    source.FilePath, // GetLocation prefers RelativePath but we want FilePath.
                    lastPosition + 1,
                    lastLocation.Line + (endsWithLineBreak ? 1 : 0),
                    endsWithLineBreak ? 0 : lastLocation.Character + 1);
            }

            var location = source.Text.Lines.GetLinePosition(nodeOrToken.Position);
            return new SourceLocation(
                source.FilePath, // GetLocation prefers RelativePath but we want FilePath.
                nodeOrToken.Position,
                location);
        }
        catch (IndexOutOfRangeException)
        {
            Debug.Assert(false, "Node position should stay within document length.");
            return new SourceLocation(source.FilePath, nodeOrToken.Position, 0, 0);
        }
    }
}
