// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class SyntaxTokenExtensions
{
    public static SourceLocation GetSourceLocation(this SyntaxToken token, RazorSourceDocument source)
        => ((SyntaxNodeOrToken)token).GetSourceLocation(source);

}
