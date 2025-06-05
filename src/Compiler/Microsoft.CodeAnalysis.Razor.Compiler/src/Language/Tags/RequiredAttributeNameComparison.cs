// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
///  Acceptable <see cref="RequiredAttributeDescriptor.Name"/> comparison modes.
/// </summary>
/// <remarks>
///  ⚠️ This enum are packed into one bit of <see cref="RequiredAttributeFlags"/> values.
///  Adding more than two values requires updating <see cref="RequiredAttributeFlags"/> and related logic.
/// </remarks>
public enum RequiredAttributeNameComparison
{
    /// <summary>
    ///  HTML attribute name case insensitively matches <see cref="RequiredAttributeDescriptor.Name"/>.
    /// </summary>
    FullMatch = 0,

    /// <summary>
    ///  HTML attribute name case insensitively starts with <see cref="RequiredAttributeDescriptor.Name"/>.
    /// </summary>
    PrefixMatch = 1,
}
