// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language.TagHelpers;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial class AllowedChildTagDescriptorBuilder : TagHelperObjectBuilder<AllowedChildTagDescriptor>
{
    [AllowNull]
    private TagHelperDescriptorBuilder _parent;

    private AllowedChildTagDescriptorBuilder()
    {
    }

    internal AllowedChildTagDescriptorBuilder(TagHelperDescriptorBuilder parent)
    {
        _parent = parent;
    }

    public string? Name { get; set; }
    public string? DisplayName { get; set; }

    private protected override AllowedChildTagDescriptor BuildCore(ImmutableArray<RazorDiagnostic> diagnostics)
    {
        var name = Name ?? string.Empty;
        var displayName = DisplayName ?? Name ?? string.Empty;

        var checksum = ChecksumFactory.ComputeForAllowedChildTag(name, displayName, diagnostics);

        return new AllowedChildTagDescriptor(name, displayName, checksum, diagnostics);
    }

    private protected override void CollectDiagnostics(ref PooledArrayBuilder<RazorDiagnostic> diagnostics)
    {
        if (Name.IsNullOrWhiteSpace())
        {
            var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidRestrictedChildNullOrWhitespace(_parent.GetDisplayName());

            diagnostics.Add(diagnostic);
        }
        else if (Name != TagHelperMatchingConventions.ElementCatchAllName)
        {
            foreach (var character in Name)
            {
                if (char.IsWhiteSpace(character) || HtmlConventions.IsInvalidNonWhitespaceHtmlCharacters(character))
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidRestrictedChild(_parent.GetDisplayName(), Name, character);

                    diagnostics.Add(diagnostic);
                }
            }
        }
    }
}
