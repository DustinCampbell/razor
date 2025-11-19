// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers;

internal readonly struct AttributeMatchingRuleData(
    AttributeMatchingRuleFlags flags,
    string name,
    AttributeNameComparison nameComparison,
    string? value,
    AttributeValueComparison valueComparison,
    ReadOnlyMemory<RazorDiagnostic> diagnostics)
{
    public AttributeMatchingRuleFlags Flags => flags;
    public string Name => name;
    public AttributeNameComparison NameComparison => nameComparison;
    public string? Value => value;
    public AttributeValueComparison ValueComparison => valueComparison;
    public ReadOnlySpan<RazorDiagnostic> Diagnostics => diagnostics.Span;

    public Checksum GetChecksum()
    {
        var builder = new Checksum.Builder();

        builder.Append((byte)flags);
        builder.Append(name);
        builder.Append((byte)nameComparison);
        builder.Append(value);
        builder.Append((byte)valueComparison);

        foreach (var diagnostic in Diagnostics)
        {
            builder.Append(diagnostic.Checksum);
        }

        return builder.FreeAndGetChecksum();
    }
}
