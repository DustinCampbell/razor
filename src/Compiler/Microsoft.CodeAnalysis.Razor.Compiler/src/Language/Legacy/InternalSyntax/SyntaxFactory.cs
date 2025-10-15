// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

internal partial class SyntaxFactory
{
    public static CSharpExpressionLiteralSyntax CSharpExpressionLiteral(SyntaxToken token)
        => CSharpExpressionLiteral(token, chunkGenerator: null, editHandler: null);

    public static CSharpExpressionLiteralSyntax CSharpExpressionLiteral(SyntaxList<SyntaxToken> literalTokens)
        => CSharpExpressionLiteral(literalTokens, chunkGenerator: null, editHandler: null);

    public static CSharpTransitionSyntax CSharpTransition(SyntaxToken transition)
        => CSharpTransition(transition, chunkGenerator: null, editHandler: null);

    public static MarkupTextLiteralSyntax MarkupTextLiteral(SyntaxToken token)
        => MarkupTextLiteral(token, chunkGenerator: null, editHandler: null);

    public static MarkupTextLiteralSyntax MarkupTextLiteral(SyntaxList<SyntaxToken> literalTokens)
        => MarkupTextLiteral(literalTokens, chunkGenerator: null, editHandler: null);

    public static RazorMetaCodeSyntax RazorMetaCode(SyntaxToken token)
        => RazorMetaCode(token, chunkGenerator: null, editHandler: null);

    public static RazorMetaCodeSyntax RazorMetaCode(SyntaxList<SyntaxToken> metaCode)
        => RazorMetaCode(metaCode, chunkGenerator: null, editHandler: null);
}
