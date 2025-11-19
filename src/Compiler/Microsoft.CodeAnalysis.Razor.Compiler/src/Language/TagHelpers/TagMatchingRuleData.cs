// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers;

internal readonly struct TagMatchingRuleData(
    TagMatchingRuleFlags flags,
    string tagName,
    string? parentTag,
    TagStructure tagStructure,
    ReadOnlyMemory<AttributeMatchingRuleData> attributes,
    ReadOnlyMemory<RazorDiagnostic> diagnostics)
{
    public TagMatchingRuleFlags Flags => flags;
    public string TagName => tagName;
    public string? ParentTag => parentTag;
    public TagStructure TagStructure => tagStructure;
    public ReadOnlySpan<AttributeMatchingRuleData> Attributes => attributes.Span;
    public ReadOnlySpan<RazorDiagnostic> Diagnostics => diagnostics.Span;

    public Checksum GetChecksum()
    {
        var builder = new Checksum.Builder();

        builder.Append((int)flags);
        builder.Append(tagName);
        builder.Append(parentTag);
        builder.Append((int)tagStructure);

        foreach (var attribute in attributes.Span)
        {
            builder.Append(attribute.GetChecksum());
        }

        foreach (var diagnostic in Diagnostics)
        {
            builder.Append(diagnostic.Checksum);
        }

        return builder.FreeAndGetChecksum();
    }
}
