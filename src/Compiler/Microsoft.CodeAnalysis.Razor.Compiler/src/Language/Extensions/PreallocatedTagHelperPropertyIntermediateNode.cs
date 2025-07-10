// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal sealed class PreallocatedTagHelperPropertyIntermediateNode : ExtensionIntermediateNode
{
    public required string AttributeName { get; init; }
    public required AttributeStructure AttributeStructure { get; init; }
    public required BoundAttributeDescriptor BoundAttribute { get; init; }
    public required Content FieldName { get; init; }
    public required bool IsIndexerNameMatch { get; init; }
    public required Content PropertyName { get; init; }
    public required TagHelperDescriptor TagHelper { get; init; }
    public required Content VariableName { get; init; }

    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public PreallocatedTagHelperPropertyIntermediateNode()
    {
    }

    [SetsRequiredMembers]
    public PreallocatedTagHelperPropertyIntermediateNode(
        DefaultTagHelperPropertyIntermediateNode node,
        Content variableName)
    {
        AttributeName = node.AttributeName;
        AttributeStructure = node.AttributeStructure;
        BoundAttribute = node.BoundAttribute;
        FieldName = node.FieldName;
        IsIndexerNameMatch = node.IsIndexerNameMatch;
        PropertyName = node.PropertyName;
        Source = node.Source;
        TagHelper = node.TagHelper;
        VariableName = variableName;
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
