﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Text;

using RazorSyntaxToken = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxToken;
using RazorSyntaxTokenList = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxTokenList;
using RazorSyntaxWalker = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxWalker;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

// There is already RazorSyntaxNode so not following that pattern for this alias
using SyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

internal class FormattingVisitor : RazorSyntaxWalker
{
    private const string HtmlTag = "html";

    private readonly bool _inGlobalNamespace;
    private readonly List<FormattingSpan> _spans;
    private FormattingBlockKind _currentBlockKind;
    private SyntaxNode? _currentBlock;
    private int _currentHtmlIndentationLevel = 0;
    private int _currentRazorIndentationLevel = 0;
    private int _currentComponentIndentationLevel = 0;
    private bool _isInClassBody = false;

    public FormattingVisitor(bool inGlobalNamespace)
    {
        _inGlobalNamespace = inGlobalNamespace;
        _spans = new List<FormattingSpan>();
        _currentBlockKind = FormattingBlockKind.Markup;
    }

    public IReadOnlyList<FormattingSpan> FormattingSpans => _spans;

    public override void VisitRazorCommentBlock(RazorCommentBlockSyntax node)
    {
        WriteBlock(node, FormattingBlockKind.Comment, razorCommentSyntax =>
        {
            // We only want to move the start of the comment into the right spot, so we only
            // create spans for the start.
            // The body of the comment, including whitespace before the "*@" is left exactly
            // as the user has it in the file.
            WriteSpan(razorCommentSyntax.StartCommentTransition, FormattingSpanKind.Transition);
            WriteSpan(razorCommentSyntax.StartCommentStar, FormattingSpanKind.MetaCode);
        });
    }

    public override void VisitCSharpCodeBlock(CSharpCodeBlockSyntax node)
    {
        if (node.Parent is CSharpStatementBodySyntax ||
            node.Parent is CSharpImplicitExpressionBodySyntax ||
            node.Parent is RazorDirectiveBodySyntax ||
            (_currentBlockKind == FormattingBlockKind.Directive &&
            node.Parent?.Parent is RazorDirectiveBodySyntax))
        {
            // If we get here, it means we don't want this code block to be considered significant.
            // Without this, we would have double indentation in places where
            // CSharpCodeBlock is used as a wrapper block in the syntax tree.

            if (node.Parent is not RazorDirectiveBodySyntax)
            {
                _currentRazorIndentationLevel++;
            }

            var isInCodeBlockDirective =
                node.Parent?.Parent?.Parent is RazorDirectiveSyntax directive &&
                directive.DirectiveDescriptor.Kind == DirectiveKind.CodeBlock;

            if (isInCodeBlockDirective)
            {
                // This means this is the code portion of an @code or @functions kind of block.
                _isInClassBody = true;
            }

            base.VisitCSharpCodeBlock(node);

            if (isInCodeBlockDirective)
            {
                // Finished visiting the code portion. We are no longer in it.
                _isInClassBody = false;
            }

            if (node.Parent is not RazorDirectiveBodySyntax)
            {
                _currentRazorIndentationLevel--;
            }

            return;
        }

        WriteBlock(node, FormattingBlockKind.Statement, base.VisitCSharpCodeBlock);
    }

    public override void VisitCSharpStatement(CSharpStatementSyntax node)
    {
        WriteBlock(node, FormattingBlockKind.Statement, base.VisitCSharpStatement);
    }

    public override void VisitCSharpExplicitExpression(CSharpExplicitExpressionSyntax node)
    {
        WriteBlock(node, FormattingBlockKind.Expression, base.VisitCSharpExplicitExpression);
    }

    public override void VisitCSharpImplicitExpression(CSharpImplicitExpressionSyntax node)
    {
        WriteBlock(node, FormattingBlockKind.Expression, base.VisitCSharpImplicitExpression);
    }

    public override void VisitRazorDirective(RazorDirectiveSyntax node)
    {
        WriteBlock(node, FormattingBlockKind.Directive, base.VisitRazorDirective);
    }

    public override void VisitCSharpTemplateBlock(CSharpTemplateBlockSyntax node)
    {
        WriteBlock(node, FormattingBlockKind.Template, base.VisitCSharpTemplateBlock);
    }

    public override void VisitMarkupBlock(MarkupBlockSyntax node)
    {
        WriteBlock(node, FormattingBlockKind.Markup, base.VisitMarkupBlock);
    }

    public override void VisitMarkupElement(MarkupElementSyntax node)
    {
        Visit(node.StartTag);

        // Void elements, like <meta> or <input> which don't need an end tag don't cause indentation.
        // We also cheat and treat the <html> tag as a void element, so it doesn't cause indentation,
        // as that's what the Html formatter does, to avoid one level of indentation in every html file.
        var voidElement = node.StartTag is { } startTag &&
            (startTag.IsVoidElement() || string.Equals(startTag.Name.Content, HtmlTag, StringComparison.OrdinalIgnoreCase));

        if (!voidElement)
        {
            _currentHtmlIndentationLevel++;
        }

        foreach (var child in node.Body)
        {
            Visit(child);
        }

        if (!voidElement)
        {
            _currentHtmlIndentationLevel--;
        }

        Visit(node.EndTag);
    }

    public override void VisitMarkupStartTag(MarkupStartTagSyntax node)
    {
        WriteBlock(node, FormattingBlockKind.Tag, n =>
        {
            var children = SyntaxUtilities.GetRewrittenMarkupStartTagChildren(node);
            foreach (var child in children)
            {
                Visit(child);
            }
        });
    }

    public override void VisitMarkupEndTag(MarkupEndTagSyntax node)
    {
        WriteBlock(node, FormattingBlockKind.Tag, n =>
        {
            var children = SyntaxUtilities.GetRewrittenMarkupEndTagChildren(node);

            foreach (var child in children)
            {
                Visit(child);
            }
        });
    }

    public override void VisitMarkupTagHelperElement(MarkupTagHelperElementSyntax node)
    {
        var isComponent = IsComponentTagHelperNode(node);
        // Components with cascading type parameters cause an extra level of indentation
        var componentIndentationLevels = isComponent && HasUnspecifiedCascadingTypeParameter(node) ? 2 : 1;

        var causesIndentation = isComponent;
        if (node.Parent is MarkupTagHelperElementSyntax parentComponent &&
            IsComponentTagHelperNode(parentComponent) &&
            ParentHasProperty(parentComponent, node.TagHelperInfo?.TagName))
        {
            causesIndentation = false;
        }

        Visit(node.StartTag);

        _currentHtmlIndentationLevel++;
        if (causesIndentation)
        {
            _currentComponentIndentationLevel += componentIndentationLevels;
        }

        foreach (var child in node.Body)
        {
            Visit(child);
        }

        if (causesIndentation)
        {
            Debug.Assert(_currentComponentIndentationLevel > 0, "Component indentation level should not be at 0.");
            _currentComponentIndentationLevel -= componentIndentationLevels;
        }

        _currentHtmlIndentationLevel--;

        Visit(node.EndTag);

        static bool IsComponentTagHelperNode(MarkupTagHelperElementSyntax node)
        {
            return node.TagHelperInfo?.BindingResult?.Descriptors is { Length: > 0 } descriptors &&
                   descriptors.Any(static d => d.IsComponentOrChildContentTagHelper);
        }

        static bool ParentHasProperty(MarkupTagHelperElementSyntax parentComponent, string? propertyName)
        {
            // If this is a child tag helper that match a property of its parent tag helper
            // then it means this specific node won't actually cause a change in indentation.
            // For example, the following two bits of Razor generate identical C# code, even though the code block is
            // nested in a different number of tag helper elements:
            //
            // <Component>
            //     @if (true)
            //     {
            //     }
            // </Component>
            //
            // and
            //
            // <Component>
            //     <ChildContent>
            //         @if (true)
            //         {
            //         }
            //     </ChildContent>
            // </Component>
            //
            // This code will not count "ChildContent" as causing indentation because its parent
            // has a property called "ChildContent".
            if (parentComponent.TagHelperInfo?.BindingResult.Descriptors.Any(d => d.BoundAttributes.Any(a => a.Name == propertyName)) ?? false)
            {
                return true;
            }

            return false;
        }

        static bool HasUnspecifiedCascadingTypeParameter(MarkupTagHelperElementSyntax node)
        {
            if (node.TagHelperInfo?.BindingResult?.Descriptors is not { Length: > 0 } descriptors)
            {
                return false;
            }

            // A cascading type parameter will mean the generated code will get a TypeInference class generated
            // for it, which we need to account for with an extra level of indentation in our expected C# indentation
            var hasCascadingGenericParameters = descriptors.Any(static d => d.SuppliesCascadingGenericParameters());
            if (!hasCascadingGenericParameters)
            {
                return false;
            }

            // BUT, because life wasn't mean to be easy, the indentation is only affected when the developer
            // doesn't specify any type parameter in the element itself as an attribute.

            // Get all type parameters for later use. Array is fine to use as the list should be tiny (I hope!!)
            var typeParameterNames = descriptors.SelectMany(d => d.GetTypeParameters().Select(p => p.Name)).ToArray();

            var attributes = node.StartTag.Attributes.OfType<MarkupTagHelperAttributeSyntax>();
            foreach (var attribute in attributes)
            {
                if (attribute.TagHelperAttributeInfo.Bound)
                {
                    var name = attribute.TagHelperAttributeInfo.Name;
                    if (typeParameterNames.Contains(name))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    public override void VisitMarkupTagHelperStartTag(MarkupTagHelperStartTagSyntax node)
    {
        WriteBlock(node, FormattingBlockKind.Tag, n =>
        {
            foreach (var child in n.LegacyChildren)
            {
                Visit(child);
            }
        });
    }

    public override void VisitMarkupTagHelperEndTag(MarkupTagHelperEndTagSyntax node)
    {
        WriteBlock(node, FormattingBlockKind.Tag, n =>
        {
            foreach (var child in n.LegacyChildren)
            {
                Visit(child);
            }
        });
    }

    public override void VisitMarkupAttributeBlock(MarkupAttributeBlockSyntax node)
    {
        WriteBlock(node, FormattingBlockKind.Markup, n =>
        {
            var equalsSyntax = SyntaxFactory.MarkupTextLiteral(new RazorSyntaxTokenList(node.EqualsToken), chunkGenerator: null);
            var mergedAttributePrefix = SyntaxUtilities.MergeTextLiterals(node.NamePrefix, node.Name, node.NameSuffix, equalsSyntax, node.ValuePrefix);
            Visit(mergedAttributePrefix);
            Visit(node.Value);
            Visit(node.ValueSuffix);
        });
    }

    public override void VisitMarkupTagHelperAttribute(MarkupTagHelperAttributeSyntax node)
    {
        WriteBlock(node, FormattingBlockKind.Tag, n =>
        {
            var equalsSyntax = SyntaxFactory.MarkupTextLiteral(new RazorSyntaxTokenList(node.EqualsToken), chunkGenerator: null);
            var mergedAttributePrefix = SyntaxUtilities.MergeTextLiterals(node.NamePrefix, node.Name, node.NameSuffix, equalsSyntax, node.ValuePrefix);
            Visit(mergedAttributePrefix);
            Visit(node.Value);
            Visit(node.ValueSuffix);
        });
    }

    public override void VisitMarkupTagHelperDirectiveAttribute(MarkupTagHelperDirectiveAttributeSyntax node)
    {
        Visit(node.Transition);
        Visit(node.Colon);
        Visit(node.Value);
    }

    public override void VisitMarkupMinimizedTagHelperDirectiveAttribute(MarkupMinimizedTagHelperDirectiveAttributeSyntax node)
    {
        Visit(node.Transition);
        Visit(node.Colon);
    }

    public override void VisitMarkupMinimizedAttributeBlock(MarkupMinimizedAttributeBlockSyntax node)
    {
        WriteBlock(node, FormattingBlockKind.Markup, n =>
        {
            var mergedAttributePrefix = SyntaxUtilities.MergeTextLiterals(node.NamePrefix, node.Name);
            Visit(mergedAttributePrefix);
        });
    }

    public override void VisitMarkupCommentBlock(MarkupCommentBlockSyntax node)
    {
        WriteBlock(node, FormattingBlockKind.HtmlComment, base.VisitMarkupCommentBlock);
    }

    public override void VisitMarkupDynamicAttributeValue(MarkupDynamicAttributeValueSyntax node)
    {
        WriteBlock(node, FormattingBlockKind.Markup, base.VisitMarkupDynamicAttributeValue);
    }

    public override void VisitMarkupTagHelperAttributeValue(MarkupTagHelperAttributeValueSyntax node)
    {
        WriteBlock(node, FormattingBlockKind.Markup, base.VisitMarkupTagHelperAttributeValue);
    }

    public override void VisitRazorMetaCode(RazorMetaCodeSyntax node)
    {
        if (node.Parent is MarkupTagHelperDirectiveAttributeSyntax { TagHelperAttributeInfo.Bound: true })
        {
            // For @bind attributes we want to pretend that we're in a Html context, so write this span as markup
            WriteSpan(node, FormattingSpanKind.Markup);
        }
        else
        {
            WriteSpan(node, FormattingSpanKind.MetaCode);
        }

        base.VisitRazorMetaCode(node);
    }

    public override void VisitCSharpTransition(CSharpTransitionSyntax node)
    {
        WriteSpan(node, FormattingSpanKind.Transition);
        base.VisitCSharpTransition(node);
    }

    public override void VisitMarkupTransition(MarkupTransitionSyntax node)
    {
        WriteSpan(node, FormattingSpanKind.Transition);
        base.VisitMarkupTransition(node);
    }

    public override void VisitCSharpStatementLiteral(CSharpStatementLiteralSyntax node)
    {
        // Workaround for a quirk of runtime code gen, where an empty marker is inserted before the close brace
        // of an explicit expression. ie, at the $$ below:
        //
        // @{
        //     something
        // $$}
        //
        // Writing a span for this empty marker will cause the close brace to be incorrectly indented, because its seen as
        // being "inside" the block.
        if (node.LiteralTokens is not [{ Kind: SyntaxKind.Marker }])
        {
            WriteSpan(node, FormattingSpanKind.Code);
        }

        base.VisitCSharpStatementLiteral(node);
    }

    public override void VisitCSharpExpressionLiteral(CSharpExpressionLiteralSyntax node)
    {
        WriteSpan(node, FormattingSpanKind.Code);
        base.VisitCSharpExpressionLiteral(node);
    }

    public override void VisitCSharpEphemeralTextLiteral(CSharpEphemeralTextLiteralSyntax node)
    {
        WriteSpan(node, FormattingSpanKind.Code);
        base.VisitCSharpEphemeralTextLiteral(node);
    }

    public override void VisitUnclassifiedTextLiteral(UnclassifiedTextLiteralSyntax node)
    {
        WriteSpan(node, FormattingSpanKind.None);
        base.VisitUnclassifiedTextLiteral(node);
    }

    public override void VisitMarkupLiteralAttributeValue(MarkupLiteralAttributeValueSyntax node)
    {
        WriteSpan(node, FormattingSpanKind.Markup);
        base.VisitMarkupLiteralAttributeValue(node);
    }

    public override void VisitMarkupTextLiteral(MarkupTextLiteralSyntax node)
    {
        if (node.Parent is MarkupLiteralAttributeValueSyntax)
        {
            base.VisitMarkupTextLiteral(node);
            return;
        }

        WriteSpan(node, FormattingSpanKind.Markup);
        base.VisitMarkupTextLiteral(node);
    }

    public override void VisitMarkupEphemeralTextLiteral(MarkupEphemeralTextLiteralSyntax node)
    {
        WriteSpan(node, FormattingSpanKind.Markup);
        base.VisitMarkupEphemeralTextLiteral(node);
    }

    private void WriteBlock<TNode>(TNode node, FormattingBlockKind kind, Action<TNode> handler) where TNode : SyntaxNode
    {
        var previousBlock = _currentBlock;
        var previousKind = _currentBlockKind;

        _currentBlock = node;
        _currentBlockKind = kind;

        handler(node);

        _currentBlock = previousBlock;
        _currentBlockKind = previousKind;
    }

    private void WriteSpan(SyntaxNode node, FormattingSpanKind kind)
    {
        if (node.IsMissing)
        {
            return;
        }

        Assumes.NotNull(_currentBlock);

        var span = new FormattingSpan(
            node.Span,
            _currentBlock.Span,
            kind,
            _currentBlockKind,
            _currentRazorIndentationLevel,
            _currentHtmlIndentationLevel,
            isInGlobalNamespace: _inGlobalNamespace,
            isInClassBody: _isInClassBody,
            _currentComponentIndentationLevel);

        _spans.Add(span);
    }

    private void WriteSpan(RazorSyntaxToken token, FormattingSpanKind kind)
    {
        if (token.IsMissing)
        {
            return;
        }

        Assumes.NotNull(_currentBlock);

        var span = new FormattingSpan(
            token.Span,
            _currentBlock.Span,
            kind,
            _currentBlockKind,
            _currentRazorIndentationLevel,
            _currentHtmlIndentationLevel,
            isInGlobalNamespace: _inGlobalNamespace,
            isInClassBody: _isInClassBody,
            _currentComponentIndentationLevel);

        _spans.Add(span);
    }
}
