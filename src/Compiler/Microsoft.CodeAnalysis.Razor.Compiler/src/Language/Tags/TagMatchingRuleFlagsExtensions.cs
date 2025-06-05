// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

internal static class TagMatchingRuleFlagsExtensions
{
    private const int TagStructureShift = 1;

    public static TagStructure GetTagStructure(this TagMatchingRuleFlags flags)
        => (TagStructure)((byte)(flags & TagMatchingRuleFlags.TagStructureMask) >> TagStructureShift);

    public static void SetTagStructure(ref this TagMatchingRuleFlags flags, TagStructure value)
    {
        flags.ClearFlag(TagMatchingRuleFlags.TagStructureMask);
        flags.SetFlag((TagMatchingRuleFlags)((byte)value << TagStructureShift));
    }
}
