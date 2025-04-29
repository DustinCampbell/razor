// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DirectiveRemovalOptimizationPass : IntermediateNodePassBase, IRazorOptimizationPass
{
    public override int Order => DefaultFeatureOrder + 50;

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        using var _ = ListPool<IntermediateNodeReference<DirectiveIntermediateNode>>.GetPooledObject(out var directiveNodes);

        Visitor.Collect(documentNode, directiveNodes);

        foreach (var nodeReference in directiveNodes)
        {
            // Lift the diagnostics in the directive node up to the document node.
            if (nodeReference.Node.HasDiagnostics)
            {
                documentNode.Diagnostics.AddRange(nodeReference.Node.Diagnostics);
            }

            nodeReference.Remove();
        }
    }

    private sealed class Visitor : IntermediateNodeWalker
    {
        private readonly List<IntermediateNodeReference<DirectiveIntermediateNode>> _results;

        private Visitor(List<IntermediateNodeReference<DirectiveIntermediateNode>> results)
        {
            _results = results;
        }

        public static void Collect(DocumentIntermediateNode documentNode, List<IntermediateNodeReference<DirectiveIntermediateNode>> results)
        {
            var visitor = new Visitor(results);
            visitor.Visit(documentNode);
        }

        public override void VisitDirective(DirectiveIntermediateNode node)
        {
            _results.Add(new(Parent, node));
        }
    }
}
