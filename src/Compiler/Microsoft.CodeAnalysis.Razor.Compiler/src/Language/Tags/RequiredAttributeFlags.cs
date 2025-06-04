// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

[Flags]
internal enum RequiredAttributeFlags : byte
{
    CaseSensitive = 0x01,
    IsDirectiveAttribute = 0x02,

    /// <summary>
    ///  <see cref="RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch"/> if set;
    ///  otherwise, <see cref="RequiredAttributeDescriptor.NameComparisonMode.FullMatch"/>.
    /// </summary>
    IsNamePrefixMatch = 0x04,
}
