// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public sealed class DefaultTagHelperPropertyIntermediateNode(
    string attributeName,
    string fieldName,
    string propertyName,
    AttributeStructure attributeStructure,
    BoundAttributeDescriptor boundAttribute,
    TagHelperDescriptor tagHelper,
    bool isIndexerNameMatch)
    : ExtensionIntermediateNode
{
    public string AttributeName { get; } = attributeName;
    public string FieldName { get; } = fieldName;
    public string PropertyName { get; } = propertyName;
    public AttributeStructure AttributeStructure { get; } = attributeStructure;
    public bool IsIndexerNameMatch { get; } = isIndexerNameMatch;
    public BoundAttributeDescriptor BoundAttribute { get; } = boundAttribute;
    public TagHelperDescriptor TagHelper { get; } = tagHelper;

    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public DefaultTagHelperPropertyIntermediateNode(TagHelperPropertyIntermediateNode node, string fieldName)
        : this(node.AttributeName, fieldName, propertyName: node.BoundAttribute.GetPropertyName(),
               node.AttributeStructure, node.BoundAttribute, node.TagHelper, node.IsIndexerNameMatch)
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

        extension.WriteTagHelperProperty(context, this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(AttributeName);

        formatter.WriteProperty(nameof(AttributeName), AttributeName);
        formatter.WriteProperty(nameof(AttributeStructure), AttributeStructure.ToString());
        formatter.WriteProperty(nameof(BoundAttribute), BoundAttribute.DisplayName);
        formatter.WriteProperty(nameof(FieldName), FieldName);
        formatter.WriteProperty(nameof(IsIndexerNameMatch), IsIndexerNameMatch.ToString());
        formatter.WriteProperty(nameof(PropertyName), PropertyName);
        formatter.WriteProperty(nameof(TagHelper), TagHelper.DisplayName);
    }
}
