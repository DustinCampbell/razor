// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
///  Acceptable <see cref="RequiredAttributeDescriptor.Value"/> comparison modes.
/// </summary>
/// <remarks>
///  ⚠️ This enum are packed into two bits of <see cref="RequiredAttributeFlags"/> values.
///  Adding more than four values requires updating <see cref="RequiredAttributeFlags"/> and related logic.
/// </remarks>
public enum RequiredAttributeValueComparison
{
    /// <summary>
    ///  HTML attribute value always matches <see cref="RequiredAttributeDescriptor.Value"/>.
    /// </summary>
    None,

    /// <summary>
    ///  HTML attribute value case sensitively matches <see cref="RequiredAttributeDescriptor.Value"/>.
    /// </summary>
    FullMatch,

    /// <summary>
    ///  HTML attribute value case sensitively starts with <see cref="RequiredAttributeDescriptor.Value"/>.
    /// </summary>
    PrefixMatch,

    /// <summary>
    ///  HTML attribute value case sensitively ends with <see cref="RequiredAttributeDescriptor.Value"/>.
    /// </summary>
    SuffixMatch,
}
