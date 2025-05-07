// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class GreenNodeExtensions
{
    internal static InternalSyntax.SyntaxList<T> ToGreenList<T>(this SyntaxNode? node)
        where T : GreenNode
    {
        return node != null ?
            ToGreenList<T>(node.Green) :
            default;
    }

    internal static InternalSyntax.SyntaxList<T> ToGreenList<T>(this GreenNode? node)
        where T : GreenNode
    {
        return new InternalSyntax.SyntaxList<T>(node);
    }

    public static TNode WithAnnotationsGreen<TNode>(this TNode node, params SyntaxAnnotation[] annotations)
        where TNode : GreenNode
    {
        using var newAnnotations = new PooledArrayBuilder<SyntaxAnnotation>();

        foreach (var candidate in annotations)
        {
            if (!newAnnotations.Contains(candidate))
            {
                newAnnotations.Add(candidate);
            }
        }

        if (newAnnotations.Count == 0)
        {
            var existingAnnotations = node.GetAnnotations();
            return existingAnnotations != null && existingAnnotations.Length != 0
                ? (TNode)node.SetAnnotations(null)
                : node;
        }

        return (TNode)node.SetAnnotations(newAnnotations.ToArray());
    }

    public static TNode WithAdditionalAnnotationsGreen<TNode>(this TNode node, IEnumerable<SyntaxAnnotation>? annotations)
        where TNode : GreenNode
    {
        var existingAnnotations = node.GetAnnotations();

        if (annotations == null)
        {
            return node;
        }

        using var newAnnotations = new PooledArrayBuilder<SyntaxAnnotation>();
        newAnnotations.AddRange(existingAnnotations);

        foreach (var candidate in annotations)
        {
            if (!newAnnotations.Contains(candidate))
            {
                newAnnotations.Add(candidate);
            }
        }

        return newAnnotations.Count != existingAnnotations.Length
            ? (TNode)node.SetAnnotations(newAnnotations.ToArray())
            : node;
    }

    public static TNode WithDiagnosticsGreen<TNode>(this TNode node, RazorDiagnostic[]? diagnostics)
        where TNode : GreenNode
    {
        return (TNode)node.SetDiagnostics(diagnostics);
    }

    public static TNode WithDiagnosticsGreen<TNode>(this TNode node, params ImmutableArray<RazorDiagnostic> diagnostics)
        where TNode : GreenNode
    {
        var array = ImmutableCollectionsMarshal.AsArray(diagnostics);
        return node.WithDiagnosticsGreen(array);
    }
}
