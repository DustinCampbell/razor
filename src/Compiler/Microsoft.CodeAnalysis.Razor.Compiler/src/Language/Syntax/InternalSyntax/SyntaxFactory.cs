// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

internal static partial class SyntaxFactory
{
    internal static SyntaxToken Token(SyntaxKind kind)
    {
        return SyntaxToken.Create(kind);
    }

    internal static SyntaxToken Token(SyntaxKind kind, string content, params RazorDiagnostic[] diagnostics)
    {
        if (kind >= SyntaxToken.FirstTokenWithWellKnownText &&
            kind <= SyntaxToken.LastTokenWithWellKnownText &&
            diagnostics?.Length is not > 0)
        {
            var token = SyntaxToken.Create(kind);
            Debug.Assert(token.Content == content);

            return token;
        }

        if (SyntaxTokenCache.Instance.CanBeCached(kind, diagnostics))
        {
            return SyntaxTokenCache.Instance.GetCachedToken(kind, content);
        }

        return new SyntaxToken(kind, content, diagnostics);
    }

    internal static SyntaxToken MissingToken(SyntaxKind kind, params RazorDiagnostic[] diagnostics)
    {
        return SyntaxToken.CreateMissing(kind, diagnostics);
    }
}
