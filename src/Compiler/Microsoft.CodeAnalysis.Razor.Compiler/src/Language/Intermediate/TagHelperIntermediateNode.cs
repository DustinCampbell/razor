// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class TagHelperIntermediateNode(
    string tagName,
    TagMode tagMode,
    ImmutableArray<TagHelperDescriptor> tagHelpers = default)
    : IntermediateNode
{
    public string TagName { get; } = tagName;
    public TagMode TagMode { get; } = tagMode;
    public ImmutableArray<TagHelperDescriptor> TagHelpers { get; } = tagHelpers.NullToEmpty();

    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitTagHelper(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(TagName);

        formatter.WriteProperty(nameof(TagHelpers), string.Join(", ", TagHelpers, static t => t.DisplayName));
        formatter.WriteProperty(nameof(TagMode), TagMode.ToString());
        formatter.WriteProperty(nameof(TagName), TagName);
    }
}
