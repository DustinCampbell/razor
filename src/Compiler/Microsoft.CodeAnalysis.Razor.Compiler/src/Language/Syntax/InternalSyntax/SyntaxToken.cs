// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

internal class SyntaxToken : RazorSyntaxNode
{
    internal const SyntaxKind FirstTokenWithWellKnownText = SyntaxKind.Colon;
    internal const SyntaxKind LastTokenWithWellKnownText = SyntaxKind.EndOfFile;

    private static readonly ArrayElement<SyntaxToken>[] s_tokens = new ArrayElement<SyntaxToken>[(int)LastTokenWithWellKnownText + 1];

    static SyntaxToken()
    {
        for (var kind = FirstTokenWithWellKnownText; kind <= LastTokenWithWellKnownText; kind++)
        {
            s_tokens[(int)kind].Value = new(kind, RazorSyntaxFacts.GetText(kind));
        }
    }

    public static SyntaxToken Create(SyntaxKind kind)
    {
        Debug.Assert(kind >= FirstTokenWithWellKnownText && kind <= LastTokenWithWellKnownText);

        return s_tokens[(int)kind].Value;
    }

    internal SyntaxToken(
        SyntaxKind kind,
        string content,
        RazorDiagnostic[] diagnostics = null,
        SyntaxAnnotation[] annotations = null)
        : base(kind, content.Length, diagnostics, annotations)
    {
        Content = content;
    }

    public string Content { get; }

    internal override bool IsToken => true;

    internal override SyntaxNode CreateRed(SyntaxNode parent, int position)
    {
        return Assumed.Unreachable<SyntaxNode>();
    }

    protected override void WriteTokenTo(TextWriter writer)
    {
        writer.Write(Content);
    }

    internal override GreenNode SetDiagnostics(RazorDiagnostic[] diagnostics)
    {
        return new SyntaxToken(Kind, Content, diagnostics, GetAnnotations());
    }

    internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
    {
        return new SyntaxToken(Kind, Content, GetDiagnostics(), annotations);
    }

    protected sealed override int GetSlotCount()
    {
        return 0;
    }

    internal sealed override GreenNode GetSlot(int index)
    {
        throw new InvalidOperationException("Tokens don't have slots.");
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.VisitToken(this);
    }

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitToken(this);
    }

    public override bool IsEquivalentTo(GreenNode other)
    {
        if (!base.IsEquivalentTo(other))
        {
            return false;
        }

        var otherToken = (SyntaxToken)other;

        if (Content != otherToken.Content)
        {
            return false;
        }

        return true;
    }

    public override string ToString()
    {
        return Content;
    }

    internal static SyntaxToken CreateMissing(SyntaxKind kind, params RazorDiagnostic[] diagnostics)
    {
        return new MissingToken(kind, diagnostics);
    }

    private class MissingToken : SyntaxToken
    {
        internal MissingToken(SyntaxKind kind, RazorDiagnostic[] diagnostics)
            : base(kind, string.Empty, diagnostics)
        {
            Flags |= NodeFlags.IsMissing;
        }
    }
}
