// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class SyntaxTokenExtensions
{
    public static SyntaxToken WithAnnotations(this SyntaxToken token, params SyntaxAnnotation[] annotations)
    {
        return new(parent: null, token.RequiredNode.SetAnnotations(annotations), position: 0, index: 0);
    }

    public static object? GetAnnotationValue(this SyntaxToken token, string key)
    {
        if (!token.ContainsAnnotations)
        {
            return null;
        }

        var annotations = token.GetAnnotations();
        foreach (var annotation in annotations)
        {
            if (annotation.Kind == key)
            {
                return annotation.Data;
            }
        }

        return null;
    }

    public static SourceLocation GetSourceLocation(this SyntaxToken token, RazorSourceDocument source)
    {
        try
        {
            if (source.Text.Length == 0)
            {
                // Just a marker symbol
                return new SourceLocation(source.FilePath, absoluteIndex: 0, lineIndex: 0, characterIndex: 0);
            }

            if (token.Position == source.Text.Length)
            {
                // E.g. Marker symbol at the end of the document
                var lastPosition = source.Text.Length - 1;
                var endsWithLineBreak = SyntaxFacts.IsNewLine(source.Text[lastPosition]);
                var lastLocation = source.Text.Lines.GetLinePosition(lastPosition);

                return new SourceLocation(
                    source.FilePath, // GetLocation prefers RelativePath but we want FilePath.
                    absoluteIndex: lastPosition + 1,
                    lineIndex: lastLocation.Line + (endsWithLineBreak ? 1 : 0),
                    characterIndex: endsWithLineBreak ? 0 : lastLocation.Character + 1);
            }

            var location = source.Text.Lines.GetLinePosition(token.Position);

            return new SourceLocation(
                source.FilePath, // GetLocation prefers RelativePath but we want FilePath.
                absoluteIndex: token.Position,
                linePosition: location);
        }
        catch (IndexOutOfRangeException)
        {
            Debug.Assert(false, "Node position should stay within document length.");
            return new SourceLocation(source.FilePath, absoluteIndex: token.Position, lineIndex: 0, characterIndex: 0);
        }
    }

    public static SourceSpan GetSourceSpan(this SyntaxToken token, RazorSourceDocument source)
    {
        var location = token.GetSourceLocation(source);
        var endLocation = source.Text.Lines.GetLinePosition(token.EndPosition);
        var lineCount = endLocation.Line - location.LineIndex;

        return new SourceSpan(location.FilePath, location.AbsoluteIndex, location.LineIndex, location.CharacterIndex, token.Width, lineCount, endLocation.Character);
    }
}
