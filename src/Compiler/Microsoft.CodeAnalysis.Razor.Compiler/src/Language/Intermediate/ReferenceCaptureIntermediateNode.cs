// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class ReferenceCaptureIntermediateNode : IntermediateNode
{
    private const string ElementReferenceTypeName = $"global::{ComponentsApi.ElementReference.FullTypeName}";
    private const string ActionOfElementReferenceTypeName = $"global::System.Action<{ElementReferenceTypeName}>";

    public IntermediateToken IdentifierToken { get; }

    private string? _componentCaptureTypeName;
    public string? ComponentCaptureTypeName => _componentCaptureTypeName;

    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    [MemberNotNullWhen(true, nameof(ComponentCaptureTypeName))]
    public bool IsComponentCapture => _componentCaptureTypeName != null;

    public string TypeName => IsComponentCapture
        ? $"global::System.Action<{ComponentCaptureTypeName}>"
        : ActionOfElementReferenceTypeName;

    public ReferenceCaptureIntermediateNode(IntermediateToken identifierToken)
    {
        ArgHelper.ThrowIfNull(identifierToken);

        IdentifierToken = identifierToken;
        Source = IdentifierToken.Source;
    }

    public ReferenceCaptureIntermediateNode(IntermediateToken identifierToken, string componentCaptureTypeName)
        : this(identifierToken)
    {
        ArgHelper.ThrowIfNullOrEmpty(componentCaptureTypeName);

        _componentCaptureTypeName = componentCaptureTypeName;
    }

    public void SetComponentCaptureTypeName(string newName)
    {
        ArgHelper.ThrowIfNullOrEmpty(newName);

        _componentCaptureTypeName = newName;
    }

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitReferenceCapture(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(IdentifierToken?.Content);

        formatter.WriteProperty(nameof(IdentifierToken), IdentifierToken?.Content);
        formatter.WriteProperty(nameof(ComponentCaptureTypeName), ComponentCaptureTypeName);
    }
}
