// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

internal static class RequiredAttributeFlagsExtensions
{
    public static RequiredAttributeNameComparison GetNameComparison(this RequiredAttributeFlags flags)
        => flags.IsFlagSet(RequiredAttributeFlags.IsNamePrefixMatch)
            ? RequiredAttributeNameComparison.PrefixMatch
            : RequiredAttributeNameComparison.FullMatch;

    public static void SetNameComparison(ref this RequiredAttributeFlags flags, RequiredAttributeNameComparison value)
    {
        flags.UpdateFlag(RequiredAttributeFlags.IsNamePrefixMatch, value == RequiredAttributeNameComparison.PrefixMatch);
    }

    public static RequiredAttributeValueComparison GetValueComparison(this RequiredAttributeFlags flags)
        => (RequiredAttributeValueComparison)((byte)(flags & RequiredAttributeFlags.ValueComparisonMask) >> 3);

    public static void SetValueComparison(ref this RequiredAttributeFlags flags, RequiredAttributeValueComparison value)
    {
        flags.ClearFlag(RequiredAttributeFlags.ValueComparisonMask);
        flags.SetFlag((RequiredAttributeFlags)((byte)value << 3));
    }
}
