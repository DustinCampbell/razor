// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal abstract partial class SyntaxRewriter : SyntaxVisitor<SyntaxNode?>
{
    private int _recursionDepth;

    [return: NotNullIfNotNull(nameof(node))]
    public override SyntaxNode? Visit(SyntaxNode? node)
    {
        if (node != null)
        {
            _recursionDepth++;
            StackGuard.EnsureSufficientExecutionStack(_recursionDepth);

            var result = ((RazorSyntaxNode)node).Accept(this);

            _recursionDepth--;
            return result.AssumeNotNull();
        }
        else
        {
            return null;
        }
    }

    public virtual SyntaxToken VisitToken(SyntaxToken token)
    {
        return token;
    }

    public virtual SyntaxTokenList VisitList(SyntaxTokenList list)
    {
        var builder = PooledArrayBuilder<SyntaxToken>.Empty;
        var updating = false;

        var count = list.Count;
        var index = -1;

        foreach (var item in list)
        {
            index++;

            var visited = VisitToken(item);

            if (item != visited && !updating)
            {
                builder = new PooledArrayBuilder<SyntaxToken>(count);

                for (var i = 0; i < index; i++)
                {
                    builder.Add(list[i]);
                }

                updating = true;
            }

            if (updating && visited.Kind != SyntaxKind.None)
            {
                builder.Add(visited);
            }
        }

        if (updating)
        {
            var result = builder.ToList();
            builder.Dispose();

            return result;
        }

        return list;
    }

    public virtual SyntaxList<TNode> VisitList<TNode>(SyntaxList<TNode> list)
        where TNode : SyntaxNode
    {
        var builder = PooledArrayBuilder<TNode>.Empty;
        var updating = false;

        var count = list.Count;
        var index = -1;

        foreach (var item in list)
        {
            index++;

            var visited = VisitListElement(item);

            if (item != visited && !updating)
            {
                builder = new PooledArrayBuilder<TNode>(count);

                for (var i = 0; i < index; i++)
                {
                    builder.Add(list[i]);
                }

                updating = true;
            }

            if (updating && visited != null && visited.Kind != SyntaxKind.None)
            {
                builder.Add(visited);
            }
        }

        if (updating)
        {
            var result = builder.ToList();
            builder.Dispose();

            return result;
        }

        return list;
    }

    public virtual TNode? VisitListElement<TNode>(TNode? node)
        where TNode : SyntaxNode
    {
        return (TNode?)Visit(node);
    }
}
