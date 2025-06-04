// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

[Flags]
internal enum RequiredAttributeFlags : byte
{
    CaseSensitive = 1 << 0,
    IsDirectiveAttribute = 1 << 1,

    /// <summary>
    ///  <see cref="RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch"/> if set;
    ///  otherwise, <see cref="RequiredAttributeDescriptor.NameComparisonMode.FullMatch"/>.
    /// </summary>
    IsNamePrefixMatch = 1 << 2,

    /// <summary>
    ///  Mask for extracting the <see cref="RequiredAttributeDescriptor.ValueComparisonMode"/> value.
    /// </summary>
    ValueComparisonMask = (1 << 3) | (1 << 4)
}
