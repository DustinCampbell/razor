// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.AspNetCore.Razor.PooledObjects;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

[CollectionBuilder(typeof(SyntaxList), methodName: nameof(SyntaxList.Create))]
internal readonly partial struct SyntaxList<TNode>(SyntaxNode? node) : IReadOnlyList<TNode>, IEquatable<SyntaxList<TNode>>
    where TNode : SyntaxNode
{
    internal SyntaxNode? Node { get; } = node;

    private static SyntaxNode? CreateNode(SyntaxList<TNode> nodes)
    {
        using var builder = new PooledArrayBuilder<TNode>(nodes.Count);
        builder.AddRange(nodes);

        return builder.ToList().Node;
    }

    public static SyntaxList<TNode> Create(SyntaxNode node, SyntaxNode parent, int position)
        => new(node.Green.CreateRed(parent, position));

    public static SyntaxList<TNode> Create(SyntaxNode node, SyntaxNode parent)
        => new(node.Green.CreateRed(parent, parent.Position));

    /// <summary>
    /// The number of nodes in the list.
    /// </summary>
    public int Count
        => Node is SyntaxNode node
            ? (node.IsList ? node.SlotCount : 1)
            : 0;

    /// <summary>
    /// Gets the node at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the node to get or set.</param>
    /// <returns>The node at the specified index.</returns>
    public TNode this[int index]
    {
        get
        {
            if (Node is SyntaxNode node)
            {
                if (node.IsList)
                {
                    if (unchecked((uint)index < (uint)node.SlotCount))
                    {
                        return (TNode)node.GetNodeSlot(index).AssumeNotNull();
                    }
                }
                else if (index == 0)
                {
                    return (TNode)node;
                }
            }

            return ThrowHelper.ThrowArgumentOutOfRangeException<TNode>(nameof(index));
        }
    }

    internal SyntaxNode? ItemInternal(int index)
    {
        if (Node?.IsList is true)
        {
            return Node.GetNodeSlot(index);
        }

        Debug.Assert(index == 0);
        return Node;
    }

    /// <summary>
    /// The absolute span of the list elements in characters.
    /// </summary>
    public TextSpan Span
        => Count > 0
            ? TextSpan.FromBounds(this[0].Span.Start, this[Count - 1].Span.End)
            : default;

    /// <summary>
    /// Returns the string representation of the nodes in this list.
    /// </summary>
    /// <returns>
    /// The string representation of the nodes in this list.
    /// </returns>
    public override string ToString()
        => Node?.ToString() ?? string.Empty;

    /// <summary>
    /// Creates a new list with the specified node added at the end.
    /// </summary>
    /// <param name="node">The node to add.</param>
    public SyntaxList<TNode> Add(TNode node)
    {
        return Insert(Count, node);
    }

    /// <summary>
    /// Creates a new list with the specified nodes added at the end.
    /// </summary>
    /// <param name="nodes">The nodes to add.</param>
    public SyntaxList<TNode> AddRange(IEnumerable<TNode> nodes)
    {
        return InsertRange(Count, nodes);
    }

    /// <summary>
    /// Creates a new list with the specified node inserted at the index.
    /// </summary>
    /// <param name="index">The index to insert at.</param>
    /// <param name="node">The node to insert.</param>
    public SyntaxList<TNode> Insert(int index, TNode node)
    {
        ArgHelper.ThrowIfNull(node);

        return InsertRange(index, [node]);
    }

    /// <summary>
    /// Creates a new list with the specified nodes inserted at the index.
    /// </summary>
    /// <param name="index">The index to insert at.</param>
    /// <param name="nodes">The nodes to insert.</param>
    public SyntaxList<TNode> InsertRange(int index, IEnumerable<TNode> nodes)
    {
        ArgHelper.ThrowIfNull(index);
        ArgHelper.ThrowIfGreaterThanOrEqual(index, Count);
        ArgHelper.ThrowIfNull(nodes);

        var list = this.ToList();
        list.InsertRange(index, nodes);

        if (list.Count == 0)
        {
            return this;
        }
        else
        {
            return CreateList(list[0].Green, list);
        }
    }

    /// <summary>
    /// Creates a new list with the element at specified index removed.
    /// </summary>
    /// <param name="index">The index of the element to remove.</param>
    public SyntaxList<TNode> RemoveAt(int index)
    {
        ArgHelper.ThrowIfNegative(index);
        ArgHelper.ThrowIfGreaterThanOrEqual(index, Count);

        return Remove(this[index]);
    }

    /// <summary>
    /// Creates a new list with the element removed.
    /// </summary>
    /// <param name="node">The element to remove.</param>
    public SyntaxList<TNode> Remove(TNode node)
    {
        return CreateList(Where(x => x != node).ToList());
    }

    /// <summary>
    /// Creates a new list with the specified element replaced with the new node.
    /// </summary>
    /// <param name="nodeInList">The element to replace.</param>
    /// <param name="newNode">The new node.</param>
    public SyntaxList<TNode> Replace(TNode nodeInList, TNode newNode)
    {
        return ReplaceRange(nodeInList, [newNode]);
    }

    /// <summary>
    /// Creates a new list with the specified element replaced with new nodes.
    /// </summary>
    /// <param name="nodeInList">The element to replace.</param>
    /// <param name="newNodes">The new nodes.</param>
    public SyntaxList<TNode> ReplaceRange(TNode nodeInList, IEnumerable<TNode> newNodes)
    {
        ArgHelper.ThrowIfNull(nodeInList);
        ArgHelper.ThrowIfNull(newNodes);

        var index = IndexOf(nodeInList);

        if (index < 0 || index >= Count)
        {
            return ThrowHelper.ThrowArgumentOutOfRangeException<SyntaxList<TNode>>(nameof(nodeInList));
        }

        var list = this.ToList();
        list.RemoveAt(index);
        list.InsertRange(index, newNodes);

        return CreateList(list);
    }

    private static SyntaxList<TNode> CreateList(List<TNode> items)
    {
        return items.Count != 0
            ? CreateList(items[0].Green, items)
            : default;
    }

    static SyntaxList<TNode> CreateList(GreenNode creator, List<TNode> items)
    {
        if (items.Count == 0)
        {
            return default;
        }

        var newGreen = creator.CreateList(items.Select(n => n.Green));
        return new SyntaxList<TNode>(newGreen.CreateRed());
    }

    /// <summary>
    /// The first node in the list.
    /// </summary>
    public TNode First()
        => this[0];

    /// <summary>
    /// The first node in the list or default if the list is empty.
    /// </summary>
    public TNode? FirstOrDefault()
        => Any() ? this[0] : null;

    /// <summary>
    /// The last node in the list.
    /// </summary>
    public TNode Last()
        => this[^1];

    /// <summary>
    /// The last node in the list or default if the list is empty.
    /// </summary>
    public TNode? LastOrDefault()
        => Any() ? this[^1] : null;

    /// <summary>
    /// True if the list has at least one node.
    /// </summary>
    public bool Any()
    {
        Debug.Assert(Node == null || Count != 0);
        return Node != null;
    }

    public SyntaxList<TNode> Where(Func<TNode, bool> predicate)
    {
        using var builder = new PooledArrayBuilder<TNode>(Count);

        foreach (var node in this)
        {
            if (predicate(node))
            {
                builder.Add(node);
            }
        }

        return builder.ToList();
    }

    // for debugging
#pragma warning disable IDE0051 // Remove unused private members
    private TNode[] Nodes => [.. this];
#pragma warning restore IDE0051 // Remove unused private members

    /// <summary>
    /// Get's the enumerator for this list.
    /// </summary>
    public Enumerator GetEnumerator()
        => new(in this);

    IEnumerator<TNode> IEnumerable<TNode>.GetEnumerator()
        => Any()
            ? new EnumeratorImpl(this)
            : SpecializedCollections.EmptyEnumerator<TNode>();

    IEnumerator IEnumerable.GetEnumerator()
        => Any()
            ? new EnumeratorImpl(this)
            : (IEnumerator)SpecializedCollections.EmptyEnumerator<TNode>();

    public static bool operator ==(SyntaxList<TNode> left, SyntaxList<TNode> right)
        => left.Node == right.Node;

    public static bool operator !=(SyntaxList<TNode> left, SyntaxList<TNode> right)
        => left.Node != right.Node;

    public bool Equals(SyntaxList<TNode> other)
        => Node == other.Node;

    public override bool Equals(object? obj)
        => obj is SyntaxList<TNode> list &&
           Equals(list);

    public override int GetHashCode()
        => Node?.GetHashCode() ?? 0;

    public static implicit operator SyntaxList<TNode>(SyntaxList<SyntaxNode> nodes)
        => new(nodes.Node);

    public static implicit operator SyntaxList<SyntaxNode>(SyntaxList<TNode> nodes)
        => new(nodes.Node);

    /// <summary>
    /// The index of the node in this list, or -1 if the node is not in the list.
    /// </summary>
    public int IndexOf(TNode node)
    {
        var index = 0;
        foreach (var child in this)
        {
            if (Equals(child, node))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    public int IndexOf(Func<TNode, bool> predicate)
    {
        var index = 0;
        foreach (var child in this)
        {
            if (predicate(child))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal int IndexOf(SyntaxKind kind)
    {
        var index = 0;
        foreach (var child in this)
        {
            if (child.Kind == kind)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    public int LastIndexOf(TNode node)
    {
        for (var i = Count - 1; i >= 0; i--)
        {
            if (Equals(this[i], node))
            {
                return i;
            }
        }

        return -1;
    }

    public int LastIndexOf(Func<TNode, bool> predicate)
    {
        for (var i = Count - 1; i >= 0; i--)
        {
            if (predicate(this[i]))
            {
                return i;
            }
        }

        return -1;
    }
}
