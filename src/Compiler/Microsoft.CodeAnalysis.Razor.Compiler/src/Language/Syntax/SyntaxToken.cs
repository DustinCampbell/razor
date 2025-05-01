// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal readonly struct SyntaxToken : IEquatable<SyntaxToken>
{
    internal static readonly Func<SyntaxToken, bool> NonZeroWidth = t => t.Width > 0;
    internal static readonly Func<SyntaxToken, bool> Any = t => true;

    public GreenNode? Node { get; }
    public SyntaxNode? Parent { get; }

    internal int Position { get; }
    internal int Index { get; }

    internal SyntaxToken(GreenNode? token, SyntaxNode? parent, int position, int index)
    {
        Debug.Assert(parent == null || !parent.Green.IsList, "list cannot be a parent");
        Debug.Assert(token == null || token.IsToken, "token must be a token");

        Node = token;
        Parent = parent;
        Position = position;
        Index = index;
    }

    public SyntaxKind Kind => Node?.Kind ?? 0;

    internal GreenNode RequiredNode
    {
        get
        {
            Debug.Assert(Node != null);
            return Node;
        }
    }

    internal int Width => Node?.Width ?? 0;

    public int SpanStart
        => Node != null ? Position : 0;

    public TextSpan Span
        => Node != null ? new(Position, Node.Width) : default;

    internal int EndPosition
        => Node != null ? Position + Node.Width : default;

    public bool IsMissing => Node?.IsMissing ?? false;

    public string Content => (Node as InternalSyntax.SyntaxToken)?.Content ?? string.Empty;

    /// <summary>
    /// Gets the token that follows this token in the syntax tree.
    /// </summary>
    /// <returns>The token that follows this token in the syntax tree.</returns>
    public SyntaxToken GetNextToken(bool includeZeroWidth = false)
    {
        return SyntaxNavigator.GetNextToken(this, includeZeroWidth);
    }

    /// <summary>
    /// Gets the token that precedes this token in the syntax tree.
    /// </summary>
    /// <returns>The previous token that precedes this token in the syntax tree.</returns>
    public SyntaxToken GetPreviousToken(bool includeZeroWidth = false)
    {
        return SyntaxNavigator.GetPreviousToken(this, includeZeroWidth);
    }

    public override string ToString()
        => Content;

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is SyntaxToken other &&
           Equals(other);

    public bool Equals(SyntaxToken other)
    {
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public static bool operator ==(SyntaxToken left, SyntaxToken right)
        => left.Equals(right);

    public static bool operator !=(SyntaxToken left, SyntaxToken right)
        => !left.Equals(right);
}
