// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

[Flags]
internal enum TagMatchingRuleFlags : byte
{
    CaseSensitive = 1 << 0,

    /// <summary>
    ///  Mask for extracting the <see cref="TagStructure"/> value.
    /// </summary>
    TagStructureMask = (1 << 1) | (1 << 2)
}
