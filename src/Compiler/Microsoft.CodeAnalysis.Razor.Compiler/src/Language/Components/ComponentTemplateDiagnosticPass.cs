// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal class ComponentTemplateDiagnosticPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
{
    // Runs after components/eventhandlers/ref/bind. We need to check for templates in all of those
    // places.
    public override int Order => 150;

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        if (!IsComponentDocument(documentNode))
        {
            return;
        }

        using var _ = ListPool<IntermediateNodeReference>.GetPooledObject(out var candidates);

        Visitor.Collect(documentNode, candidates);

        foreach (var candidate in candidates)
        {
            candidate.Parent.Diagnostics.Add(ComponentDiagnosticFactory.Create_TemplateInvalidLocation(candidate.Node.Source));

            // Remove the offending node since we don't know how to render it. This means that the user won't get C#
            // completion at this location, which is fine because it's inside an HTML attribute.
            candidate.Remove();
        }
    }

    private class Visitor : IntermediateNodeWalker, IExtensionIntermediateNodeVisitor<TemplateIntermediateNode>
    {
        private readonly  List<IntermediateNodeReference> _results;

        private Visitor(List<IntermediateNodeReference> results)
        {
            _results = results;
        }

        public static void Collect(DocumentIntermediateNode documentNode, List<IntermediateNodeReference> results)
        {
            var visitor = new Visitor(results);
            visitor.Visit(documentNode);
        }

        public void VisitExtension(TemplateIntermediateNode node)
        {
            // We found a template, let's check where it's located.
            foreach (var ancestor in Ancestors)
            {
                // HtmlAttributeIntermediateNode: Inside markup attribute
                // ComponentAttributeIntermediateNode: Inside component attribute
                // TagHelperPropertyIntermediateNode: Inside malformed ref attribute
                // TagHelperDirectiveAttributeIntermediateNode: Inside directive attribute

                if (ancestor is HtmlAttributeIntermediateNode
                             or ComponentAttributeIntermediateNode
                             or TagHelperPropertyIntermediateNode
                             or TagHelperDirectiveAttributeIntermediateNode)
                {
                    Debug.Assert(HasParent);
                    _results.Add(IntermediateNodeReference.Create(Parent, node));
                }
            }
        }
    }
}
