// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

[CollectionBuilder(typeof(SyntaxNodeOrTokenList), "Create")]
internal readonly partial struct SyntaxNodeOrTokenList : IEquatable<SyntaxNodeOrTokenList>, IReadOnlyList<SyntaxNodeOrToken>
{
    private readonly SyntaxNode? _node;

    internal SyntaxNodeOrTokenList(SyntaxNode? node)
        : this()
    {
        _node = node;
    }

    public SyntaxNodeOrTokenList(IEnumerable<SyntaxNodeOrToken> nodesAndTokens)
        : this(CreateNode(nodesAndTokens))
    {
    }

    public static SyntaxNodeOrTokenList Create(ReadOnlySpan<SyntaxNodeOrToken> nodesAndTokens)
    {
        if (nodesAndTokens.IsEmpty)
        {
            return default;
        }

        return new(CreateNodeFromSpan(nodesAndTokens));
    }

    private static SyntaxNode? CreateNodeFromSpan(ReadOnlySpan<SyntaxNodeOrToken> nodesAndTokens)
    {
        Debug.Assert(!nodesAndTokens.IsEmpty);

        switch (nodesAndTokens.Length)
        {
            case 0:
                return null;

            case 1:
                return nodesAndTokens[0].IsToken
                    ? InternalSyntax.SyntaxList.List([nodesAndTokens[0].UnderlyingNode!]).CreateRed()
                    : nodesAndTokens[0].AsNode();

            case 2:
                return InternalSyntax.SyntaxList
                    .List(nodesAndTokens[0].UnderlyingNode!, nodesAndTokens[1].UnderlyingNode!)
                    .CreateRed();

            case 3:
                return InternalSyntax.SyntaxList
                    .List(nodesAndTokens[0].UnderlyingNode!, nodesAndTokens[1].UnderlyingNode!, nodesAndTokens[2].UnderlyingNode!)
                    .CreateRed();

            default:
                {
                    var copy = new ArrayElement<GreenNode>[nodesAndTokens.Length];
                    for (var i = 0; i < nodesAndTokens.Length; i++)
                    {
                        copy[i].Value = nodesAndTokens[i].UnderlyingNode!;
                    }

                    return InternalSyntax.SyntaxList.List(copy).CreateRed();
                }
        }
    }

    private static SyntaxNode? CreateNode(IEnumerable<SyntaxNodeOrToken> nodesAndTokens)
    {
        ArgHelper.ThrowIfNull(nodesAndTokens);

        var initialCapacity = nodesAndTokens.TryGetCount(out var count) ? count : 4;

        using var builder = new MemoryBuilder<SyntaxNodeOrToken>(initialCapacity);

        foreach (var nodeOrToken in nodesAndTokens)
        {
            builder.Append(nodeOrToken);
        }

        return CreateNodeFromSpan(builder.AsMemory().Span);
    }

    internal SyntaxNode? Node => _node;
    internal int Position => _node?.Position ?? 0;
    internal SyntaxNode? Parent => _node?.Parent;

    public int Count => _node == null ? 0 : _node.Green.IsList ? _node.SlotCount : 1;

    public SyntaxNodeOrToken this[int index]
    {
        get
        {
            if (_node != null)
            {
                if (!_node.IsList)
                {
                    if (index == 0)
                    {
                        return _node;
                    }
                }
                else
                {
                    if (unchecked((uint)index < (uint)_node.SlotCount))
                    {
                        var green = _node.Green.GetSlot(index);
                        Debug.Assert(green != null);

                        if (green.IsToken)
                        {
                            return new SyntaxToken(green, _node.Parent, _node.GetChildPosition(index));
                        }

                        return _node.GetNodeSlot(index);
                    }
                }
            }

            return ThrowHelper.ThrowArgumentOutOfRangeException<SyntaxNodeOrToken>(nameof(index));
        }
    }

    public TextSpan Span => _node?.Span ?? default;

    public override string ToString()
        => _node?.ToString() ?? string.Empty;

    public Enumerator GetEnumerator()
        => new(in this);

    IEnumerator<SyntaxNodeOrToken> IEnumerable<SyntaxNodeOrToken>.GetEnumerator()
        => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is SyntaxNodeOrTokenList other &&
           Equals(other);

    public bool Equals(SyntaxNodeOrTokenList other)
        => _node == other._node;

    public override int GetHashCode()
        => _node?.GetHashCode() ?? 0;

    public static bool operator ==(SyntaxNodeOrTokenList left, SyntaxNodeOrTokenList right)
        => left.Equals(right);

    public static bool operator !=(SyntaxNodeOrTokenList left, SyntaxNodeOrTokenList right)
        => !left.Equals(right);
}
