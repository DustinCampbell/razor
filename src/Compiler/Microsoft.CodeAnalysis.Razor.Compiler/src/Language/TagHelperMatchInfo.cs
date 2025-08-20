// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

internal readonly record struct TagHelperMatchInfo(
    BoundAttributeDescriptor? Attribute,
    bool IsIndexerMatch,
    BoundAttributeParameterDescriptor? Parameter)
{
    [MemberNotNullWhen(true, nameof(Parameter))]
    public bool IsParameterMatch => Parameter is not null;
}
