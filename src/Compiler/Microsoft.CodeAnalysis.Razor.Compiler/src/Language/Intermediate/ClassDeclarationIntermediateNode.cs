// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class ClassDeclarationIntermediateNode(
    Content name = default,
    bool isPrimaryClass = false) : MemberDeclarationIntermediateNode
{
    private Content _name = name;
    private IntermediateNodeCollection? _children;
    private ImmutableArray<string> _modifiers = [];
    private ImmutableArray<IntermediateToken> _interfaces = [];
    private ImmutableArray<TypeParameter> _typeParameters = [];

    public bool IsPrimaryClass { get; } = isPrimaryClass;

    public ImmutableArray<string> Modifiers
    {
        get => _modifiers;
        init => _modifiers = value.NullToEmpty();
    }

    public Content Name => _name;

    public BaseTypeWithModel? BaseType { get; set; }

    public ImmutableArray<IntermediateToken> Interfaces
    {
        get => _interfaces;
        init => _interfaces = value.NullToEmpty();
    }

    public ImmutableArray<TypeParameter> TypeParameters
    {
        get => _typeParameters;
        init => _typeParameters = value.NullToEmpty();
    }

    public bool NullableContext { get; set; }

    public override IntermediateNodeCollection Children
        => _children ??= [];

    public void UpdateName(Content value)
        => _name = value;

    public void UpdateName(ref Content.ContentInterpolatedStringHandler handler)
        => _name = new(ref handler);

    public void UpdateModifiers(params ImmutableArray<string> modifiers)
        => _modifiers = modifiers.NullToEmpty();

    public void UpdateInterfaces(params ImmutableArray<IntermediateToken> interfaces)
        => _interfaces = interfaces.NullToEmpty();

    public void UpdateTypeParameters(params ImmutableArray<TypeParameter> typeParameters)
        => _typeParameters = typeParameters.NullToEmpty();

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitClassDeclaration(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(Name);

        formatter.WriteProperty(nameof(Name), Name);
        formatter.WriteProperty(nameof(Interfaces), string.Join(", ", Interfaces.Select(i => i.Content)));
        formatter.WriteProperty(nameof(Modifiers), string.Join(", ", Modifiers));
        formatter.WriteProperty(nameof(TypeParameters), string.Join(", ", TypeParameters.Select(t => t.Name)));
    }
}
