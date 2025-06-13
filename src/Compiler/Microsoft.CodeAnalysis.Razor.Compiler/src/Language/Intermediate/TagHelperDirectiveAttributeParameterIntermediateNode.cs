// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class TagHelperDirectiveAttributeParameterIntermediateNode : IntermediateNode
{
    public string AttributeName { get; }
    public string AttributeNameWithoutParameter { get; }
    public string OriginalAttributeName { get; }
    public SourceSpan? OriginalAttributeSpan { get; }
    public AttributeStructure AttributeStructure { get; }
    public BoundAttributeParameterDescriptor BoundAttributeParameter { get; }
    public BoundAttributeDescriptor BoundAttribute { get; }
    public TagHelperDescriptor TagHelper { get; }
    public bool IsIndexerNameMatch { get; }

    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public TagHelperDirectiveAttributeParameterIntermediateNode(
        string attributeName,
        string attributeNameWithoutParameter,
        string originalAttributeName,
        AttributeStructure attributeStructure,
        BoundAttributeParameterDescriptor boundAttributeParameter,
        BoundAttributeDescriptor boundAttribute,
        TagHelperDescriptor tagHelper,
        bool isIndexerNameMatch,
        SourceSpan? originalAttributeSpan = null)
    {
        AttributeName = attributeName;
        AttributeNameWithoutParameter = attributeNameWithoutParameter;
        OriginalAttributeName = originalAttributeName;
        AttributeStructure = attributeStructure;
        BoundAttributeParameter = boundAttributeParameter;
        BoundAttribute = boundAttribute;
        TagHelper = tagHelper;
        IsIndexerNameMatch = isIndexerNameMatch;
        OriginalAttributeSpan = originalAttributeSpan;
    }

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitTagHelperDirectiveAttributeParameter(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(AttributeName);

        formatter.WriteProperty(nameof(AttributeName), AttributeName);
        formatter.WriteProperty(nameof(OriginalAttributeName), OriginalAttributeName);
        formatter.WriteProperty(nameof(AttributeStructure), AttributeStructure.ToString());
        formatter.WriteProperty(nameof(BoundAttribute), BoundAttribute?.DisplayName);
        formatter.WriteProperty(nameof(BoundAttributeParameter), BoundAttributeParameter?.DisplayName);
        formatter.WriteProperty(nameof(TagHelper), TagHelper?.DisplayName);
    }
}
