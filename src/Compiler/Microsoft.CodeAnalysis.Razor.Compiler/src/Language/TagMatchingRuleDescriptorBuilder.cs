// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;

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

    public void Attribute(Action<RequiredAttributeDescriptorBuilder> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = RequiredAttributeDescriptorBuilder.GetInstance(this);
        configure(builder);
        Attributes.Add(builder);
    }

    private protected override void BuildChecksum(ref readonly Checksum.Builder builder)
    {
        GetValues(out var tagName, out var parentTag, out var tagStructure, out var caseSensitive);

        TagMatchingRuleDescriptor.AppendChecksumValues(in builder, tagName, parentTag, tagStructure, caseSensitive);

        foreach (var attribute in Attributes)
        {
            builder.Append(attribute.GetChecksum());
        }
    }

    private protected override TagMatchingRuleDescriptor BuildCore(ImmutableArray<RazorDiagnostic> diagnostics)
    {
        GetValues(out var tagName, out var parentTag, out var tagStructure, out var caseSensitive);

        return new(tagName, parentTag, tagStructure, caseSensitive, Attributes.BuildAll(), diagnostics);
    }

    private void GetValues(out string tagName, out string? parentTag, out TagStructure tagStructure, out bool caseSensitive)
    {
        tagName = TagName ?? string.Empty;
        parentTag = ParentTag;
        tagStructure = TagStructure;
        caseSensitive = CaseSensitive;
    }

    private protected override void CollectDiagnostics(ref PooledHashSet<RazorDiagnostic> diagnostics)
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
