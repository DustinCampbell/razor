// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

/// <summary>
/// Represents a <see cref="SyntaxVisitor"/> that descends an entire <see cref="SyntaxNode"/> graph
/// visiting each SyntaxNode and its child SyntaxNodes and <see cref="SyntaxToken"/>s in depth-first order.
/// An optional range can be passed in which reduces the <see cref="SyntaxNode"/>s and <see cref="SyntaxToken"/>s
/// visited to those overlapping with the given range.
/// </summary>
internal abstract class SyntaxWalker : SyntaxVisitor
{
    private int _recursionDepth;
    private readonly TextSpan? _range;

    public SyntaxWalker(TextSpan? range = null)
    {
        _range = range;
    }

    public override void Visit(SyntaxNode? node)
    {
        if (node != null && (_range is null || _range.Value.OverlapsWith(node.Span)))
        {
            _recursionDepth++;
            StackGuard.EnsureSufficientExecutionStack(_recursionDepth);

            ((RazorSyntaxNode)node).Accept(this);

            _recursionDepth--;
        }
    }

    public override void DefaultVisit(SyntaxNode node)
    {
        foreach (var child in node.ChildNodesAndTokens())
        {
            if (_range is null || _range.Value.OverlapsWith(child.Span))
            {
                if (child.IsToken)
                {
                    VisitToken(child.AsToken());
                }
                else
                {
                    Debug.Assert(child.IsNode);
                    Visit(child.AsNode()!);
                }
            }
        }
    }

    public virtual void VisitToken(SyntaxToken token)
    {
    }
}
