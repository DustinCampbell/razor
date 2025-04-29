// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public static class IntermediateNodeExtensions
{
    public static bool IsImported(this IntermediateNode node)
    {
        return ReferenceEquals(node.Annotations[CommonAnnotations.Imported], CommonAnnotations.Imported);
    }

    public static bool IsDesignTimePropertyAccessHelper(this IntermediateNode tagHelper)
    {
        return tagHelper.Annotations[ComponentMetadata.Common.IsDesignTimePropertyAccessHelper] is string text &&
            bool.TryParse(text, out var result) &&
            result;
    }

    public static ImmutableArray<RazorDiagnostic> GetAllDiagnostics(this IntermediateNode node)
    {
        ArgHelper.ThrowIfNull(node);

        var diagnostics = new PooledHashSet<RazorDiagnostic>();
        try
        {
            CollectDiagnostics(node, ref diagnostics);

            return diagnostics.OrderByAsArray(static d => d.Span.AbsoluteIndex);
        }
        finally
        {
            diagnostics.ClearAndFree();
        }

        static void CollectDiagnostics(IntermediateNode node, ref PooledHashSet<RazorDiagnostic> diagnostics)
        {
            if (node.HasDiagnostics)
            {
                diagnostics.UnionWith(node.Diagnostics);
            }

            foreach (var childNode in node.Children)
            {
                CollectDiagnostics(childNode, ref diagnostics);
            }
        }
    }

    internal static string GetAllContent(this IntermediateNode node, Func<IntermediateToken, bool>? includeToken = null)
    {
        using var _ = ListPool<IntermediateToken>.GetPooledObject(out var tokens);

        node.CollectDescendantNodes(tokens, includeToken);

        return GetTokenContent(tokens);
    }

    internal static string GetAllChildContent(this IntermediateNode node)
    {
        using var _ = ListPool<IntermediateToken>.GetPooledObject(out var tokens);
        tokens.SetCapacityIfLarger(node.Children.Count);

        foreach (var child in node.Children)
        {
            if (child is IntermediateToken token)
            {
                tokens.Add(token);
            }
        }

        return GetTokenContent(tokens);
    }

    private static string GetTokenContent(List<IntermediateToken> tokens)
    {
        var length = 0;

        foreach (var token in tokens)
        {
            length += token.Content?.Length ?? 0;
        }

        return StringExtensions.CreateString(length, tokens, static (span, tokens) =>
        {
            foreach (var token in tokens)
            {
                var content = token.Content.AsSpan();

                if (content.Length > 0)
                {
                    content.CopyTo(span);
                    span = span[content.Length..];
                }
            }

            Debug.Assert(span.IsEmpty);
        });
    }

    public static IReadOnlyList<TNode> FindDescendantNodes<TNode>(this IntermediateNode node, Func<TNode, bool>? includeNode = null)
        where TNode : IntermediateNode
    {
        var results = new List<TNode>();
        node.CollectDescendantNodes(results, includeNode);

        return results;
    }

    internal static void CollectDescendantNodes<TNode>(this IntermediateNode node, List<TNode> results, Func<TNode, bool>? includeNode = null)
        where TNode : IntermediateNode
        => Visitor<TNode>.CollectNodes(node, includeNode, results);

    private sealed class Visitor<TNode> : IntermediateNodeWalker
        where TNode : IntermediateNode
    {
        private static readonly Func<TNode, bool> s_includeAll = _ => true;

        private readonly IntermediateNode _root;
        private readonly Func<TNode, bool> _includeNode;
        private readonly List<TNode> _results;

        private Visitor(IntermediateNode root, Func<TNode, bool>? includeNode, List<TNode> results)
        {
            _root = root;
            _includeNode = includeNode ?? s_includeAll;
            _results = results;
        }

        public static void CollectNodes(IntermediateNode root, Func<TNode, bool>? includeNode, List<TNode> results)
        {
            var visitor = new Visitor<TNode>(root, includeNode, results);

            visitor.Visit(root);
        }

        public override void VisitDefault(IntermediateNode node)
        {
            if (node is TNode matchedNode && _includeNode(matchedNode))
            {
                // Don't put root in the results.
                if (_results.Count > 0 || matchedNode != _root)
                {
                    _results.Add(matchedNode);
                }
            }

            base.VisitDefault(node);
        }
    }
}
