// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public sealed class ViewComponentTagHelperIntermediateNode : ExtensionIntermediateNode
{
    public override IntermediateNodeCollection Children { get; } = IntermediateNodeCollection.ReadOnly;

    public string ClassName { get; set; }

    public TagHelperDescriptor TagHelper { get; set; }

    public override void Accept(IntermediateNodeVisitor visitor)
        => AcceptExtensionNode(this, visitor);

    public override void WriteNode(CodeTarget target, CodeRenderingContext context)
    {
        if (!TryGetExtensionAndReportIfMissing<IViewComponentTagHelperTargetExtension>(target, context, out var extension))
        {
            return;
        }

        extension.WriteViewComponentTagHelper(context, this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(ClassName);

        formatter.WriteProperty(nameof(ClassName), ClassName);
        formatter.WriteProperty(nameof(TagHelper), TagHelper?.DisplayName);
    }
}
