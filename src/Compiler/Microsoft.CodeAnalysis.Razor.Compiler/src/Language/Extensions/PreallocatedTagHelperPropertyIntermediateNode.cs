// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal sealed class PreallocatedTagHelperPropertyIntermediateNode(
    string attributeName,
    string fieldName,
    string propertyName,
    string variableName,
    BoundAttributeDescriptor boundAttribute,
    TagHelperDescriptor tagHelper,
    bool isIndexerNameMatch = false)
    : ExtensionIntermediateNode
{
    public string AttributeName { get; } = attributeName;
    public string FieldName { get; } = fieldName;
    public string PropertyName { get; } = propertyName;
    public string VariableName { get; } = variableName;
    public bool IsIndexerNameMatch { get; } = isIndexerNameMatch;
    public BoundAttributeDescriptor BoundAttribute { get; } = boundAttribute;
    public TagHelperDescriptor TagHelper { get; } = tagHelper;

    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public PreallocatedTagHelperPropertyIntermediateNode(DefaultTagHelperPropertyIntermediateNode node, string variableName)
        : this(node.AttributeName, node.FieldName, node.PropertyName, variableName,
               node.BoundAttribute, node.TagHelper, node.IsIndexerNameMatch)
    {
        Source = node.Source;
    }

    public override void Accept(IntermediateNodeVisitor visitor)
        => AcceptExtensionNode(this, visitor);

    public override void WriteNode(CodeTarget target, CodeRenderingContext context)
    {
        if (!TryGetExtensionAndReportIfMissing<IPreallocatedAttributeTargetExtension>(target, context, out var extension))
        {
            return;
        }

        extension.WriteTagHelperProperty(context, this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(AttributeName);

        formatter.WriteProperty(nameof(AttributeName), AttributeName);
        formatter.WriteProperty(nameof(BoundAttribute), BoundAttribute.DisplayName);
        formatter.WriteProperty(nameof(FieldName), FieldName);
        formatter.WriteProperty(nameof(IsIndexerNameMatch), IsIndexerNameMatch.ToString());
        formatter.WriteProperty(nameof(PropertyName), PropertyName);
        formatter.WriteProperty(nameof(TagHelper), TagHelper.DisplayName);
        formatter.WriteProperty(nameof(VariableName), VariableName);
    }
}
