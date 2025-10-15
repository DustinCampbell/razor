// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class GreenNodeExtensions
{
    internal static InternalSyntax.SyntaxList<T> ToGreenList<T>(this SyntaxNode node) where T : GreenNode
    {
        return node != null ?
            ToGreenList<T>(node.Green) :
            default(InternalSyntax.SyntaxList<T>);
    }

    internal static InternalSyntax.SyntaxList<T> ToGreenList<T>(this GreenNode node) where T : GreenNode
    {
        return new InternalSyntax.SyntaxList<T>(node);
    }

#nullable enable

    public static TNode WithAnnotationGreen<TNode>(this TNode node, SyntaxAnnotation annotation)
        where TNode : GreenNode
    {
        return (TNode)node.SetAnnotations([annotation]);
    }

    public static TNode WithAnnotationsGreen<TNode>(this TNode node, IEnumerable<SyntaxAnnotation> annotations)
        where TNode : GreenNode
    {
        if (!annotations.TryGetCount(out var count))
        {
            return WithAnnotationsGreenSlow(node, annotations);
        }

        if (count == 0)
        {
            return node;
        }

        using var newAnnotations = new PooledHashSet<SyntaxAnnotation>(count);

        var result = new SyntaxAnnotation[count];
        var index = 0;

        foreach (var candidate in annotations)
        {
            if (!newAnnotations.Add(candidate))
            {
                result[index++] = candidate;
            }
        }

        if (result.Length > index)
        {
            // Resize the array if annotations contained duplicates.
            Array.Resize(ref result, index);
        }

        return (TNode)node.SetAnnotations(result);

        static TNode WithAnnotationsGreenSlow(TNode node, IEnumerable<SyntaxAnnotation> annotations)
        {
            using var newAnnotations = new PooledHashSet<SyntaxAnnotation>();
            using var builder = new PooledArrayBuilder<SyntaxAnnotation>();

            foreach (var candidate in annotations)
            {
                if (!newAnnotations.Add(candidate))
                {
                    builder.Add(candidate);
                }
            }

            return (TNode)node.SetAnnotations(builder.ToArrayAndClear());
        }
    }

    public static TNode WithAdditionalAnnotationGreen<TNode>(this TNode node, SyntaxAnnotation annotation)
        where TNode : GreenNode
    {
        ArgHelper.ThrowIfNull(annotation);

        var existingAnnotations = node.GetAnnotations();
        if (existingAnnotations.Length == 0)
        {
            return (TNode)node.SetAnnotations([annotation]);
        }

        if (existingAnnotations.Contains(annotation))
        {
            // If we already have this annotation, we're done.
            return node;
        }

        var result = new SyntaxAnnotation[existingAnnotations.Length + 1];
        existingAnnotations.CopyTo(result);
        result[^1] = annotation;

        return (TNode)node.SetAnnotations(result);
    }

    public static TNode WithAdditionalAnnotationsGreen<TNode>(this TNode node, IEnumerable<SyntaxAnnotation> annotations)
        where TNode : GreenNode
    {
        ArgHelper.ThrowIfNull(annotations);

        var existingAnnotations = node.GetAnnotations();

        using var existingAnnotationSet = new PooledHashSet<SyntaxAnnotation>(existingAnnotations.Length);
        existingAnnotationSet.UnionWith(existingAnnotations);

        using var newAnnotations = new PooledArrayBuilder<SyntaxAnnotation>(capacity: existingAnnotations.Length);
        newAnnotations.AddRange(existingAnnotations);

        foreach (var candidate in annotations)
        {
            if (!existingAnnotationSet.Contains(candidate))
            {
                newAnnotations.Add(candidate);
            }
        }

        return newAnnotations.Count != existingAnnotations.Length
            ? (TNode)node.SetAnnotations(newAnnotations.ToArrayAndClear())
            : node;
    }

    public static TNode WithoutAnnotationGreen<TNode>(this TNode node, SyntaxAnnotation annotation)
        where TNode : GreenNode
    {
        ArgHelper.ThrowIfNull(annotation);

        var existingAnnotations = node.GetAnnotations();
        if (existingAnnotations.Length == 0)
        {
            // If there aren't any existing annotations, we have nothing to remove.
            return node;
        }

        var foundCount = 0;

        foreach (var candidate in existingAnnotations)
        {
            if (candidate == annotation)
            {
                foundCount++;
            }
        }

        if (foundCount == 0)
        {
            return node;
        }

        var result = new SyntaxAnnotation[existingAnnotations.Length - foundCount];
        var index = 0;

        foreach (var candidate in existingAnnotations)
        {
            if (candidate != annotation)
            {
                result[index++] = candidate;
            }
        }

        Debug.Assert(index == result.Length);

        return (TNode)node.SetAnnotations(result);
    }

    public static TNode WithoutAnnotationsGreen<TNode>(this TNode node, string annotationKind)
        where TNode : GreenNode
    {
        ArgHelper.ThrowIfNull(annotationKind);

        var existingAnnotations = node.GetAnnotations();
        if (existingAnnotations.Length == 0)
        {
            // If there aren't any existing annotations, we have nothing to remove.
            return node;
        }

        // First, determine how many annotations we'll need to remove.
        var foundCount = 0;

        foreach (var candidate in existingAnnotations)
        {
            if (candidate.Kind == annotationKind)
            {
                foundCount++;
            }
        }

        if (foundCount == 0)
        {
            // If we don't have anything to remove, we're done.
            return node;
        }

        var result = new SyntaxAnnotation[existingAnnotations.Length - foundCount];
        var index = 0;

        foreach (var candidate in existingAnnotations)
        {
            if (candidate.Kind != annotationKind)
            {
                result[index++] = candidate;
            }
        }

        Debug.Assert(index == result.Length);

        return (TNode)node.SetAnnotations(result);
    }

    public static TNode WithoutAnnotationsGreen<TNode>(this TNode node, IEnumerable<SyntaxAnnotation> annotations)
        where TNode : GreenNode
    {
        ArgHelper.ThrowIfNull(annotations);

        var existingAnnotations = node.GetAnnotations();
        if (existingAnnotations.Length == 0)
        {
            // If there aren't any existing annotations, we have nothing to remove.
            return node;
        }

        PooledHashSet<SyntaxAnnotation> annotationsToRemove;

        if (annotations.TryGetCount(out var count))
        {
            if (count == 0)
            {
                // No annotations to remove!
                return node;
            }

            // Set the capacity to our count to limit HashSet growth.
            annotationsToRemove = new PooledHashSet<SyntaxAnnotation>(count);
        }
        else
        {
            annotationsToRemove = new PooledHashSet<SyntaxAnnotation>();
        }

        try
        {
            annotationsToRemove.UnionWith(annotations);

            if (annotationsToRemove.Count == 0)
            {
                return node;
            }

            using var annotationsToKeep = new PooledArrayBuilder<SyntaxAnnotation>(existingAnnotations.Length);

            foreach (var candidate in existingAnnotations)
            {
                if (!annotationsToRemove.Contains(candidate))
                {
                    annotationsToKeep.Add(candidate);
                }
            }

            return (TNode)node.SetAnnotations(annotationsToKeep.ToArrayAndClear());
        }
        finally
        {
            annotationsToRemove.Dispose();
        }
    }

#nullable disable

    public static TNode WithDiagnosticsGreen<TNode>(this TNode node, RazorDiagnostic[] diagnostics)
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
