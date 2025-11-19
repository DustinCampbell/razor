// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers;

internal sealed class TagMatchingRule
{
    private readonly TagMatchingRuleFlags _flags;

    public RazorSymbol Parent { get; }
    public string TagName { get; }
    public string? ParentTag { get; }
    public TagStructure TagStructure { get; }

    public ImmutableArray<AttributeMatchingRule> Attributes { get; }
    public ImmutableArray<RazorDiagnostic> Diagnostics { get; }

    public bool CaseSensitive => _flags.IsFlagSet(TagMatchingRuleFlags.CaseSensitive);

    public TagMatchingRule(RazorSymbol parent, ref readonly TagMatchingRuleData data)
    {
        Parent = parent;

        _flags = data.Flags;
        TagName = data.TagName;
        ParentTag = data.ParentTag;
        TagStructure = data.TagStructure;

        Attributes = CreateAttributes(data.Attributes);
        Diagnostics = data.Diagnostics.ToImmutableArray();
    }

    private ImmutableArray<AttributeMatchingRule> CreateAttributes(ReadOnlySpan<AttributeMatchingRuleData> attributes)
    {
        if (attributes.Length == 0)
        {
            return [];
        }

        var array = new AttributeMatchingRule[attributes.Length];

        for (var i = 0; i < attributes.Length; i++)
        {
            array[i] = new(this, in attributes[i]);
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(array);
    }
}
