// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal class OldSyntaxToken : RazorSyntaxNode
{
    internal static readonly Func<OldSyntaxToken, bool> NonZeroWidth = t => t.Width > 0;
    internal static readonly Func<OldSyntaxToken, bool> Any = t => true;

    internal OldSyntaxToken(GreenNode green, SyntaxNode parent, int position)
        : base(green, parent, position)
    {
    }

    internal new InternalSyntax.SyntaxToken Green => (InternalSyntax.SyntaxToken)base.Green;

    public string Content => Green.Content;

    internal sealed override SyntaxNode GetCachedSlot(int index)
    {
        throw new InvalidOperationException("Tokens can't have slots.");
    }

    internal sealed override SyntaxNode GetNodeSlot(int slot)
    {
        throw new InvalidOperationException("Tokens can't have slots.");
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
    {
        return visitor.VisitToken(this.AsToken());
    }

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitToken(this.AsToken());
    }

    /// <summary>
    /// Gets the token that follows this token in the syntax tree.
    /// </summary>
    /// <returns>The token that follows this token in the syntax tree.</returns>
    public OldSyntaxToken GetNextOldToken(bool includeZeroWidth = false)
    {
        return SyntaxNavigator.GetNextOldToken(this, includeZeroWidth);
    }

    /// <summary>
    /// Gets the token that precedes this token in the syntax tree.
    /// </summary>
    /// <returns>The previous token that precedes this token in the syntax tree.</returns>
    public OldSyntaxToken GetPreviousOldToken(bool includeZeroWidth = false)
    {
        return SyntaxNavigator.GetPreviousOldToken(this, includeZeroWidth);
    }

    public override string ToString()
    {
        return Content;
    }
}
