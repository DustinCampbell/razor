﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// Acceptable <see cref="RequiredAttributeDescriptor.Value"/> comparison modes.
/// </summary>
public enum RequiredAttributeValueComparison
{
    /// <summary>
    /// HTML attribute value always matches <see cref="Value"/>.
    /// </summary>
    None,

    /// <summary>
    /// HTML attribute value case sensitively matches <see cref="Value"/>.
    /// </summary>
    FullMatch,

    /// <summary>
    /// HTML attribute value case sensitively starts with <see cref="Value"/>.
    /// </summary>
    PrefixMatch,

    /// <summary>
    /// HTML attribute value case sensitively ends with <see cref="Value"/>.
    /// </summary>
    SuffixMatch,
}
