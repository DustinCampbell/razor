// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal class ComponentSplatLoweringPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
{
    // Run after component lowering pass
    public override int Order => 50;

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        if (!IsComponentDocument(documentNode))
        {
            return;
        }

        var references = documentNode.FindDescendantReferences<TagHelperDirectiveAttributeIntermediateNode>();
        var parents = new HashSet<IntermediateNode>();
        for (var i = 0; i < references.Count; i++)
        {
            parents.Add(references[i].Parent);
        }

        for (var i = 0; i < references.Count; i++)
        {
            var reference = references[i];
            var node = (TagHelperDirectiveAttributeIntermediateNode)reference.Node;
            if (node.TagHelper.IsSplatTagHelper())
            {
                reference.Replace(RewriteUsage(reference.Parent, node));
            }
        }
    }

    private IntermediateNode RewriteUsage(IntermediateNode parent, TagHelperDirectiveAttributeIntermediateNode node)
    {
        var result = new SplatIntermediateNode()
        {
            Source = node.Source,
        };

        using var _ = ListPool<IntermediateToken>.GetPooledObject(out var tokens);
        node.CollectDescendantNodes(tokens, static t => t.IsCSharp);

        result.Children.AddRange(tokens);
        result.Diagnostics.AddRange(node.Diagnostics);
        return result;
    }
}
