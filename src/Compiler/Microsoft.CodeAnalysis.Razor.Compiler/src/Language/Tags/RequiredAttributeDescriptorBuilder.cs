// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial class RequiredAttributeDescriptorBuilder : TagHelperObjectBuilder<RequiredAttributeDescriptor>
{
    [AllowNull]
    private TagMatchingRuleDescriptorBuilder _parent;
    private RequiredAttributeFlags _flags;

    private RequiredAttributeDescriptorBuilder()
    {
    }

    internal RequiredAttributeDescriptorBuilder(TagMatchingRuleDescriptorBuilder parent)
    {
        _parent = parent;
    }

    public string? Name { get; set; }
    public string? Value { get; set; }

    internal bool CaseSensitive => _parent.CaseSensitive;

    public bool IsDirectiveAttribute
    {
        get => _flags.IsFlagSet(RequiredAttributeFlags.IsDirectiveAttribute);
        set => _flags.UpdateFlag(RequiredAttributeFlags.IsDirectiveAttribute, value);
    }

    public RequiredAttributeNameComparison NameComparison
    {
        get => _flags.GetNameComparison();
        set => _flags.SetNameComparison(value);
    }

    public RequiredAttributeValueComparison ValueComparison
    {
        get => _flags.GetValueComparison();
        set => _flags.SetValueComparison(value);
    }

    private protected override RequiredAttributeDescriptor BuildCore(ImmutableArray<RazorDiagnostic> diagnostics)
    {
        var flags = _flags;

        if (CaseSensitive)
        {
            flags.SetFlag(RequiredAttributeFlags.CaseSensitive);
        }

        var displayName = GetDisplayName();

        return new RequiredAttributeDescriptor(
            flags,
            Name ?? string.Empty,
            Value,
            displayName,
            diagnostics);
    }

    private string GetDisplayName()
    {
        return (NameComparison == RequiredAttributeNameComparison.PrefixMatch ? string.Concat(Name, "...") : Name) ?? string.Empty;
    }

    private protected override void CollectDiagnostics(ref PooledHashSet<RazorDiagnostic> diagnostics)
    {
        if (Name.IsNullOrWhiteSpace())
        {
            var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedAttributeNameNullOrWhitespace();

            diagnostics.Add(diagnostic);
        }
        else
        {
            var name = Name.AsSpan();
            var isDirectiveAttribute = IsDirectiveAttribute;
            if (isDirectiveAttribute && name[0] == '@')
            {
                name = name[1..];
            }
            else if (isDirectiveAttribute)
            {
                var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidRequiredDirectiveAttributeName(GetDisplayName(), Name);

                diagnostics.Add(diagnostic);
            }

            foreach (var ch in name)
            {
                if (char.IsWhiteSpace(ch) || HtmlConventions.IsInvalidNonWhitespaceHtmlCharacters(ch))
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedAttributeName(Name, ch);

                    diagnostics.Add(diagnostic);
                }
            }
        }
    }
}
