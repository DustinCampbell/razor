// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public readonly struct IntermediateNodeReference<TNode>
    where TNode : IntermediateNode
{
    public TNode Node { get; }
    public IntermediateNode Parent { get; }

    public IntermediateNodeReference(IntermediateNode parent, TNode node)
    {
        ArgHelper.ThrowIfNull(parent);
        ArgHelper.ThrowIfNull(node);

        Parent = parent;
        Node = node;
    }

    public void InsertAfter(IntermediateNode node)
    {
        ((IntermediateNodeReference)this).InsertAfter(node);
    }

    public void InsertAfter(IEnumerable<IntermediateNode> nodes)
    {
        ((IntermediateNodeReference)this).InsertAfter(nodes);
    }

    public void InsertBefore(IntermediateNode node)
    {
        ((IntermediateNodeReference)this).InsertBefore(node);
    }

    public void InsertBefore(IEnumerable<IntermediateNode> nodes)
    {
        ((IntermediateNodeReference)this).InsertBefore(nodes);
    }

    public void Remove()
    {
        ((IntermediateNodeReference)this).Remove();
    }

    public void Replace(IntermediateNode node)
    {
        ((IntermediateNodeReference)this).Replace(node);
    }

    public static implicit operator IntermediateNodeReference(IntermediateNodeReference<TNode> reference)
        => new(reference.Parent, reference.Node);
}

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
