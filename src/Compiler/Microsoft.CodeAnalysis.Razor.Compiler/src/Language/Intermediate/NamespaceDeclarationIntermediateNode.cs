// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class NamespaceDeclarationIntermediateNode(
    Content name = default,
    bool isPrimaryNamespace = false) : IntermediateNode
{
    private Content _name = name;
    private IntermediateNodeCollection? _children;

    public bool IsPrimaryNamespace { get; } = isPrimaryNamespace;

    public Content Name => _name;

    public bool IsGenericTyped { get; set; }

    public override IntermediateNodeCollection Children
        => _children ??= [];

    public void UpdateName(Content value)
        => _name = value;

    public void UpdateName(ref Content.ContentInterpolatedStringHandler handler)
        => _name = new(ref handler);

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitNamespaceDeclaration(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(Name);

        formatter.WriteProperty(nameof(Name), Name);
    }
}
