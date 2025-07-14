// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class ClassDeclarationIntermediateNode : MemberDeclarationIntermediateNode
{
    private IntermediateNodeCollection? _children;

    public Content Name { get; set; }
    public BaseTypeWithModel? BaseType { get; set; }

    public ImmutableArray<Content> Modifiers { get; set => field = value.NullToEmpty(); } = [];
    public ImmutableArray<IntermediateToken> Interfaces { get; set => field = value.NullToEmpty(); } = [];
    public ImmutableArray<TypeParameter> TypeParameters { get; set => field = value.NullToEmpty(); } = [];

    public bool IsPrimaryClass { get; set; }
    public bool NullableContext { get; set; }

    public override IntermediateNodeCollection Children => _children ??= [];

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitClassDeclaration(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(Name);

        formatter.WriteProperty(nameof(Name), Name);
        formatter.WriteProperty(nameof(Interfaces), Content.Join(", ", Interfaces.Select(i => i.Content)));
        formatter.WriteProperty(nameof(Modifiers), Content.Join(", ", Modifiers));
        formatter.WriteProperty(nameof(TypeParameters), Content.Join(", ", TypeParameters.Select(t => t.Name)));
    }
}
