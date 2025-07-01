// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal sealed class PreallocatedTagHelperPropertyIntermediateNode : ExtensionIntermediateNode
{
    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public string AttributeName { get; }
    public AttributeStructure AttributeStructure { get; }
    public BoundAttributeDescriptor BoundAttribute { get; }
    public string FieldName { get; }
    public bool IsIndexerNameMatch { get; }
    public string PropertyName { get; }
    public TagHelperDescriptor TagHelper { get; }
    public string VariableName { get; }

    public PreallocatedTagHelperPropertyIntermediateNode(
        string attributeName,
        AttributeStructure attributeStructure,
        BoundAttributeDescriptor boundAttribute,
        string fieldName,
        bool isIndexerNameMatch,
        string propertyName,
        TagHelperDescriptor tagHelper,
        string variableName,
        SourceSpan? source = null)
    {
        AttributeName = attributeName;
        AttributeStructure = attributeStructure;
        BoundAttribute = boundAttribute;
        FieldName = fieldName;
        IsIndexerNameMatch = isIndexerNameMatch;
        PropertyName = propertyName;
        TagHelper = tagHelper;
        VariableName = variableName;
        Source = source;
    }

    public PreallocatedTagHelperPropertyIntermediateNode(DefaultTagHelperPropertyIntermediateNode propertyNode, string variableName)
        : this(propertyNode.AttributeName, propertyNode.AttributeStructure, propertyNode.BoundAttribute,
              propertyNode.FieldName, propertyNode.IsIndexerNameMatch, propertyNode.PropertyName,
              propertyNode.TagHelper, variableName, propertyNode.Source)
    {
    }

    public override void Accept(IntermediateNodeVisitor visitor)
        => AcceptExtensionNode(this, visitor);

    public override void WriteNode(CodeTarget target, CodeRenderingContext context)
    {
        var extension = target.GetExtension<IPreallocatedAttributeTargetExtension>();
        if (extension == null)
        {
            ReportMissingCodeTargetExtension<IPreallocatedAttributeTargetExtension>(context);
            return;
        }

        extension.WriteTagHelperProperty(context, this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(AttributeName);

        formatter.WriteProperty(nameof(AttributeName), AttributeName);
        formatter.WriteProperty(nameof(AttributeStructure), AttributeStructure.ToString());
        formatter.WriteProperty(nameof(BoundAttribute), BoundAttribute?.DisplayName);
        formatter.WriteProperty(nameof(FieldName), FieldName);
        formatter.WriteProperty(nameof(IsIndexerNameMatch), IsIndexerNameMatch.ToString());
        formatter.WriteProperty(nameof(PropertyName), PropertyName);
        formatter.WriteProperty(nameof(TagHelper), TagHelper?.DisplayName);
        formatter.WriteProperty(nameof(VariableName), VariableName);
    }
}
