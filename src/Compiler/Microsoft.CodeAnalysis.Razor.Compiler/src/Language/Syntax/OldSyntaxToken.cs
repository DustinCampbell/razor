// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal class OldSyntaxToken : SyntaxNode
{
    internal static readonly Func<OldSyntaxToken, bool> NonZeroWidth = t => t.Width > 0;
    internal static readonly Func<OldSyntaxToken, bool> Any = t => true;

    internal OldSyntaxToken(GreenNode green, SyntaxNode parent, int position)
        : base(green, parent, position)
    {
    }

    internal override string SerializedValue => Serializer.Serialize(this);

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

    protected internal override SyntaxNode ReplaceCore<TNode>(
        IEnumerable<TNode>? nodes = null,
        Func<TNode, TNode, SyntaxNode>? computeReplacementNode = null,
        IEnumerable<OldSyntaxToken>? tokens = null,
        Func<OldSyntaxToken, OldSyntaxToken, OldSyntaxToken>? computeReplacementToken = null)
        => Assumed.Unreachable<SyntaxNode>();

    protected internal override SyntaxNode ReplaceNodeInListCore(SyntaxNode originalNode, IEnumerable<SyntaxNode> replacementNodes)
        => Assumed.Unreachable<SyntaxNode>();

    protected internal override SyntaxNode InsertNodesInListCore(SyntaxNode nodeInList, IEnumerable<SyntaxNode> nodesToInsert, bool insertBefore)
        => Assumed.Unreachable<SyntaxNode>();

    protected internal override SyntaxNode ReplaceTokenInListCore(OldSyntaxToken originalToken, IEnumerable<OldSyntaxToken> newTokens)
        => Assumed.Unreachable<SyntaxNode>();

    protected internal override SyntaxNode InsertTokensInListCore(OldSyntaxToken originalToken, IEnumerable<OldSyntaxToken> newTokens, bool insertBefore)
        => Assumed.Unreachable<SyntaxNode>();

    /// <summary>
    /// Gets the token that follows this token in the syntax tree.
    /// </summary>
    /// <returns>The token that follows this token in the syntax tree.</returns>
    public OldSyntaxToken? GetNextToken(bool includeZeroWidth = false)
    {
        return SyntaxNavigator.GetNextToken(this, includeZeroWidth);
    }

    /// <summary>
    /// Gets the token that precedes this token in the syntax tree.
    /// </summary>
    /// <returns>The previous token that precedes this token in the syntax tree.</returns>
    public OldSyntaxToken? GetPreviousToken(bool includeZeroWidth = false)
    {
        return SyntaxNavigator.GetPreviousToken(this, includeZeroWidth);
    }

    public override string ToString()
    {
        return Content;
    }
}
