﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LegacyEditor.Razor.Indentation;

internal static class RazorIndentationFacts
{
    // This method dives down a syntax tree looking for open curly braces, every time
    // it finds one it increments its indent until it finds the provided "line".
    //
    // Examples:
    // @{
    //    <strong>Hello World</strong>
    // }
    // Asking for desired indentation of the @{ or } lines should result in a desired indentation of 4.
    //
    // <div>
    //     @{
    //         <strong>Hello World</strong>
    //     }
    // </div>
    // Asking for desired indentation of the @{ or } lines should result in a desired indentation of 8.
    public static int? GetDesiredIndentation(
        RazorSyntaxTree syntaxTree,
        ITextSnapshot syntaxTreeSnapshot,
        ITextSnapshotLine line,
        int indentSize,
        int tabSize)
    {
        Debug.Assert(syntaxTreeSnapshot.TextBuffer.IsLegacyCoreRazorBuffer());
        if (syntaxTree is null)
        {
            throw new ArgumentNullException(nameof(syntaxTree));
        }

        if (syntaxTreeSnapshot is null)
        {
            throw new ArgumentNullException(nameof(syntaxTreeSnapshot));
        }

        if (line is null)
        {
            throw new ArgumentNullException(nameof(line));
        }

        if (indentSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(indentSize));
        }

        if (tabSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tabSize));
        }

        var previousLineEndIndex = GetPreviousLineEndIndex(syntaxTreeSnapshot, line);
        var simulatedChange = new SourceChange(previousLineEndIndex, 0, string.Empty);
#pragma warning disable CS0618 // Type or member is obsolete, RazorIndentationFacts is only used in legacy scenarios
        var owner = syntaxTree.Root.LocateOwner(simulatedChange);
#pragma warning restore CS0618 // Type or member is obsolete
        if (owner is null || owner.IsCodeSpanKind())
        {
            // Example,
            // @{\n
            //   ^  - The newline here is a code span and we should just let the default c# editor take care of indentation.

            return null;
        }

        int? desiredIndentation = null;
        while (owner.Parent is not null)
        {
            foreach (var currentChild in owner.Parent.ChildNodes())
            {
                if (IsCSharpOpenCurlyBrace(currentChild))
                {
                    var location = currentChild.GetSourceLocation(syntaxTree.Source);
                    var lineText = line.Snapshot.GetLineFromLineNumber(location.LineIndex).GetText();
                    desiredIndentation = GetIndentLevelOfLine(lineText, tabSize) + indentSize;
                }

                if (currentChild == owner)
                {
                    break;
                }
            }

            if (desiredIndentation.HasValue)
            {
                return desiredIndentation;
            }

            owner = owner.Parent;
        }

        // Couldn't determine indentation
        return null;
    }

    // Internal for testing
    internal static int GetIndentLevelOfLine(string line, int tabSize)
    {
        var indentLevel = 0;

        foreach (var c in line)
        {
            if (!char.IsWhiteSpace(c))
            {
                break;
            }
            else if (c == '\t')
            {
                indentLevel += tabSize;
            }
            else
            {
                indentLevel++;
            }
        }

        return indentLevel;
    }

    // Internal for testing
    internal static int GetPreviousLineEndIndex(ITextSnapshot syntaxTreeSnapshot, ITextSnapshotLine line)
    {
        var previousLine = line.Snapshot.GetLineFromLineNumber(line.LineNumber - 1);
        var trackingPoint = previousLine.Snapshot.CreateTrackingPoint(previousLine.End, PointTrackingMode.Negative);
        var previousLineEndIndex = trackingPoint.GetPosition(syntaxTreeSnapshot);

        return previousLineEndIndex;
    }

    // Internal for testing
    internal static bool IsCSharpOpenCurlyBrace(SyntaxNode node)
    {
        return node.ChildNodesAndTokens() is [{ IsToken: true, Kind: SyntaxKind.LeftBrace }];
    }
}
