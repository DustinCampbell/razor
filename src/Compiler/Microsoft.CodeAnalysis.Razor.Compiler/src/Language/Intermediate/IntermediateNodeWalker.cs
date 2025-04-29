// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public abstract class IntermediateNodeWalker : IntermediateNodeVisitor
{
    private readonly ImmutableArray<IntermediateNode>.Builder _ancestors = ImmutableArray.CreateBuilder<IntermediateNode>();

    private ImmutableArray<IntermediateNode> _currentAncestors;

    protected ImmutableArray<IntermediateNode> Ancestors
    {
        get
        {
            return !_currentAncestors.IsDefault
                ? _currentAncestors
                : GetCurrentAncestors();

            ImmutableArray<IntermediateNode> GetCurrentAncestors()
            {
                var result = _ancestors.ToImmutable();
                result.Unsafe().Reverse();

                _currentAncestors = result;

                return result;
            }
        }
    }

    [MemberNotNullWhen(true, nameof(Parent))]
    protected bool HasParent => _ancestors.Count > 0;

    protected IntermediateNode? Parent
        => _ancestors.Count > 0 ? _ancestors[^1] : null;

    public override void VisitDefault(IntermediateNode node)
    {
        var children = node.Children;

        if (children.Count == 0)
        {
            return;
        }

        _ancestors.Add(node);
        _currentAncestors = default;

        try
        {
            foreach (var child in children)
            {
                Visit(child);
            }
        }
        finally
        {
            _ancestors.RemoveAt(_ancestors.Count - 1);
            _currentAncestors = default;
        }
    }
}
