// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class NamespaceDeclarationIntermediateNode(string? content = null, bool isPrimaryNamespace = false, bool isGenericTyped = false) : IntermediateNode
{
    public string? Content { get; set; } = content;

    public bool IsPrimaryNamespace { get; } = isPrimaryNamespace;
    public bool IsGenericTyped { get; } = isGenericTyped;

    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public static NamespaceDeclarationIntermediateNode CreatePrimary(string? content = null)
        => new(content, isPrimaryNamespace: true, isGenericTyped: false);

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitNamespaceDeclaration(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(Content);

        formatter.WriteProperty(nameof(Content), Content);
    }
}
