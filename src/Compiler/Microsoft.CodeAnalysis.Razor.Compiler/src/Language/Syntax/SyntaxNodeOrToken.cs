// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

[DebuggerDisplay($"{nameof(GetDebuggerDisplay)}(), nq")]
internal readonly struct SyntaxNodeOrToken : IEquatable<SyntaxNodeOrToken>
{
    private readonly SyntaxNode? _nodeOrParent;

    // Green node for this token.
    private readonly GreenNode? _token;

    private readonly int _position;

    private readonly int _tokenIndex;

    internal SyntaxNodeOrToken(SyntaxNode node)
        : this()
    {
        Debug.Assert(!node.Green.IsList, "node cannot be a list");

        _nodeOrParent = node;
        _position = node.Position;
        _tokenIndex = -1;
    }

    internal SyntaxNodeOrToken(SyntaxNode? parent, GreenNode? token, int position, int index)
    {
        Debug.Assert(parent == null || !parent.Green.IsList, "parent cannot be a list");
        Debug.Assert(token != null || (parent == null && position == 0 & index == 0), "parts must form a token");
        Debug.Assert(token == null || token.IsToken, "token must be a token");
        Debug.Assert(index >= 0, "index must not be negative");
        Debug.Assert(parent == null || token != null, "null token cannot have parent");

        _nodeOrParent = parent;
        _token = token;
        _position = position;
        _tokenIndex = index;
    }

    private string GetDebuggerDisplay()
        => GetType().Name + " " + Kind.ToString() + " " + ToString();

    public SyntaxKind Kind => _token?.Kind ?? _nodeOrParent?.Kind ?? 0;

    public bool IsMissing => _token?.IsMissing ?? _nodeOrParent?.IsMissing ?? false;

    public SyntaxNode? Parent => _token != null ? _nodeOrParent : _nodeOrParent?.Parent;

    internal GreenNode? UnderlyingNode => _token ?? _nodeOrParent?.Green;

    public int Position => _position;

    public bool IsToken => !IsNode;

    public bool IsNode => _tokenIndex < 0;

    public SyntaxToken AsToken()
        => _token != null ? new SyntaxToken(_token, _nodeOrParent, _position, _tokenIndex) : default;

    internal bool AsToken(out SyntaxToken token)
    {
        if (IsToken)
        {
            token = AsToken();
            return true;
        }

        token = default;
        return false;
    }

    public SyntaxNode? AsNode()
        => _token == null ? _nodeOrParent : null;

    internal bool AsNode([NotNullWhen(true)] out SyntaxNode? node)
    {
        if (IsNode)
        {
            node = _nodeOrParent;
            return node is not null;
        }

        node = null;
        return false;
    }

    public ChildSyntaxList ChildNodesAndTokens()
        => AsNode(out var node)
            ? node.ChildNodesAndTokens()
            : default;

    public TextSpan Span
    {
        get
        {
            if (_token != null)
            {
                // TODO: Remove ! once SyntaxToken is a struct.
                return AsToken()!.Span;
            }

            if (_nodeOrParent != null)
            {
                return _nodeOrParent.Span;
            }

            return default;
        }
    }

    public int SpanStart
    {
        get
        {
            if (_token != null)
            {
                // TODO: Remove ! once SyntaxToken is a struct.
                return AsToken()!.SpanStart;
            }

            if (_nodeOrParent != null)
            {
                return _nodeOrParent.SpanStart;
            }

            return default;
        }
    }

    public int Width  => _token?.Width ?? _nodeOrParent?.Width ?? 0;

    public int EndPosition => _position + Width;

    public bool ContainsDiagnostics
    {
        get
        {
            if (_token != null)
            {
                return _token.ContainsDiagnostics;
            }

            if (_nodeOrParent != null)
            {
                return _nodeOrParent.ContainsDiagnostics;
            }

            return false;
        }
    }

    public IEnumerable<RazorDiagnostic> GetDiagnostics()
    {
        if (_token != null)
        {
            return _token.GetDiagnostics();
        }

        if (_nodeOrParent != null)
        {
            return _nodeOrParent.GetDiagnostics();
        }

        return SpecializedCollections.EmptyEnumerable<RazorDiagnostic>();
    }

    public bool ContainsAnnotations
    {
        get
        {
            if (_token != null)
            {
                return _token.ContainsAnnotations;
            }

            if (_nodeOrParent != null)
            {
                return _nodeOrParent.ContainsAnnotations;
            }

            return false;
        }
    }

    public IEnumerable<SyntaxAnnotation> Annotations()
    {
        if (_token != null)
        {
            return _token.GetAnnotations();
        }

        if (_nodeOrParent != null)
        {
            return _nodeOrParent.GetAnnotations();
        }

        return SpecializedCollections.EmptyEnumerable<SyntaxAnnotation>();
    }

    public override string ToString()
    {
        if (_token != null)
        {
            return _token.ToString();
        }

        if (_nodeOrParent != null)
        {
            return _nodeOrParent.ToString();
        }

        return string.Empty;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is SyntaxNodeOrToken syntaxNodeOrToken &&
           Equals(syntaxNodeOrToken);

    public bool Equals(SyntaxNodeOrToken other)
    {
        // index replaces position to ensure equality.  Assert if offset affects equality.
        Debug.Assert(
            (_nodeOrParent == other._nodeOrParent && _token == other._token && _position == other._position && _tokenIndex == other._tokenIndex) ==
            (_nodeOrParent == other._nodeOrParent && _token == other._token && _tokenIndex == other._tokenIndex));

        return _nodeOrParent == other._nodeOrParent &&
               _token == other._token &&
               _tokenIndex == other._tokenIndex;
    }

    public override int GetHashCode()
    {
        var hash = HashCodeCombiner.Start();

        hash.Add(_nodeOrParent);
        hash.Add(_token);
        hash.Add(_tokenIndex);

        return hash.CombinedHash;
    }

    public static bool operator ==(SyntaxNodeOrToken left, SyntaxNodeOrToken right)
        => left.Equals(right);

    public static bool operator !=(SyntaxNodeOrToken left, SyntaxNodeOrToken right)
        => !left.Equals(right);

    public static implicit operator SyntaxNodeOrToken(SyntaxToken token)
        => new(token.Parent, token.Node, token.Position, token.Index);

    public static explicit operator SyntaxToken(SyntaxNodeOrToken nodeOrToken)
        => nodeOrToken.AsToken();

    public static implicit operator SyntaxNodeOrToken(SyntaxNode? node)
        => node is not null ? new(node) : default;

    public static explicit operator SyntaxNode?(SyntaxNodeOrToken nodeOrToken)
        => nodeOrToken.AsNode();
}
