// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class SyntaxNodeOrTokenExtensions
{
    public static SourceLocation GetSourceLocation(this SyntaxNodeOrToken nodeOrToken, RazorSourceDocument source)
    {
        return nodeOrToken.IsNode
            ? nodeOrToken.AsNode()!.GetSourceLocation(source)
            : nodeOrToken.AsToken().GetSourceLocation(source);
    }
}
