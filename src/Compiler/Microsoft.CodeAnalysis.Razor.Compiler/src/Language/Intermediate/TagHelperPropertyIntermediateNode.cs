// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class TagHelperPropertyIntermediateNode : IntermediateNode
{
    public string AttributeName { get; }
    public AttributeStructure AttributeStructure { get; }
    public BoundAttributeDescriptor BoundAttribute { get; }
    public TagHelperDescriptor TagHelper { get; }
    public bool IsIndexerNameMatch { get; }
    public SourceSpan? OriginalAttributeSpan { get; }

    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public TagHelperPropertyIntermediateNode(
        string attributeName,
        AttributeStructure attributeStructure,
        BoundAttributeDescriptor boundAttribute,
        TagHelperDescriptor tagHelper,
        bool isIndexerNameMatch = false,
        SourceSpan? originalAttributeSpan = null)
    {
        AttributeName = attributeName;
        AttributeStructure = attributeStructure;
        BoundAttribute = boundAttribute;
        TagHelper = tagHelper;
        IsIndexerNameMatch = isIndexerNameMatch;
        OriginalAttributeSpan = originalAttributeSpan;
    }

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitTagHelperProperty(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(AttributeName);

        formatter.WriteProperty(nameof(AttributeName), AttributeName);
        formatter.WriteProperty(nameof(AttributeStructure), AttributeStructure.ToString());
        formatter.WriteProperty(nameof(BoundAttribute), BoundAttribute?.DisplayName);
        formatter.WriteProperty(nameof(IsIndexerNameMatch), IsIndexerNameMatch.ToString());
        formatter.WriteProperty(nameof(TagHelper), TagHelper?.DisplayName);
    }
}
