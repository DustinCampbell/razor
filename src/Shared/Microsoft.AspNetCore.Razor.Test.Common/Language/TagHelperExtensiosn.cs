// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

internal static class TagHelperExtensiosn
{
    public static bool IsDefaultKind(this BoundAttributeDescriptor attribute)
    {
        ArgHelper.ThrowIfNull(attribute);

        return attribute.Parent.Kind == TagHelperConventions.DefaultKind;
    }

    public static bool IsDefaultKind(this BoundAttributeParameterDescriptor parameter)
    {
        ArgHelper.ThrowIfNull(parameter);

        return parameter.Parent.Parent.Kind == TagHelperConventions.DefaultKind;
    }
}
