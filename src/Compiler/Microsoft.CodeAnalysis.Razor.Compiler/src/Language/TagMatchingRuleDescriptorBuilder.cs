// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language.TagHelpers;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial class TagMatchingRuleDescriptorBuilder : TagHelperObjectBuilder<TagMatchingRuleDescriptor>
{
    [AllowNull]
    private TagHelperDescriptorBuilder _parent;

    private TagMatchingRuleDescriptorBuilder()
    {
    }

    internal TagMatchingRuleDescriptorBuilder(TagHelperDescriptorBuilder parent)
    {
        _parent = parent;
    }

    public string? TagName { get; set; }
    public string? ParentTag { get; set; }
    public TagStructure TagStructure { get; set; }

    internal bool CaseSensitive => _parent.CaseSensitive;

    public TagHelperObjectBuilderCollection<RequiredAttributeDescriptor, RequiredAttributeDescriptorBuilder> Attributes { get; }
        = new(RequiredAttributeDescriptorBuilder.Pool);

    public TagMatchingRuleDescriptorBuilder AddAttribute(
        string name,
        RequiredAttributeNameComparison nameComparison = default,
        string? value = null,
        RequiredAttributeValueComparison valueComparison = default,
        bool isDirectiveAttribute = false,
        ImmutableArray<RazorDiagnostic> diagnostics = default)
    {
        var builder = RequiredAttributeDescriptorBuilder.GetInstance(this);
        Attributes.Add(builder);

        builder.Name = name;
        builder.NameComparison = nameComparison;
        builder.Value = value;
        builder.ValueComparison = valueComparison;
        builder.IsDirectiveAttribute = isDirectiveAttribute;

        if (!diagnostics.IsDefaultOrEmpty)
        {
            builder.Diagnostics.AddRange(diagnostics);
        }

        return this;
    }

    private protected override TagMatchingRuleDescriptor BuildCore(ImmutableArray<RazorDiagnostic> diagnostics)
    {
        var tagName = TagName ?? string.Empty;
        var parentTag = ParentTag;
        var tagStructure = TagStructure;
        var caseSensitive = CaseSensitive;
        var attributes = Attributes.ToImmutable();

        var checksum = ChecksumFactory.ComputeForTagMatchingRule(
            tagName, parentTag, tagStructure, caseSensitive, attributes, diagnostics);

        return new TagMatchingRuleDescriptor(
            tagName, parentTag, tagStructure, caseSensitive, attributes, checksum, diagnostics);
    }

    private protected override void CollectDiagnostics(ref PooledArrayBuilder<RazorDiagnostic> diagnostics)
    {
        if (TagName.IsNullOrWhiteSpace())
        {
            var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedTagNameNullOrWhitespace();

            diagnostics.Add(diagnostic);
        }
        else if (TagName != TagHelperMatchingConventions.ElementCatchAllName)
        {
            foreach (var character in TagName)
            {
                if (char.IsWhiteSpace(character) || HtmlConventions.IsInvalidNonWhitespaceHtmlCharacters(character))
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedTagName(TagName, character);

                    diagnostics.Add(diagnostic);
                }
            }
        }

        if (ParentTag != null)
        {
            if (ParentTag.IsNullOrWhiteSpace())
            {
                var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedParentTagNameNullOrWhitespace();

                diagnostics.Add(diagnostic);
            }
            else
            {
                foreach (var character in ParentTag)
                {
                    if (char.IsWhiteSpace(character) || HtmlConventions.IsInvalidNonWhitespaceHtmlCharacters(character))
                    {
                        var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedParentTagName(ParentTag, character);

                        diagnostics.Add(diagnostic);
                    }
                }
            }
        }
    }
}
