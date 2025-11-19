// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers;

/// <summary>
///  Acceptable <see cref="AttributeMatchingRule.NameComparison"/> comparison values.
/// </summary>
internal enum AttributeNameComparison : byte
{
    /// <summary>
    ///  HTML attribute name case insensitively matches <see cref="AttributeMatchingRule.Name"/>.
    /// </summary>
    FullMatch,

    /// <summary>
    ///  HTML attribute name case insensitively starts with <see cref="AttributeMatchingRule.Name"/>.
    /// </summary>
    PrefixMatch
}
