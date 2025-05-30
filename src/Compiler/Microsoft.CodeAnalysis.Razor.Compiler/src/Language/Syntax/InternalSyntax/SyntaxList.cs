// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

internal abstract class SyntaxList : GreenNode
{
    internal override bool IsList => true;

    private SyntaxList(RazorDiagnostic[]? diagnostics, SyntaxAnnotation[]? annotations)
        : base(SyntaxKind.List, diagnostics, annotations)
    {
    }

    internal static GreenNode List(GreenNode child0, GreenNode child1)
    {
        Debug.Assert(child0 != null);
        Debug.Assert(child1 != null);

        return new WithTwoChildren(child0, child1);
    }

    internal static GreenNode List(GreenNode child0, GreenNode child1, GreenNode child2)
    {
        Debug.Assert(child0 != null);
        Debug.Assert(child1 != null);
        Debug.Assert(child2 != null);

        return new WithThreeChildren(child0, child1, child2);
    }

    internal static GreenNode List(ReadOnlySpan<GreenNode> nodes)
    {
        var length = nodes.Length;
        var array = new ArrayElement<GreenNode>[length];

        for (var i = 0; i < length; i++)
        {
            Debug.Assert(nodes[i] != null);
            array[i].Value = nodes[i];
        }

        return List(array);
    }

    internal static GreenNode List(ArrayElement<GreenNode>[] children)
    {
        // "WithLotsOfChildren" list will allocate a separate array to hold
        // precomputed node offsets. It may not be worth it for smallish lists.
        return children.Length < 10
            ? new WithManyChildren(children)
            : new WithLotsOfChildren(children);
    }

    internal abstract void CopyTo(ArrayElement<GreenNode>[] destination, int offset);

    internal static GreenNode? Concat(GreenNode? left, GreenNode? right)
    {
        // Handle null cases
        if (left is null)
        {
            return right;
        }

        if (right is null)
        {
            return left;
        }

        if (left is SyntaxList { SlotCount: var leftSlotCount } leftList)
        {
            if (right is SyntaxList { SlotCount: var rightSlotCount } rightList)
            {
                // Both are lists - concatenate all elements
                var array = new ArrayElement<GreenNode>[leftSlotCount + rightSlotCount];
                leftList.CopyTo(array, 0);
                rightList.CopyTo(array, leftSlotCount);

                return List(array);
            }
            else
            {
                // Left is a list, right is a single node
                var array = new ArrayElement<GreenNode>[leftSlotCount + 1];
                leftList.CopyTo(array, 0);
                array[leftSlotCount].Value = right;

                return List(array);
            }
        }
        else if (right is SyntaxList { SlotCount: var rightSlotCount } rightList)
        {
            // Right is a list, left is a single node
            var array = new ArrayElement<GreenNode>[rightSlotCount + 1];
            array[0].Value = left;
            rightList.CopyTo(array, 1);

            return List(array);
        }

        // Both are simple nodes - create a new list with both
        return List(left, right);
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        => visitor.Visit(this);

    public override void Accept(SyntaxVisitor visitor)
        => visitor.Visit(this);

    private sealed class WithTwoChildren : SyntaxList
    {
        private readonly GreenNode _child0;
        private readonly GreenNode _child1;

        internal WithTwoChildren(
            GreenNode child0,
            GreenNode child1,
            RazorDiagnostic[]? diagnostics = null,
            SyntaxAnnotation[]? annotations = null)
            : base(diagnostics, annotations)
        {
            SlotCount = 2;
            AdjustFlagsAndWidth(child0);
            _child0 = child0;
            AdjustFlagsAndWidth(child1);
            _child1 = child1;
        }

        internal override GreenNode? GetSlot(int index)
            => index switch
            {
                0 => _child0,
                1 => _child1,
                _ => null,
            };

        internal override void CopyTo(ArrayElement<GreenNode>[] destination, int offset)
        {
            destination[offset].Value = _child0;
            destination[offset + 1].Value = _child1;
        }

        internal override SyntaxNode CreateRed(SyntaxNode? parent, int position)
            => new Syntax.SyntaxList.WithTwoChildren(this, parent, position);

        internal override GreenNode SetDiagnostics(RazorDiagnostic[]? diagnostics)
            => new WithTwoChildren(_child0, _child1, diagnostics, GetAnnotations());

        internal override GreenNode SetAnnotations(SyntaxAnnotation[]? annotations)
            => new WithTwoChildren(_child0, _child1, GetDiagnostics(), annotations);
    }

    private sealed class WithThreeChildren : SyntaxList
    {
        private readonly GreenNode _child0;
        private readonly GreenNode _child1;
        private readonly GreenNode _child2;

        internal WithThreeChildren(
            GreenNode child0,
            GreenNode child1,
            GreenNode child2,
            RazorDiagnostic[]? diagnostics = null,
            SyntaxAnnotation[]? annotations = null)
            : base(diagnostics, annotations)
        {
            SlotCount = 3;
            AdjustFlagsAndWidth(child0);
            _child0 = child0;
            AdjustFlagsAndWidth(child1);
            _child1 = child1;
            AdjustFlagsAndWidth(child2);
            _child2 = child2;
        }

        internal override GreenNode? GetSlot(int index)
            => index switch
            {
                0 => _child0,
                1 => _child1,
                2 => _child2,
                _ => null,
            };

        internal override void CopyTo(ArrayElement<GreenNode>[] destination, int offset)
        {
            destination[offset].Value = _child0;
            destination[offset + 1].Value = _child1;
            destination[offset + 2].Value = _child2;
        }

        internal override SyntaxNode CreateRed(SyntaxNode? parent, int position)
            => new Syntax.SyntaxList.WithThreeChildren(this, parent, position);

        internal override GreenNode SetDiagnostics(RazorDiagnostic[]? diagnostics)
            => new WithThreeChildren(_child0, _child1, _child2, diagnostics, GetAnnotations());

        internal override GreenNode SetAnnotations(SyntaxAnnotation[]? annotations)
            => new WithThreeChildren(_child0, _child1, _child2, GetDiagnostics(), annotations);
    }

    private abstract class WithManyChildrenBase : SyntaxList
    {
        protected readonly ArrayElement<GreenNode>[] Children;

        internal WithManyChildrenBase(
            ArrayElement<GreenNode>[] children,
            RazorDiagnostic[]? diagnostics = null,
            SyntaxAnnotation[]? annotations = null)
            : base(diagnostics, annotations)
        {
            Children = children;

            var length = Children.Length;

            SlotCount = length < byte.MaxValue
                ? (byte)length
                : byte.MaxValue;

            foreach (var child in Children)
            {
                Debug.Assert(child.Value != null);
                AdjustFlagsAndWidth(child);
            }
        }

        protected override int GetSlotCount()
            => Children.Length;

        internal override GreenNode? GetSlot(int index)
            => Children[index];

        internal override void CopyTo(ArrayElement<GreenNode>[] destination, int offset)
        {
            Array.Copy(Children, 0, destination, offset, Children.Length);
        }

        internal override SyntaxNode CreateRed(SyntaxNode? parent, int position)
            => new Syntax.SyntaxList.WithManyChildren(this, parent, position);
    }

    private sealed class WithManyChildren : WithManyChildrenBase
    {
        internal WithManyChildren(
            ArrayElement<GreenNode>[] children,
            RazorDiagnostic[]? diagnostics = null,
            SyntaxAnnotation[]? annotations = null)
            : base(children, diagnostics, annotations)
        {
        }

        internal override GreenNode SetDiagnostics(RazorDiagnostic[]? diagnostics)
            => new WithManyChildren(Children, diagnostics, GetAnnotations());

        internal override GreenNode SetAnnotations(SyntaxAnnotation[]? annotations)
            => new WithManyChildren(Children, GetDiagnostics(), annotations);
    }

    private sealed class WithLotsOfChildren : WithManyChildrenBase
    {
        private readonly int[] _childOffsets;

        internal WithLotsOfChildren(ArrayElement<GreenNode>[] children)
            : base(children)
        {
            _childOffsets = CalculateOffsets(children);
        }

        private WithLotsOfChildren(
            ArrayElement<GreenNode>[] children,
            int[] childOffsets,
            RazorDiagnostic[]? diagnostics,
            SyntaxAnnotation[]? annotations)
            : base(children, diagnostics, annotations)
        {
            _childOffsets = childOffsets;
        }

        public override int GetSlotOffset(int index)
            => _childOffsets[index];

        /// <summary>
        /// Find the slot that contains the given offset.
        /// </summary>
        /// <param name="offset">The target offset. Must be between 0 and <see cref="GreenNode.Width"/>.</param>
        /// <returns>The slot index of the slot containing the given offset.</returns>
        /// <remarks>
        /// This implementation uses a binary search to find the first slot that contains
        /// the given offset.
        /// </remarks>
        public override int FindSlotIndexContainingOffset(int offset)
        {
            Debug.Assert(offset >= 0 && offset < Width);
            return BinarySearchUpperBound(_childOffsets, offset) - 1;
        }

        private static int[] CalculateOffsets(ArrayElement<GreenNode>[] children)
        {
            var length = children.Length;
            var childOffsets = new int[length];
            var offset = 0;

            for (var i = 0; i < length; i++)
            {
                childOffsets[i] = offset;
                offset += children[i].Value.Width;
            }

            return childOffsets;
        }

        internal override GreenNode SetDiagnostics(RazorDiagnostic[]? diagnostics)
            => new WithLotsOfChildren(Children, _childOffsets, diagnostics, GetAnnotations());

        internal override GreenNode SetAnnotations(SyntaxAnnotation[]? annotations)
            => new WithLotsOfChildren(Children, _childOffsets, GetDiagnostics(), annotations);

        /// <summary>
        /// Search a sorted integer array for the target value in O(log N) time.
        /// </summary>
        /// <param name="array">The array of integers which must be sorted in ascending order.</param>
        /// <param name="value">The target value.</param>
        /// <returns>
        /// An index in the array pointing to the position where <paramref name="value"/> should be
        /// inserted in order to maintain the sorted order. All values to the right of this position will be
        /// strictly greater than <paramref name="value"/>. Note that this may return a position off the end
        /// of the array if all elements are less than or equal to <paramref name="value"/>.
        /// </returns>
        private static int BinarySearchUpperBound(int[] array, int value)
        {
            var low = 0;
            var high = array.Length - 1;

            while (low <= high)
            {
                var middle = low + ((high - low) >> 1);
                if (array[middle] > value)
                {
                    high = middle - 1;
                }
                else
                {
                    low = middle + 1;
                }
            }

            return low;
        }
    }
}
