// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public sealed class DefaultTagHelperHtmlAttributeIntermediateNode : ExtensionIntermediateNode
{
    public DefaultTagHelperHtmlAttributeIntermediateNode()
    {
    }

    public DefaultTagHelperHtmlAttributeIntermediateNode(TagHelperHtmlAttributeIntermediateNode htmlAttributeNode)
    {
        if (htmlAttributeNode == null)
        {
            throw new ArgumentNullException(nameof(htmlAttributeNode));
        }

        AttributeName = htmlAttributeNode.AttributeName;
        AttributeStructure = htmlAttributeNode.AttributeStructure;
        Source = htmlAttributeNode.Source;

        for (var i = 0; i < htmlAttributeNode.Children.Count; i++)
        {
            Children.Add(htmlAttributeNode.Children[i]);
        }

        AddDiagnosticsFromNode(htmlAttributeNode);
    }

    public string AttributeName { get; set; }

    public AttributeStructure AttributeStructure { get; set; }

    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

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
