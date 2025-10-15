// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal partial class SyntaxFactory
{
    public static CSharpExpressionLiteralSyntax CSharpExpressionLiteral(SyntaxToken token)
        => CSharpExpressionLiteral(token, chunkGenerator: null, editHandler: null);

    public static CSharpExpressionLiteralSyntax CSharpExpressionLiteral(SyntaxTokenList literalTokens)
        => CSharpExpressionLiteral(literalTokens, chunkGenerator: null, editHandler: null);

    public static CSharpTransitionSyntax CSharpTransition(SyntaxToken transition)
        => CSharpTransition(transition, chunkGenerator: null, editHandler: null);

    public static MarkupTagHelperEndTagSyntax MarkupTagHelperEndTag(
        SyntaxToken openAngle,
        SyntaxToken forwardSlash,
        SyntaxToken bang,
        SyntaxToken name,
        MarkupMiscAttributeContentSyntax miscAttributeContent,
        SyntaxToken closeAngle)
        => MarkupTagHelperEndTag(openAngle, forwardSlash, bang, name, miscAttributeContent, closeAngle, chunkGenerator: null, editHandler: null);

    public static MarkupTextLiteralSyntax MarkupTextLiteral(SyntaxToken token)
        => MarkupTextLiteral(token, chunkGenerator: null, editHandler: null);

    public static MarkupTextLiteralSyntax MarkupTextLiteral(SyntaxTokenList literalTokens)
        => MarkupTextLiteral(literalTokens, chunkGenerator: null, editHandler: null);

    public static RazorMetaCodeSyntax RazorMetaCode(SyntaxToken token)
        => RazorMetaCode(token, chunkGenerator: null, editHandler: null);

    public static RazorMetaCodeSyntax RazorMetaCode(SyntaxTokenList metaCode)
        => RazorMetaCode(metaCode, chunkGenerator: null, editHandler: null);
}
