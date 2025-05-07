// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class PooledArrayBuilderExtensions
{
    public static SyntaxTokenList ToList(ref readonly this PooledArrayBuilder<GreenNode> builder)
    {
        switch (builder.Count)
        {
            case 0:
                return default;

            case 1:
                return new(parent: null, builder[0], position: 0, index: 0);

            case 2:
                return new(parent: null, InternalSyntax.SyntaxList.List(builder[0], builder[1]), position: 0, index: 0);

            case 3:
                return new(parent: null, InternalSyntax.SyntaxList.List(builder[0], builder[1], builder[2]), position: 0, index: 0);

            default:
                var copy = new ArrayElement<GreenNode>[builder.Count];

                for (var i = 0; i < builder.Count; i++)
                {
                    copy[i].Value = builder[i];
                }

                return new(parent: null, InternalSyntax.SyntaxList.List(copy), position: 0, index: 0);
        }
    }

    public static SyntaxList<TNode> ToList<TNode>(ref readonly this PooledArrayBuilder<TNode> builder)
        where TNode : SyntaxNode
    {
        var listNode = builder.ToListNode();

        return listNode is null
            ? default
            : new(listNode.CreateRed());
    }

    private static GreenNode? ToListNode<TNode>(ref readonly this PooledArrayBuilder<TNode> builder)
        where TNode : SyntaxNode
    {
        var count = builder.Count;

        switch (count)
        {
            case 0:
                return null;
            case 1:
                return builder[0].Green;
            case 2:
                return InternalSyntax.SyntaxList.List(builder[0].Green, builder[1].Green);
            case 3:
                return InternalSyntax.SyntaxList.List(builder[0].Green, builder[1].Green, builder[2].Green);
            default:
                var copy = new ArrayElement<GreenNode>[count];
                for (var i = 0; i < count; i++)
                {
                    copy[i].Value = builder[i].Green;
                }

                return InternalSyntax.SyntaxList.List(copy);
        }
    }

    public static SyntaxTokenList ToList(ref readonly this PooledArrayBuilder<SyntaxToken> builder)
    {
        switch (builder.Count)
        {
            case 0:
                return default;

            case 1:
                return new(parent: null, builder[0].RequiredNode, position: 0, index: 0);

            case 2:
                return new(parent: null, InternalSyntax.SyntaxList.List(builder[0].RequiredNode, builder[1].RequiredNode), position: 0, index: 0);

            case 3:
                return new(parent: null, InternalSyntax.SyntaxList.List(builder[0].RequiredNode, builder[1].RequiredNode, builder[2].RequiredNode), position: 0, index: 0);

            default:
                var copy = new ArrayElement<GreenNode>[builder.Count];

                for (var i = 0; i < builder.Count; i++)
                {
                    copy[i].Value = builder[i].RequiredNode;
                }

                return new(parent: null, InternalSyntax.SyntaxList.List(copy), position: 0, index: 0);
        }
    }

    public static SyntaxTokenList Consume(ref this PooledArrayBuilder<SyntaxToken> builder)
    {
        var result = builder.ToList();
        builder.Clear();

        return result;
    }
}
