// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class TagHelperDirectiveAttributeIntermediateNode(
    string attributeName,
    string originalAttributeName,
    AttributeStructure attributeStructure,
    BoundAttributeDescriptor boundAttribute,
    TagHelperDescriptor tagHelper,
    bool isIndexerNameMatch,
    SourceSpan? originalAttributeSpan = null) : IntermediateNode
{
    public string AttributeName { get; } = attributeName;
    public string OriginalAttributeName { get; } = originalAttributeName;
    public SourceSpan? OriginalAttributeSpan { get; } = originalAttributeSpan;
    public AttributeStructure AttributeStructure { get; } = attributeStructure;
    public BoundAttributeDescriptor BoundAttribute { get; } = boundAttribute;
    public TagHelperDescriptor TagHelper { get; } = tagHelper;
    public bool IsIndexerNameMatch { get; } = isIndexerNameMatch;

    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitTagHelperDirectiveAttribute(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(AttributeName);

        formatter.WriteProperty(nameof(AttributeName), AttributeName);
        formatter.WriteProperty(nameof(OriginalAttributeName), OriginalAttributeName);
        formatter.WriteProperty(nameof(AttributeStructure), AttributeStructure.ToString());
        formatter.WriteProperty(nameof(BoundAttribute), BoundAttribute?.DisplayName);
        formatter.WriteProperty(nameof(IsIndexerNameMatch), IsIndexerNameMatch.ToString());
        formatter.WriteProperty(nameof(TagHelper), TagHelper?.DisplayName);
    }
}
