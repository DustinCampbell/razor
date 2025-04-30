// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Components;

// Rewrites contiguous subtrees of HTML into a special node type to reduce the
// size of the Render tree.
//
// Does not preserve insignificant details of the HTML, like tag closing style
// or quote style.
internal class ComponentMarkupBlockPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
{
    private readonly RazorLanguageVersion _version;

    public ComponentMarkupBlockPass(RazorLanguageVersion version)
    {
        _version = version;
    }

    // Runs LATE because we want to destroy structure.
    //
    // We also need to run after ComponentMarkupDiagnosticPass to avoid destroying diagnostics
    // added in that pass.
    public override int Order => ComponentMarkupDiagnosticPass.DefaultOrder + 10;

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        if (!IsComponentDocument(documentNode))
        {
            return;
        }

        if (documentNode.Options.DesignTime)
        {
            // Nothing to do during design time.
            return;
        }

        using var _1 = ListPool<IntermediateNodeReference>.GetPooledObject(out var trees);
        using var _2 = StringBuilderPool.GetPooledObject(out var builder);

        FindHtmlTreeVisitor.Collect(documentNode, _version, trees);

        var rewriteVisitor = new RewriteVisitor(trees, builder);
        while (trees.Count > 0)
        {
            // Walk backwards since we did a postorder traversal.
            var reference = trees[^1];

            // Forcibly remove a node to prevent infinite loops.
            trees.RemoveAt(trees.Count - 1);

            // We want to fold together siblings where possible. To do this, first we find
            // the index of the node we're looking at now - then we need to walk backwards
            // and identify a set of contiguous nodes we can merge.
            var start = reference.Parent.Children.Count - 1;
            for (; start >= 0; start--)
            {
                if (ReferenceEquals(reference.Node, reference.Parent.Children[start]))
                {
                    break;
                }
            }

            // This is the current node. Check if the left sibling is always a candidate
            // for rewriting. Due to the order we processed the nodes, we know that the
            // left sibling is next in the list to process if it's a candidate.
            var end = start;
            while (start - 1 >= 0)
            {
                var candidate = reference.Parent.Children[start - 1];
                if (trees.Count == 0 || !ReferenceEquals(trees[^1].Node, candidate))
                {
                    // This means the we're out of nodes, or the left sibling is not in the list.
                    break;
                }

                // This means that the left sibling is valid to merge.
                start--;

                // Remove this since we're combining it.
                trees.RemoveAt(trees.Count - 1);
            }

            // As a degenerate case, don't bother rewriting an single HtmlContent node
            // It doesn't add any value.
            if (end - start == 0 && reference.Node is HtmlContentIntermediateNode)
            {
                continue;
            }

            // Now we know the range of nodes to rewrite (end is inclusive)
            var length = end + 1 - start;
            while (length > 0)
            {
                // Keep using start since we're removing nodes.
                var node = reference.Parent.Children[start];
                reference.Parent.Children.RemoveAt(start);

                rewriteVisitor.Visit(node);

                length--;
            }

            reference.Parent.Children.Insert(start, new MarkupBlockIntermediateNode()
            {
                Content = builder.ToString(),
            });

            builder.Clear();
        }
    }

    // Finds HTML-blocks using a postorder traversal. We store nodes in an
    // ordered list so we can avoid redundant rewrites.
    //
    // Consider a case like:
    //  <div>
    //    <a href="...">click me</a>
    //  </div>
    //
    // We would store both the div and a tag in a list, but make sure to visit
    // the div first. Then when we process the div (recursively), we would remove
    // the a from the list.
    private class FindHtmlTreeVisitor : IntermediateNodeWalker
    {
        private readonly RazorLanguageVersion _version;
        private readonly List<IntermediateNodeReference> _results;
        private bool _foundNonHtml;

        private FindHtmlTreeVisitor(RazorLanguageVersion version, List<IntermediateNodeReference> results)
        {
            _version = version;
            _results = results;
        }

        public static void Collect(DocumentIntermediateNode documentNode, RazorLanguageVersion version, List<IntermediateNodeReference> results)
        {
            var visitor = new FindHtmlTreeVisitor(version, results);
            visitor.Visit(documentNode);
        }

        public override void VisitDefault(IntermediateNode node)
        {
            // If we get here, we found a non-HTML node. Keep traversing.
            _foundNonHtml = true;
            base.VisitDefault(node);
        }

        public override void VisitMarkupElement(MarkupElementIntermediateNode node)
        {
            // We need to restore the state after processing this node.
            // We might have found a leaf-block of HTML, but that shouldn't
            // affect our parent's state.
            var originalState = _foundNonHtml;

            _foundNonHtml = false;

            if (node.HasDiagnostics)
            {
                // Treat node with errors as non-HTML - don't let the parent rewrite this either.
                _foundNonHtml = true;
            }

            if (_version <= RazorLanguageVersion.Version_7_0 &&
                string.Equals("script", node.TagName, StringComparison.OrdinalIgnoreCase))
            {
                // Treat script tags as non-HTML in .NET 7 and earlier.
                _foundNonHtml = true;
            }
            else if (string.Equals("option", node.TagName, StringComparison.OrdinalIgnoreCase))
            {
                // Also, treat <option>...</option> as non-HTML - we don't want it to be coalesced so that we can support setting "selected" attribute on it.
                // We only care about option tags that are nested under a select tag.
                foreach (var ancestor in Ancestors)
                {
                    if (ancestor is MarkupElementIntermediateNode element &&
                        string.Equals("select", element.TagName, StringComparison.OrdinalIgnoreCase))
                    {
                        _foundNonHtml = true;
                        break;
                    }
                }
            }

            base.VisitDefault(node);

            if (!_foundNonHtml)
            {
                Debug.Assert(HasParent);
                _results.Add(new(Parent, node));
            }

            _foundNonHtml = originalState |= _foundNonHtml;
        }

        public override void VisitHtmlAttribute(HtmlAttributeIntermediateNode node)
        {
            if (node.HasDiagnostics)
            {
                // Treat node with errors as non-HTML
                _foundNonHtml = true;
            }

            // Visit Children
            base.VisitDefault(node);
        }

        public override void VisitHtmlAttributeValue(HtmlAttributeValueIntermediateNode node)
        {
            if (node.HasDiagnostics)
            {
                // Treat node with errors as non-HTML
                _foundNonHtml = true;
            }

            // Visit Children
            base.VisitDefault(node);
        }

        public override void VisitHtml(HtmlContentIntermediateNode node)
        {
            // We need to restore the state after processing this node.
            // We might have found a leaf-block of HTML, but that shouldn't
            // affect our parent's state.
            var originalState = _foundNonHtml;

            _foundNonHtml = false;

            if (node.HasDiagnostics)
            {
                // Treat node with errors as non-HTML
                _foundNonHtml = true;
            }

            // Visit Children
            base.VisitDefault(node);

            if (!_foundNonHtml)
            {
                Debug.Assert(HasParent);
                _results.Add(new(Parent, node));
            }

            _foundNonHtml = originalState |= _foundNonHtml;
        }

        public override void VisitToken(IntermediateToken node)
        {
            if (node.HasDiagnostics)
            {
                // Treat node with errors as non-HTML
                _foundNonHtml = true;
            }

            if (node.IsCSharp)
            {
                _foundNonHtml = true;
            }
        }
    }

    private sealed class RewriteVisitor(List<IntermediateNodeReference> trees, StringBuilder builder) : IntermediateNodeWalker
    {
        public override void VisitMarkupElement(MarkupElementIntermediateNode node)
        {
            for (var i = 0; i < trees.Count; i++)
            {
                // Remove this node if it's in the list. This ensures that we don't
                // do redundant operations.
                if (ReferenceEquals(trees[i].Node, node))
                {
                    trees.RemoveAt(i);
                    break;
                }
            }

            var isVoid = Legacy.ParserHelpers.VoidElements.Contains(node.TagName);
            var hasBodyContent = node.Body.Any();

            builder.Append('<');
            builder.Append(node.TagName);

            foreach (var attribute in node.Attributes)
            {
                Visit(attribute);
            }

            // If for some reason a void element contains body, then treat it as a
            // start/end tag.
            if (!hasBodyContent && isVoid)
            {
                // void
                builder.Append('>');
                return;
            }
            else if (!hasBodyContent)
            {
                // In HTML5, we can't have self-closing non-void elements, so explicitly
                // add a close tag
                builder.Append("></");
                builder.Append(node.TagName);
                builder.Append('>');
                return;
            }

            // start/end tag with body.
            builder.Append('>');

            foreach (var item in node.Body)
            {
                Visit(item);
            }

            builder.Append("</");
            builder.Append(node.TagName);
            builder.Append('>');
        }

        public override void VisitHtmlAttribute(HtmlAttributeIntermediateNode node)
        {
            builder.Append(' ');
            builder.Append(node.AttributeName);

            if (node.Children.Count == 0)
            {
                // Minimized attribute
                return;
            }

            // We examine the node.Prefix (e.g. " onfocus='" or " on focus=\"")
            // to preserve the quote type that is used in the original markup.
            var quoteType = node.Prefix.EndsWith('\'') ? "'" : "\"";

            builder.Append('=');
            builder.Append(quoteType);

            // Visit Children
            base.VisitDefault(node);

            builder.Append(quoteType);
        }

        public override void VisitHtmlAttributeValue(HtmlAttributeValueIntermediateNode node)
        {
            foreach (var child in node.Children)
            {
                builder.Append(node.Prefix);

                if (child is IntermediateToken token)
                {
                    builder.Append(token.Content);
                }
            }
        }

        public override void VisitHtml(HtmlContentIntermediateNode node)
        {
            foreach (var child in node.Children)
            {
                if (child is IntermediateToken token)
                {
                    builder.Append(token.Content);
                }
            }
        }
    }
}
