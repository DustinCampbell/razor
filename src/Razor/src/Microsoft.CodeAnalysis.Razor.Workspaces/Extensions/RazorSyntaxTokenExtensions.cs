// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class RazorSyntaxTokenExtensions
{
    public static bool IsWhitespace(this SyntaxToken token)
        => token.Kind is SyntaxKind.Whitespace or SyntaxKind.NewLine;

    public static bool IsSpace(this SyntaxToken token)
        => token.Kind == SyntaxKind.Whitespace && token.Content == " ";

    public static bool IsTab(this SyntaxToken token)
        => token.Kind == SyntaxKind.Whitespace && token.Content == "\t";

    public static LinePositionSpan GetLinePositionSpan(this SyntaxToken token, RazorSourceDocument sourceDocument)
    {
        var start = token.Position;
        var end = token.EndPosition;
        var sourceText = sourceDocument.Text;

        Debug.Assert(start <= sourceText.Length && end <= sourceText.Length, "Node position exceeds source length.");

        if (start == sourceText.Length && token.Width == 0)
        {
            // Marker symbol at the end of the document.
            var location = token.GetSourceLocation(sourceDocument);

            return location.ToLinePosition().ToZeroWidthSpan();
        }

        return sourceText.GetLinePositionSpan(start, end);
    }
}
