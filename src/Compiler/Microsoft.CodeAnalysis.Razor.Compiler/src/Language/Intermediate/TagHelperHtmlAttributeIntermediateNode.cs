// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class TagHelperHtmlAttributeIntermediateNode(
    string attributeName,
    AttributeStructure attributeStructure)
    : IntermediateNode
{
    public string AttributeName { get; } = attributeName;
    public AttributeStructure AttributeStructure { get; } = attributeStructure;

    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitTagHelperHtmlAttribute(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(AttributeName);

        formatter.WriteProperty(nameof(AttributeName), AttributeName);
        formatter.WriteProperty(nameof(AttributeStructure), AttributeStructure.ToString());
    }
}
