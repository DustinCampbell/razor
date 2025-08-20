// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

// We don't support 'complex' content for components (mixed C# and markup) right now.
// It's not clear yet if components will have a good scenario to use these constructs.
//
// This is where a lot of the complexity in the Razor/TagHelpers model creeps in and we
// might be able to avoid it if these features aren't needed.
internal class ComponentComplexAttributeContentPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
{
    // Run before other Component passes
    public override int Order => -1000;

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        if (!IsComponentDocument(documentNode))
        {
            return;
        }

        foreach (var node in documentNode.FindDescendantNodes<TagHelperIntermediateNode>())
        {
            ProcessAttributes(node);
        }
    }

    private static void ProcessAttributes(TagHelperIntermediateNode node)
    {
        var hasComponentTagHelpers = node.TagHelpers.Any(static t => t.IsComponentTagHelper);

        for (var i = node.Children.Count - 1; i >= 0; i--)
        {
            var child = node.Children[i];

            if (child is TagHelperDirectiveAttributeIntermediateNode directiveAttributeNode)
            {
                ProcessAttribute(directiveAttributeNode, directiveAttributeNode.OriginalAttributeName);
                continue;
            }

            if (!hasComponentTagHelpers)
            {
                continue;
            }

            switch (child)
            {
                case TagHelperPropertyIntermediateNode propertyNode:
                    ProcessAttribute(propertyNode, propertyNode.AttributeName);
                    break;

                case TagHelperHtmlAttributeIntermediateNode htmlNode:
                    ProcessAttribute(htmlNode, htmlNode.AttributeName);
                    break;
            }
        }
    }

    private static void ProcessAttribute(IntermediateNode node, string attributeName)
    {
        var issueDiagnostic = false;

        if (node.Children is [HtmlAttributeIntermediateNode { Children.Count: > 1 }])
        {
            // This case can be hit for a 'string' attribute
            issueDiagnostic = true;
        }
        else if (node.Children is [CSharpExpressionIntermediateNode { Children.Count: > 1 } csharpNode])
        {
            // This case can be hit when the attribute has an explicit @ inside, which
            // 'escapes' any special sugar we provide for codegen.
            //
            // There's a special case here for explicit expressions. See https://github.com/aspnet/Razor/issues/2203
            // handling this case as a tactical matter since it's important for lambdas.
            if (csharpNode.Children is [IntermediateToken { Content: "(" }, _, IntermediateToken { Content: ")" }])
            {
                csharpNode.Children.RemoveAt(2);
                csharpNode.Children.RemoveAt(0);
            }
            else
            {
                issueDiagnostic = true;
            }
        }
        else if (node.Children is [CSharpCodeIntermediateNode])
        {
            // This is the case when an attribute contains a code block @{ ... }
            // We don't support this.
            issueDiagnostic = true;
        }
        else if (node.Children.Count > 1)
        {
            // This is the common case for 'mixed' content
            issueDiagnostic = true;
        }

        if (issueDiagnostic)
        {
            node.AddDiagnostic(ComponentDiagnosticFactory.Create_UnsupportedComplexContent(node, attributeName));
        }
    }
}
