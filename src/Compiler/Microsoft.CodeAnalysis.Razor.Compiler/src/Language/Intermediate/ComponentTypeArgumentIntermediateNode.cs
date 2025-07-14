// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class ComponentTypeArgumentIntermediateNode : IntermediateNode
{
    public static ComponentTypeArgumentIntermediateNode CreateFrom(TagHelperPropertyIntermediateNode node)
    {
        return CreateFrom<TagHelperPropertyIntermediateNode, ComponentTypeArgumentIntermediateNode>(
            node, addChildren: false, addDiagnostics: true,
            copyProperties: static (node, newNode) =>
            {
                newNode.BoundAttribute = node.BoundAttribute;
                newNode.Source = node.Source;
                newNode.TagHelper = node.TagHelper;

                Debug.Assert(node.Children.Count == 1);

                newNode.Value = node.Children[0] switch
                {
                    CSharpIntermediateToken t => t,
                    CSharpExpressionIntermediateNode c => (CSharpIntermediateToken)c.Children[0], // TODO: can we break this in error cases?
                    _ => Assumed.Unreachable<CSharpIntermediateToken>()
                };
            });
    }

    public override IntermediateNodeCollection Children { get; } = [];

    public BoundAttributeDescriptor BoundAttribute { get; set; }

    public string TypeParameterName => BoundAttribute.Name;

    public TagHelperDescriptor TagHelper { get; set; }

    public CSharpIntermediateToken Value
    {
        get => field;
        set
        {
            field = value;

            // Ensure that Children is always in sync with Value.
            Children.Clear();

            if (value is not null)
            {
                Children.Add(value);
            }
        }
    }

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitComponentTypeArgument(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(TypeParameterName);

        formatter.WriteProperty(nameof(BoundAttribute), BoundAttribute?.DisplayName);
        formatter.WriteProperty(nameof(TagHelper), TagHelper?.DisplayName);
    }
}
