// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RequiredAttributeDescriptor : TagHelperObject<RequiredAttributeDescriptor>
{
    private readonly RequiredAttributeFlags _flags;

    internal RequiredAttributeFlags Flags => _flags;

    public string Name { get; }
    public string? Value { get; }
    public string DisplayName { get; }

    public bool CaseSensitive => _flags.IsFlagSet(RequiredAttributeFlags.CaseSensitive);
    public bool IsDirectiveAttribute => _flags.IsFlagSet(RequiredAttributeFlags.IsDirectiveAttribute);

    public RequiredAttributeNameComparison NameComparison => _flags.GetNameComparison();
    public RequiredAttributeValueComparison ValueComparison => _flags.GetValueComparison();

    internal RequiredAttributeDescriptor(
        RequiredAttributeFlags flags,
        string name,
        string? value,
        string displayName,
        ImmutableArray<RazorDiagnostic> diagnostics)
        : base(diagnostics)
    {
        _flags = flags;

        Name = name;
        Value = value;
        DisplayName = displayName;
    }

    private protected override void BuildChecksum(in Checksum.Builder builder)
    {
        builder.AppendData((byte)_flags);
        builder.AppendData(Name);
        builder.AppendData(Value);
        builder.AppendData(DisplayName);
    }

    public override string ToString()
    {
        return DisplayName ?? base.ToString()!;
    }
}
