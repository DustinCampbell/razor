// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class PropertyDeclarationIntermediateNode : MemberDeclarationIntermediateNode
{
    public required Content Name { get; init; }
    public required IntermediateToken ReturnTypeName { get; init; }

    public Content ExpressionBody { get; set; }

    public ImmutableArray<Content> Modifiers { get; set => field = value.NullToEmpty(); } = [];

    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitPropertyDeclaration(this);
}
