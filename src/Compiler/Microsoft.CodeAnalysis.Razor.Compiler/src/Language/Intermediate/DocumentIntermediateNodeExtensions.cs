// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public static class DocumentIntermediateNodeExtensions
{
    public static ClassDeclarationIntermediateNode? FindPrimaryClass(this DocumentIntermediateNode documentNode)
    {
        ArgHelper.ThrowIfNull(documentNode);

        return FindWithAnnotation<ClassDeclarationIntermediateNode>(documentNode, CommonAnnotations.PrimaryClass);
    }

    public static MethodDeclarationIntermediateNode? FindPrimaryMethod(this DocumentIntermediateNode documentNode)
    {
        ArgHelper.ThrowIfNull(documentNode);

        return FindWithAnnotation<MethodDeclarationIntermediateNode>(documentNode, CommonAnnotations.PrimaryMethod);
    }

    public static NamespaceDeclarationIntermediateNode? FindPrimaryNamespace(this DocumentIntermediateNode documentNode)
    {
        ArgHelper.ThrowIfNull(documentNode);

        return FindWithAnnotation<NamespaceDeclarationIntermediateNode>(documentNode, CommonAnnotations.PrimaryNamespace);
    }

    public static IReadOnlyList<IntermediateNodeReference<DirectiveIntermediateNode>> FindDirectiveReferences(this DocumentIntermediateNode documentNode, DirectiveDescriptor directive)
    {
        ArgHelper.ThrowIfNull(documentNode);
        ArgHelper.ThrowIfNull(directive);

        var results = new List<IntermediateNodeReference<DirectiveIntermediateNode>>();
        DirectiveVisitor.Collect(documentNode, directive, results);

        return results;
    }

    public static IReadOnlyList<IntermediateNodeReference<TNode>> FindDescendantReferences<TNode>(this DocumentIntermediateNode documentNode)
        where TNode : IntermediateNode
    {
        ArgHelper.ThrowIfNull(documentNode);

        var results = new List<IntermediateNodeReference<TNode>>();
        ReferenceVisitor<TNode>.Collect(documentNode, results);

        return results;
    }

    internal static void CollectDirectiveReferences(
        this DocumentIntermediateNode documentNode,
        DirectiveDescriptor directive,
        List<IntermediateNodeReference<DirectiveIntermediateNode>> results)
        => DirectiveVisitor.Collect(documentNode, directive, results);

    internal static void CollectDescendantReferences<TNode>(this DocumentIntermediateNode documentNode, List<IntermediateNodeReference<TNode>> results)
        where TNode : IntermediateNode
        => ReferenceVisitor<TNode>.Collect(documentNode, results);

    private static T? FindWithAnnotation<T>(IntermediateNode node, object annotation)
        where T : IntermediateNode
    {
        using var stack = new PooledArrayBuilder<IntermediateNode>();
        stack.Push(node);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (current is T target && ReferenceEquals(target.Annotations[annotation], annotation))
            {
                return target;
            }

            // Note: Push in reverse order
            for (var i = current.Children.Count - 1; i >= 0; i--)
            {
                stack.Push(current.Children[i]);
            }
        }

        return null;
    }

    private sealed class DirectiveVisitor : IntermediateNodeWalker
    {
        private readonly DirectiveDescriptor _directive;
        private readonly List<IntermediateNodeReference<DirectiveIntermediateNode>> _results;

        private DirectiveVisitor(DirectiveDescriptor directive, List<IntermediateNodeReference<DirectiveIntermediateNode>> results)
        {
            _directive = directive;
            _results = results;
        }

        public static void Collect(DocumentIntermediateNode documentNode, DirectiveDescriptor directive, List<IntermediateNodeReference<DirectiveIntermediateNode>> results)
        {
            var visitor = new DirectiveVisitor(directive, results);
            visitor.Visit(documentNode);
        }

        public override void VisitDirective(DirectiveIntermediateNode node)
        {
            if (_directive == node.Directive)
            {
                Debug.Assert(HasParent);
                _results.Add(new(Parent, node));
            }

            base.VisitDirective(node);
        }
    }

    private sealed class ReferenceVisitor<TNode> : IntermediateNodeWalker
        where TNode : IntermediateNode
    {
        private readonly List<IntermediateNodeReference<TNode>> _results;

        private ReferenceVisitor(List<IntermediateNodeReference<TNode>> results)
        {
            _results = results;
        }

        public static void Collect(DocumentIntermediateNode documentNode, List<IntermediateNodeReference<TNode>> results)
        {
            var visitor = new ReferenceVisitor<TNode>(results);
            visitor.Visit(documentNode);
        }

        public override void VisitDefault(IntermediateNode node)
        {
            base.VisitDefault(node);

            // Use a post-order traversal because references are used to replace nodes, and thus
            // change the parent nodes.
            //
            // This ensures that we always operate on the leaf nodes first.
            if (node is TNode typedNode)
            {
                Debug.Assert(HasParent);
                _results.Add(new(Parent, typedNode));
            }
        }
    }
}
