// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class RazorSyntaxFacts
{
    public static string GetText(SyntaxKind kind)
    {
        switch (kind)
        {
            case SyntaxKind.Colon:
                return ":";
            case SyntaxKind.QuestionMark:
                return "?";
            case SyntaxKind.RightBracket:
                return "]";
            case SyntaxKind.LeftBracket:
                return "[";
            case SyntaxKind.Transition:
                return "@";
            case SyntaxKind.OpenAngle:
                return "<";
            case SyntaxKind.Bang:
                return "!";
            case SyntaxKind.ForwardSlash:
                return "/";
            case SyntaxKind.DoubleHyphen:
                return "--";
            case SyntaxKind.CloseAngle:
                return ">";
            case SyntaxKind.DoubleQuote:
                return "\"";
            case SyntaxKind.SingleQuote:
                return "'";

            case SyntaxKind.Arrow:
                return "->";
            case SyntaxKind.Minus:
                return "-";
            case SyntaxKind.Decrement:
                return "--";
            case SyntaxKind.MinusAssign:
                return "-=";
            case SyntaxKind.NotEqual:
                return "!=";
            case SyntaxKind.Not:
                return "!";
            case SyntaxKind.Modulo:
                return "%";
            case SyntaxKind.ModuloAssign:
                return "%=";
            case SyntaxKind.AndAssign:
                return "&=";
            case SyntaxKind.And:
                return "&";
            case SyntaxKind.DoubleAnd:
                return "&&";
            case SyntaxKind.LeftParenthesis:
                return "(";
            case SyntaxKind.RightParenthesis:
                return ")";
            case SyntaxKind.Star:
                return "*";
            case SyntaxKind.MultiplyAssign:
                return "*=";
            case SyntaxKind.Comma:
                return ",";
            case SyntaxKind.Dot:
                return ".";
            case SyntaxKind.Slash:
                return "/";
            case SyntaxKind.DivideAssign:
                return "/=";
            case SyntaxKind.DoubleColon:
                return "::";
            case SyntaxKind.Semicolon:
                return ";";
            case SyntaxKind.NullCoalesce:
                return "??";
            case SyntaxKind.XorAssign:
                return "^=";
            case SyntaxKind.Xor:
                return "^";
            case SyntaxKind.LeftBrace:
                return "{";
            case SyntaxKind.OrAssign:
                return "|=";
            case SyntaxKind.DoubleOr:
                return "||";
            case SyntaxKind.Or:
                return "|";
            case SyntaxKind.RightBrace:
                return "}";
            case SyntaxKind.Tilde:
                return "~";
            case SyntaxKind.Plus:
                return "+";
            case SyntaxKind.PlusAssign:
                return "+=";
            case SyntaxKind.Increment:
                return "++";
            case SyntaxKind.LessThan:
                return "<";
            case SyntaxKind.LessThanEqual:
                return "<=";
            case SyntaxKind.LeftShift:
                return "<<";
            case SyntaxKind.LeftShiftAssign:
                return "<<=";
            case SyntaxKind.Assign:
                return "=";
            case SyntaxKind.GreaterThan:
                return ">";
            case SyntaxKind.GreaterThanEqual:
                return ">=";
            case SyntaxKind.EqualsGreaterThan:
                return "=>";
            case SyntaxKind.RightShift:
                return ">>";
            case SyntaxKind.RightShiftAssign:
                return ">>=";
            case SyntaxKind.Hash:
                return "#";
            case SyntaxKind.RazorCommentStar:
                return "*";
            case SyntaxKind.RazorCommentTransition:
                return "@";

            default:
                return string.Empty;
        }
    }
}
