// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using static Microsoft.AspNetCore.Razor.Language.Extensions.Constants;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal sealed class PreallocatedTagHelperPropertyValueIntermediateNode(
    string variableName, string attributeName, string value, AttributeStructure attributeStructure)
    : ExtensionIntermediateNode
{
    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public string VariableName { get; } = variableName;
    public string AttributeName { get; } = attributeName;
    public string Value { get; } = value;
    public AttributeStructure AttributeStructure { get; } = attributeStructure;

    public override void Accept(IntermediateNodeVisitor visitor)
        => AcceptExtensionNode(this, visitor);

    public override void WriteNode(CodeTarget target, CodeRenderingContext context)
    {
        context.CodeWriter
            .Write($"private static readonly {TagHelperAttributeTypeName} {VariableName} = ")
            .WriteStartNewObject(TagHelperAttributeTypeName)
            .WriteStringLiteral(AttributeName)
            .WriteParameterSeparator()
            .WriteStringLiteral(Value)
            .WriteParameterSeparator()
            .Write($"{HtmlAttributeValueStyleTypeName}.{AttributeStructure}")
            .WriteEndMethodInvocation();
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(AttributeName);

        formatter.WriteProperty(nameof(AttributeName), AttributeName);
        formatter.WriteProperty(nameof(AttributeStructure), AttributeStructure.ToString());
        formatter.WriteProperty(nameof(Value), Value);
        formatter.WriteProperty(nameof(VariableName), VariableName);
    }
}
