// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class MethodDeclarationIntermediateNode : MemberDeclarationIntermediateNode
{
    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public IList<string> Modifiers { get; } = new List<string>();

    public string MethodName { get; set; }

    public IList<MethodParameter> Parameters { get; } = new List<MethodParameter>();

    public string ReturnType { get; set; }

    public bool IsPrimaryMethod { get; set; }

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitMethodDeclaration(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(MethodName);

        formatter.WriteProperty(nameof(MethodName), MethodName);
        formatter.WriteProperty(nameof(Modifiers), string.Join(", ", Modifiers));
        formatter.WriteProperty(nameof(Parameters), string.Join(", ", Parameters.Select(FormatMethodParameter)));
        formatter.WriteProperty(nameof(ReturnType), ReturnType);
    }

    private static string FormatMethodParameter(MethodParameter parameter)
    {
        using var _ = StringBuilderPool.GetPooledObject(out var builder);

        foreach (var modifier in parameter.Modifiers)
        {
            builder.Append(modifier);
            builder.Append(' ');
        }

        builder.Append(parameter.TypeName);
        builder.Append(' ');

        builder.Append(parameter.ParameterName);

        return builder.ToString();
    }
}
