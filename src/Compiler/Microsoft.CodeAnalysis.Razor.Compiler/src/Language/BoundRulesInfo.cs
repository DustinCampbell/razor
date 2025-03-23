// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

internal readonly struct BoundRulesInfo(TagHelperDescriptor descriptor, ImmutableArray<TagMatchingRuleDescriptor> rules)
{
    public TagHelperDescriptor Descriptor { get; } = descriptor;
    public ImmutableArray<TagMatchingRuleDescriptor> Rules { get; } = rules;
}
