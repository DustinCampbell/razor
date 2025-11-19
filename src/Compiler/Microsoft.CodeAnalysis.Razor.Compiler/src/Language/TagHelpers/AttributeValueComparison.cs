// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers;

/// <summary>
///  Acceptable <see cref="AttributeMatchingRule.ValueComparison"/> values.
/// </summary>
internal enum AttributeValueComparison : byte
{
    /// <summary>
    ///  HTML attribute value always matches <see cref="AttributeMatchingRule.Value"/>.
    /// </summary>
    None,

    /// <summary>
    ///  HTML attribute value case sensitively matches <see cref="AttributeMatchingRule.Value"/>.
    /// </summary>
    FullMatch,

    /// <summary>
    ///  HTML attribute value case sensitively starts with <see cref="AttributeMatchingRule.Value"/>.
    /// </summary>
    PrefixMatch,

    /// <summary>
    ///  HTML attribute value case sensitively ends with <see cref="AttributeMatchingRule.Value"/>.
    /// </summary>
    SuffixMatch
}
