// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public readonly struct IntermediateNodeReference
{
    public IntermediateNode Node { get; }
    public IntermediateNode Parent { get; }

    public IntermediateNodeReference(IntermediateNode parent, IntermediateNode node)
    {
        ArgHelper.ThrowIfNull(parent);
        ArgHelper.ThrowIfNull(node);

        Parent = parent;
        Node = node;
    }

    public static IntermediateNodeReference<TNode> Create<TNode>(IntermediateNode parent, TNode node)
        where TNode : IntermediateNode
        => new(parent, node);

    public void Deconstruct(out IntermediateNode parent, out IntermediateNode node)
    {
        parent = Parent;
        node = Node;
    }

    public void InsertAfter(IntermediateNode node)
    {
        ArgHelper.ThrowIfNull(node);

        var index = GetNodeIndex();

        Parent.Children.Insert(index + 1, node);
    }

    public void InsertAfter(IEnumerable<IntermediateNode> nodes)
    {
        ArgHelper.ThrowIfNull(nodes);

        var index = GetNodeIndex();

        foreach (var node in nodes)
        {
            Parent.Children.Insert(++index, node);
        }
    }

    public void InsertBefore(IntermediateNode node)
    {
        ArgHelper.ThrowIfNull(node);

        var index = GetNodeIndex();

        Parent.Children.Insert(index, node);
    }

    public void InsertBefore(IEnumerable<IntermediateNode> nodes)
    {
        ArgHelper.ThrowIfNull(nodes);

        var index = GetNodeIndex();

        foreach (var node in nodes)
        {
            Parent.Children.Insert(index++, node);
        }
    }

    public void Remove()
    {
        var index = GetNodeIndex();

        Parent.Children.RemoveAt(index);
    }

    public void Replace(IntermediateNode node)
    {
        ArgHelper.ThrowIfNull(node);

        var index = GetNodeIndex();

        Parent.Children[index] = node;
    }

    private int GetNodeIndex()
    {
        if (Parent == null)
        {
            ThrowHelper.ThrowInvalidOperationException(Resources.IntermediateNodeReference_NotInitialized);
        }

        if (Parent.Children.IsReadOnly)
        {
            ThrowHelper.ThrowInvalidOperationException(Resources.FormatIntermediateNodeReference_CollectionIsReadOnly(Parent));
        }

        var index = Parent.Children.IndexOf(Node);
        if (index == -1)
        {
            ThrowHelper.ThrowInvalidOperationException(Resources.FormatIntermediateNodeReference_NodeNotFound(Node, Parent));
        }

        return index;
    }

    private string GetDebuggerDisplay()
    {
        return $"ref: {Parent.DebuggerToString()} - {Node.DebuggerToString()}";
    }

    public IntermediateNodeReference<TNode> As<TNode>()
        where TNode : IntermediateNode
        => new(Parent, (TNode)Node);
}
