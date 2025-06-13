// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public sealed class DefaultTagHelperHtmlAttributeIntermediateNode(string attributeName, AttributeStructure attributeStructure) : ExtensionIntermediateNode
{
    public string AttributeName { get; } = attributeName;
    public AttributeStructure AttributeStructure { get; } = attributeStructure;

    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public DefaultTagHelperHtmlAttributeIntermediateNode(TagHelperHtmlAttributeIntermediateNode node)
        : this(node.AttributeName, node.AttributeStructure)
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

        extension.WriteTagHelperHtmlAttribute(context, this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(AttributeName);

        formatter.WriteProperty(nameof(AttributeName), AttributeName);
        formatter.WriteProperty(nameof(AttributeStructure), AttributeStructure.ToString());
    }
}
