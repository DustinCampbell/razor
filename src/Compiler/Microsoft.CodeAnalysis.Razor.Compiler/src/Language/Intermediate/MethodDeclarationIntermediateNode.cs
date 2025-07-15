// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class MethodDeclarationIntermediateNode : MemberDeclarationIntermediateNode
{
    private IntermediateNodeCollection? _children;

    public Content Name { get; set; }
    public Content ReturnTypeName { get; set; }

    public ImmutableArray<Content> Modifiers { get; set => field = value.NullToEmpty(); } = [];
    public ImmutableArray<MethodParameter> Parameters { get; set => field = value.NullToEmpty(); } = [];

    public bool IsPrimaryMethod { get; set; }

    public override IntermediateNodeCollection Children => _children ??= [];

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitMethodDeclaration(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(Name);

        formatter.WriteProperty(nameof(Name), Name);
        formatter.WriteProperty(nameof(Modifiers), Content.Join(", ", Modifiers));
        formatter.WriteProperty(nameof(Parameters), Content.Join(", ", Parameters.Select(FormatMethodParameter)));
        formatter.WriteProperty(nameof(ReturnTypeName), ReturnTypeName);

        static Content FormatMethodParameter(MethodParameter parameter)
        {
            using var builder = new PooledArrayBuilder<Content>();

            foreach (var modifier in parameter.Modifiers)
            {
                builder.Add(new($"{modifier} "));
            }

            builder.Add(new($"{parameter.TypeName} "));
            builder.Add(parameter.Name);

            return builder.ToContent();
        }
    }
}
