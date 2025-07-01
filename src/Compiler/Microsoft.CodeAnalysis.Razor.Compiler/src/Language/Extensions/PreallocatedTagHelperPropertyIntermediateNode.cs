// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using static Microsoft.AspNetCore.Razor.Language.Extensions.Constants;

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
        Debug.Assert(
            context.Parent is TagHelperIntermediateNode,
            message: Resources.FormatIntermediateNodes_InvalidParentNode(GetType(), typeof(TagHelperIntermediateNode)));

        var tagHelperNode = (TagHelperIntermediateNode)context.Parent;

        // Ensure that the property we're trying to set has initialized its dictionary bound properties.
        if (IsIndexerNameMatch && ReferenceEquals(FindFirstUseOfIndexer(tagHelperNode), this))
        {
            // Throw a reasonable Exception at runtime if the dictionary property is null.
            context.CodeWriter.WriteLine($"if ({FieldName}.{PropertyName} == null)");

            using (context.CodeWriter.BuildScope())
            {
                // System is in Host.NamespaceImports for all MVC scenarios. No need to generate FullName
                // of InvalidOperationException type.
                context.CodeWriter
                    .Write("throw ")
                    .WriteStartNewObject(nameof(InvalidOperationException))
                    .WriteStartMethodInvocation(FormatInvalidIndexerAssignmentMethodName)
                    .WriteStringLiteral(AttributeName)
                    .WriteParameterSeparator()
                    .WriteStringLiteral(TagHelper.GetTypeName())
                    .WriteParameterSeparator()
                    .WriteStringLiteral(PropertyName)
                    .WriteEndMethodInvocation(endLine: false)   // End of method call
                    .WriteEndMethodInvocation();   // End of new expression / throw statement
            }
        }

        context.CodeWriter.Write($"{FieldName}.{PropertyName}");

        if (IsIndexerNameMatch && BoundAttribute.IndexerNamePrefix is string indexerNamePrefix)
        {
            var dictionaryKey = AttributeName.AsMemory()[indexerNamePrefix.Length..];
            context.CodeWriter.Write($"[\"{dictionaryKey}\"]");
        }

        context.CodeWriter
            .WriteLine($" = (string){VariableName}.Value;")
            .WriteStartInstanceMethodInvocation(ExecutionContextVariableName, ExecutionContextAddTagHelperAttributeMethodName)
            .Write(VariableName)
            .WriteEndMethodInvocation();
    }

    private PreallocatedTagHelperPropertyIntermediateNode FindFirstUseOfIndexer(TagHelperIntermediateNode tagHelperNode)
    {
        Debug.Assert(tagHelperNode.Children.Contains(this));
        Debug.Assert(IsIndexerNameMatch);

        foreach (var child in tagHelperNode.Children)
        {
            if (child is PreallocatedTagHelperPropertyIntermediateNode otherPropertyNode &&
                otherPropertyNode.TagHelper == TagHelper &&
                otherPropertyNode.BoundAttribute == BoundAttribute &&
                otherPropertyNode.IsIndexerNameMatch)
            {
                return otherPropertyNode;
            }
        }

        // This is unreachable, we should find 'propertyNode' in the list of children.
        return Assumed.Unreachable<PreallocatedTagHelperPropertyIntermediateNode>();
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
