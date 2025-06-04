// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using static Microsoft.AspNetCore.Razor.Language.RequiredAttributeDescriptor;

namespace Microsoft.AspNetCore.Razor.Language;

internal static class RequiredAttributeFlagsExtensions
{
    public static NameComparisonMode GetNameComparison(this RequiredAttributeFlags flags)
        => flags.IsFlagSet(RequiredAttributeFlags.IsNamePrefixMatch)
            ? NameComparisonMode.PrefixMatch
            : NameComparisonMode.FullMatch;

    public static void SetNameComparison(ref this RequiredAttributeFlags flags, NameComparisonMode value)
    {
        flags.UpdateFlag(RequiredAttributeFlags.IsNamePrefixMatch, value == NameComparisonMode.PrefixMatch);
    }

    public static ValueComparisonMode GetValueComparison(this RequiredAttributeFlags flags)
        => (ValueComparisonMode)((byte)(flags & RequiredAttributeFlags.ValueComparisonMask) >> 3);

    public static void SetValueComparison(ref this RequiredAttributeFlags flags, ValueComparisonMode value)
    {
        flags.ClearFlag(RequiredAttributeFlags.ValueComparisonMask);
        flags.SetFlag((RequiredAttributeFlags)((byte)value << 3));
    }
}
