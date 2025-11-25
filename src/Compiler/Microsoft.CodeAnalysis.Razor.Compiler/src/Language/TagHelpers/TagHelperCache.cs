// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers;

internal sealed class TagHelperCache : CleanableWeakCache<Checksum, TagHelperDescriptor>
{
    private const int CleanUpThreshold = 200;

    public static readonly TagHelperCache Default = new();

    public TagHelperCache()
        : base(CleanUpThreshold)
    {
    }
}
