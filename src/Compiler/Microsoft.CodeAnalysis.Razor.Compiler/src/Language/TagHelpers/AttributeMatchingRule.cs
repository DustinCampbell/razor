// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers;

internal sealed class AttributeMatchingRule
{
    private readonly AttributeMatchingRuleFlags _flags;

    public TagMatchingRule Parent { get; }
    public string Name { get; }
    public AttributeNameComparison NameComparison { get; }
    public string? Value { get; }
    public AttributeValueComparison ValueComparison { get; }

    public ImmutableArray<RazorDiagnostic> Diagnostics { get; }

    public string DisplayName
        => field ??= NameComparison == AttributeNameComparison.PrefixMatch
            ? Name + "..."
            : Name;

    public bool CaseSensitive => _flags.IsFlagSet(AttributeMatchingRuleFlags.CaseSensitive);
    public bool IsDirectiveAttribute => _flags.IsFlagSet(AttributeMatchingRuleFlags.IsDirectiveAttribute);

    public AttributeMatchingRule(
        TagMatchingRule parent,
        ref readonly AttributeMatchingRuleData data)
    {
        Parent = parent;
        Name = data.Name;
        NameComparison = data.NameComparison;
        Value = data.Value;
        ValueComparison = data.ValueComparison;
        Diagnostics = data.Diagnostics.ToImmutableArray();
    }

    public override string ToString()
        => DisplayName;
}
