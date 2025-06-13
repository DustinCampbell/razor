// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class MethodDeclarationIntermediateNode : MemberDeclarationIntermediateNode
{
    private ImmutableArray<string> _modifiers;
    private ImmutableArray<MethodParameter> _parameters;

    public ImmutableArray<string> Modifiers => _modifiers;
    public string? MethodName { get; set; }
    public ImmutableArray<MethodParameter> Parameters => _parameters;
    public string? ReturnType { get; set; }
    public bool IsPrimaryMethod { get; }

    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public MethodDeclarationIntermediateNode(
        ImmutableArray<string> modifiers,
        string methodName,
        ImmutableArray<MethodParameter> parameters,
        string returnType)
    {
        _modifiers = modifiers.NullToEmpty();
        _parameters = parameters.NullToEmpty();
        MethodName = methodName;
        ReturnType = returnType;
    }

    private MethodDeclarationIntermediateNode(bool isPrimaryMethod)
    {
        _modifiers = [];
        _parameters = [];
        IsPrimaryMethod = isPrimaryMethod;
    }

    public static MethodDeclarationIntermediateNode CreatePrimary()
        => new(isPrimaryMethod: true);

    public void ClearModifiers() => SetModifiers([]);

    public void SetModifiers(params ImmutableArray<string> modifiers)
    {
        _modifiers = modifiers.NullToEmpty();
    }

    public void ClearParameters() => SetParameters([]);

    public void SetParameters(params ImmutableArray<MethodParameter> parameters)
    {
        _parameters = parameters.NullToEmpty();
    }

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitMethodDeclaration(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(MethodName);

        formatter.WriteProperty(nameof(MethodName), MethodName);
        formatter.WriteProperty(nameof(Modifiers), string.Join(", ", Modifiers));
        formatter.WriteProperty(nameof(Parameters), string.Join(", ", Parameters, static p => FormatMethodParameter(p)));
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
