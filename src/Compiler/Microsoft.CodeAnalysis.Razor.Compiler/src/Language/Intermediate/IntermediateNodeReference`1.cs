// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
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

    private string GetDebuggerDisplay()
    {
        return $"ref: {Parent.DebuggerToString()} - {Node.DebuggerToString()}";
    }

    public static implicit operator IntermediateNodeReference(IntermediateNodeReference<TNode> reference)
        => new(reference.Parent, reference.Node);
}
