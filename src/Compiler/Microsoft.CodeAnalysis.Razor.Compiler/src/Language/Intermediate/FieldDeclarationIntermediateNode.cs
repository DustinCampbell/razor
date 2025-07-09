// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class FieldDeclarationIntermediateNode(
    Content name,
    Content typeName,
    ImmutableArray<string> modifiers,
    ImmutableArray<string> suppressWarnings,
    bool isTagHelperField = false) : MemberDeclarationIntermediateNode
{
    public Content Name { get; } = name;
    public Content TypeName { get; } = typeName;

    public bool IsTagHelperField { get; } = isTagHelperField;

    public ImmutableArray<string> Modifiers { get; } = modifiers.NullToEmpty();
    public ImmutableArray<string> SuppressWarnings { get; } = suppressWarnings.NullToEmpty();

    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public FieldDeclarationIntermediateNode(
        Content name,
        Content typeName,
        ImmutableArray<string> modifiers,
        bool isTagHelperField = false)
        : this(name, typeName, modifiers, suppressWarnings: [], isTagHelperField)
    {
    }

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitFieldDeclaration(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(Name);

        formatter.WriteProperty(nameof(Name), Name);
        formatter.WriteProperty(nameof(TypeName), TypeName);
        formatter.WriteProperty(nameof(Modifiers), string.Join(" ", Modifiers));
    }
}
