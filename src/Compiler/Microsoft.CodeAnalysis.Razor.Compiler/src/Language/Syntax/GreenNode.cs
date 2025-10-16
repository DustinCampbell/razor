// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

[DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
internal abstract class GreenNode
{
    private static readonly ConditionalWeakTable<GreenNode, RazorDiagnostic[]> s_diagnosticsTable = new();
    private static readonly RazorDiagnostic[] s_noDiagnostics = [];

    private int _width;
    private byte _slotCount;

    protected GreenNode(SyntaxKind kind)
    {
        Kind = kind;
    }

    protected GreenNode(SyntaxKind kind, int width)
        : this(kind)
    {
        _width = width;
    }

    protected GreenNode(SyntaxKind kind, RazorDiagnostic[]? diagnostics)
        : this(kind, 0, diagnostics)
    {
    }

    protected GreenNode(SyntaxKind kind, int width, RazorDiagnostic[]? diagnostics)
        : this(kind, width)
    {
        if (diagnostics?.Length > 0)
        {
            Flags |= NodeFlags.ContainsDiagnostics;
            s_diagnosticsTable.Add(this, diagnostics);
        }
    }

    protected void AdjustFlagsAndWidth(GreenNode node)
    {
        if (node == null)
        {
            return;
        }

        SetFlags(node.Flags & NodeFlags.InheritMask);
        _width += node.Width;
    }

    #region Kind
    internal SyntaxKind Kind { get; }

    internal virtual bool IsList => false;

    internal virtual bool IsToken => false;
    #endregion

    public int Width => _width;

    #region Slots
    public int SlotCount
    {
        get
        {
            int count = _slotCount;
            if (count == byte.MaxValue)
            {
                count = GetSlotCount();
            }

            return count;
        }

        protected set
        {
            _slotCount = (byte)value;
        }
    }

    internal abstract GreenNode? GetSlot(int index);

    internal GreenNode GetRequiredSlot(int index)
    {
        var node = GetSlot(index);
        Debug.Assert(node is not null);

        return node;
    }

    // for slot counts >= byte.MaxValue
    protected virtual int GetSlotCount()
    {
        return _slotCount;
    }

    public virtual int GetSlotOffset(int index)
    {
        var offset = 0;
        for (var i = 0; i < index; i++)
        {
            var child = GetSlot(i);
            if (child != null)
            {
                offset += child.Width;
            }
        }

        return offset;
    }

    public virtual int FindSlotIndexContainingOffset(int offset)
    {
        Debug.Assert(0 <= offset && offset < Width);

        int i;
        var accumulatedWidth = 0;
        for (i = 0; ; i++)
        {
            Debug.Assert(i < SlotCount);
            var child = GetSlot(i);
            if (child != null)
            {
                accumulatedWidth += child.Width;
                if (offset < accumulatedWidth)
                {
                    break;
                }
            }
        }

        return i;
    }
    #endregion

    #region Flags
    public NodeFlags Flags { get; protected set; }

    internal void SetFlags(NodeFlags flags)
    {
        Flags |= flags;
    }

    internal void ClearFlags(NodeFlags flags)
    {
        Flags &= ~flags;
    }

    internal virtual bool IsMissing => (Flags & NodeFlags.IsMissing) != 0;

    public bool ContainsDiagnostics => (Flags & NodeFlags.ContainsDiagnostics) != 0;
    #endregion

    #region Diagnostics
    internal abstract GreenNode SetDiagnostics(RazorDiagnostic[]? diagnostics);

    internal RazorDiagnostic[] GetDiagnostics()
    {
        if (ContainsDiagnostics)
        {
            if (s_diagnosticsTable.TryGetValue(this, out var diagnostics))
            {
                return diagnostics;
            }
        }

        return s_noDiagnostics;
    }
    #endregion

    #region Text

    private string GetDebuggerDisplay()
    {
        return string.Build(state: (GetType().Name, Kind), static (ref builder, state) =>
        {
            var (name, kind) = state;

            builder.Append(name);
            builder.Append("<");
            builder.Append(kind.ToString());
            builder.Append(">");
        });
    }

    public void AppendContent(ref MemoryBuilder<ReadOnlyMemory<char>> builder)
    {
        // Use an actual Stack so we can write out deeply recursive structures without overflowing.
        using var stack = new PooledArrayBuilder<GreenNode>();

        stack.Push(this);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (current.IsToken)
            {
                builder.Append(((InternalSyntax.SyntaxToken)current).Content);
                continue;
            }

            var slotCount = current.SlotCount;

            for (var i = slotCount - 1; i >= 0; i--)
            {
                if (current.GetSlot(i) is GreenNode child)
                {
                    stack.Push(child);
                }
            }
        }
    }

    public override string ToString()
    {
        return _width == 0
            ? string.Empty
            : string.Build(this, static (ref builder, @this) => @this.AppendContent(ref builder));
    }

    #endregion

    #region Equivalence
    public virtual bool IsEquivalentTo([NotNullWhen(true)] GreenNode? other)
    {
        if (this == other)
        {
            return true;
        }

        if (other == null)
        {
            return false;
        }

        return EquivalentToInternal(this, other);
    }

    private static bool EquivalentToInternal(GreenNode node1, GreenNode node2)
    {
        if (node1.Kind != node2.Kind)
        {
            // A single-element list is usually represented as just a single node,
            // but can be represented as a List node with one child. Move to that
            // child if necessary.
            if (node1.IsList && node1.SlotCount == 1)
            {
                node1 = node1.GetRequiredSlot(0);
            }

            if (node2.IsList && node2.SlotCount == 1)
            {
                node2 = node2.GetRequiredSlot(0);
            }

            if (node1.Kind != node2.Kind)
            {
                return false;
            }
        }

        if (node1.Width != node2.Width)
        {
            return false;
        }

        var n = node1.SlotCount;
        if (n != node2.SlotCount)
        {
            return false;
        }

        for (var i = 0; i < n; i++)
        {
            var node1Child = node1.GetSlot(i);
            var node2Child = node2.GetSlot(i);
            if (node1Child != null && node2Child != null && !node1Child.IsEquivalentTo(node2Child))
            {
                return false;
            }
        }

        return true;
    }
    #endregion

    #region Factories
    public SyntaxNode CreateRed()
    {
        return CreateRed(null, 0);
    }

    internal abstract SyntaxNode CreateRed(SyntaxNode? parent, int position);
    #endregion

    public abstract TResult Accept<TResult>(InternalSyntax.SyntaxVisitor<TResult> visitor);

    public abstract void Accept(InternalSyntax.SyntaxVisitor visitor);
}
