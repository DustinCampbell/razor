﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal sealed partial class MarkupMinimizedTagHelperAttributeSyntax
{
    private static readonly string TagHelperAttributeInfoKey = typeof(TagHelperAttributeInfo).Name;

    public TagHelperAttributeInfo TagHelperAttributeInfo
    {
        get
        {
            var tagHelperAttributeInfo = this.GetAnnotationValue(TagHelperAttributeInfoKey) as TagHelperAttributeInfo;
            return tagHelperAttributeInfo;
        }
    }

    public MarkupMinimizedTagHelperAttributeSyntax WithTagHelperAttributeInfo(TagHelperAttributeInfo info)
    {
        var newGreen = Green.WithAnnotationsGreen([.. GetAnnotations(), new(TagHelperAttributeInfoKey, info)]);

        return (MarkupMinimizedTagHelperAttributeSyntax)newGreen.CreateRed(Parent, Position);
    }
}
