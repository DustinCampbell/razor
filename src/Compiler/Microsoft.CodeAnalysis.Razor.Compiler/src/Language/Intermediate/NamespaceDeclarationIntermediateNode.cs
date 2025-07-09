// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class NamespaceDeclarationIntermediateNode(
    string? name = null,
    bool isPrimaryNamespace = false) : IntermediateNode
{
    private string? _name = name;
    private IntermediateNodeCollection? _children;

    public bool IsPrimaryNamespace { get; } = isPrimaryNamespace;

    public string? Name => _name;

    public bool IsGenericTyped { get; set; }

    public override IntermediateNodeCollection Children
        => _children ??= [];

    public void UpdateName(string? value)
    {
        _name = value;
    }

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitNamespaceDeclaration(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(Name);

        formatter.WriteProperty(nameof(Name), Name);
    }
}
