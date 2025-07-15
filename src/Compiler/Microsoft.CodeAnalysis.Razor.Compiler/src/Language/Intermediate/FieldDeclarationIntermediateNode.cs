// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class FieldDeclarationIntermediateNode : MemberDeclarationIntermediateNode
{
    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public Content Name { get; set; }
    public Content TypeName { get; set; }

    public ImmutableArray<Content> Modifiers { get; set => field = value.NullToEmpty(); } = [];
    public ImmutableArray<Content> SuppressWarnings { get; set => field = value.NullToEmpty(); } = [];

    public bool IsTagHelperField { get; set; }

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitFieldDeclaration(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(Name);

        formatter.WriteProperty(nameof(Name), Name);
        formatter.WriteProperty(nameof(TypeName), TypeName);
        formatter.WriteProperty(nameof(Modifiers), Content.Join(" ", Modifiers));
    }
}
