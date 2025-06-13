// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public sealed class DefaultTagHelperBodyIntermediateNode(TagMode tagMode, string tagName) : ExtensionIntermediateNode
{
    public TagMode TagMode { get; } = tagMode;
    public string TagName { get; } = tagName;

    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public DefaultTagHelperBodyIntermediateNode(TagHelperBodyIntermediateNode node, TagMode tagMode, string tagName)
        : this(tagMode, tagName)
    {
        Source = node.Source;
        Children.AddRange(node.Children);

        AddDiagnosticsFromNode(node);
    }

    public override void Accept(IntermediateNodeVisitor visitor)
        => AcceptExtensionNode(this, visitor);

    public override void WriteNode(CodeTarget target, CodeRenderingContext context)
    {
        if (!TryGetExtensionAndReportIfMissing<IDefaultTagHelperTargetExtension>(target, context, out var extension))
        {
            return;
        }

        extension.WriteTagHelperBody(context, this);
    }
}
