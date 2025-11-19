// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;

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

    private protected override void BuildChecksum(ref readonly Checksum.Builder builder)
    {
        GetValues(out var name, out var displayName);

        AllowedChildTagDescriptor.AppendChecksumValues(in builder, name, displayName);
    }

    private protected override AllowedChildTagDescriptor BuildCore(ImmutableArray<RazorDiagnostic> diagnostics)
    {
        GetValues(out var name, out var displayName);

        return new(name, displayName, diagnostics);
    }

    private void GetValues(out string name, out string displayName)
    {
        name = Name ?? string.Empty;
        displayName = DisplayName ?? Name ?? string.Empty;
    }

    private protected override void CollectDiagnostics(ref PooledHashSet<RazorDiagnostic> diagnostics)
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
