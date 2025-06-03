// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

internal enum SyntaxKind : byte
{
    None,
    List,

    // Tokens with well-known text

    // Common
    Colon,
    QuestionMark,
    RightBracket,
    LeftBracket,
    Transition,

    // HTML
    OpenAngle,
    Bang,
    ForwardSlash,
    DoubleHyphen,
    CloseAngle,
    DoubleQuote,
    SingleQuote,

    // C# operators
    Arrow,
    Minus,
    Decrement,
    MinusAssign,
    NotEqual,
    Not,
    Modulo,
    ModuloAssign,
    AndAssign,
    And,
    DoubleAnd,
    LeftParenthesis,
    RightParenthesis,
    Star,
    MultiplyAssign,
    Comma,
    Dot,
    Slash,
    DivideAssign,
    DoubleColon,
    Semicolon,
    NullCoalesce,
    XorAssign,
    Xor,
    LeftBrace,
    OrAssign,
    DoubleOr,
    Or,
    RightBrace,
    Tilde,
    Plus,
    PlusAssign,
    Increment,
    LessThan,
    LessThanEqual,
    LeftShift,
    LeftShiftAssign,
    Assign,
    GreaterThan,
    GreaterThanEqual,
    EqualsGreaterThan,
    RightShift,
    RightShiftAssign,
    Hash,

    // Razor
    RazorCommentStar,
    RazorCommentTransition,

    EndOfFile,

    #region Other tokens

    // Common
    Marker,
    Whitespace,
    NewLine,
    Equals,

    // HTML
    Text,

    // CSharp literals
    Identifier,
    Keyword,
    IntegerLiteral,
    NumericLiteral,
    CSharpComment,
    RealLiteral,
    CharacterLiteral,
    StringLiteral,
    CSharpDirective,
    CSharpDisabledText,

    // CSharp operators
    CSharpOperator,

    // Razor specific
    RazorCommentLiteral,

    #endregion

    #region Nodes

    // Common
    RazorDocument,
    GenericBlock,
    RazorComment,
    RazorMetaCode,
    RazorDirective,
    RazorDirectiveBody,
    UnclassifiedTextLiteral,

    // Markup
    MarkupBlock,
    MarkupTransition,
    MarkupElement,
    MarkupStartTag,
    MarkupEndTag,
    MarkupTagBlock,
    MarkupTextLiteral,
    MarkupEphemeralTextLiteral,
    MarkupCommentBlock,
    MarkupAttributeBlock,
    MarkupMinimizedAttributeBlock,
    MarkupMiscAttributeContent,
    MarkupLiteralAttributeValue,
    MarkupDynamicAttributeValue,
    MarkupTagHelperElement,
    MarkupTagHelperStartTag,
    MarkupTagHelperEndTag,
    MarkupTagHelperAttribute,
    MarkupMinimizedTagHelperAttribute,
    MarkupTagHelperDirectiveAttribute,
    MarkupMinimizedTagHelperDirectiveAttribute,
    MarkupTagHelperAttributeValue,

    // CSharp
    CSharpStatement,
    CSharpStatementBody,
    CSharpExplicitExpression,
    CSharpExplicitExpressionBody,
    CSharpImplicitExpression,
    CSharpImplicitExpressionBody,
    CSharpCodeBlock,
    CSharpTemplateBlock,
    CSharpStatementLiteral,
    CSharpExpressionLiteral,
    CSharpEphemeralTextLiteral,
    CSharpTransition,
    #endregion

    // New nodes should go before this one

    FirstAvailableTokenKind,
}
