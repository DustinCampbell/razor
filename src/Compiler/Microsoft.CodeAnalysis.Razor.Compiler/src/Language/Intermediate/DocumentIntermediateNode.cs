// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class DocumentIntermediateNode(RazorCodeGenerationOptions options) : IntermediateNode
{
    private string? _documentKind;
    private CodeTarget? _target;

    public override IntermediateNodeCollection Children { get; } = new();

    public RazorCodeGenerationOptions Options { get; } = options;

    public string? DocumentKind
    {
        get => _documentKind;
        init => _documentKind = value;
    }

    public CodeTarget? Target
    {
        get => _target;
        init => _target = value;
    }

    public void SetDocumentKind(string documentKind)
    {
        Debug.Assert(!string.IsNullOrEmpty(documentKind), "DocumentKind must not be null or empty.");
        Debug.Assert(_documentKind is null, "DocumentKind should only be set once.");

        _documentKind = documentKind;
    }

    public void SetTarget(CodeTarget target)
    {
        Debug.Assert(target is not null, "Target must not be null.");
        Debug.Assert(_target is null, "Target should only be set once.");

        _target = target;
    }

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitDocument(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(DocumentKind);

        formatter.WriteProperty(nameof(DocumentKind), DocumentKind);
    }
}
