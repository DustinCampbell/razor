// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class BoundAttributeParameterDescriptor : TagHelperObject<BoundAttributeParameterDescriptor>
{
    private readonly BoundAttributeParameterFlags _flags;
    private readonly DocumentationObject _documentationObject;

    private BoundAttributeDescriptor? _parent;

    internal BoundAttributeParameterFlags Flags => _flags;

    public string Kind => Parent.Kind;
    public string Name { get; }
    public string PropertyName { get; }
    public string TypeName { get; }
    public string DisplayName { get; }

    public bool CaseSensitive => Parent.CaseSensitive;
    public bool IsEnum => _flags.IsFlagSet(BoundAttributeParameterFlags.IsEnum);
    public bool IsStringProperty => _flags.IsFlagSet(BoundAttributeParameterFlags.IsStringProperty);
    public bool IsBooleanProperty => _flags.IsFlagSet(BoundAttributeParameterFlags.IsBooleanProperty);
    public bool IsBindAttributeGetSet => _flags.IsFlagSet(BoundAttributeParameterFlags.IsBindAttributeGetSet);

    internal BoundAttributeParameterDescriptor(
        BoundAttributeParameterFlags flags,
        string name,
        string propertyName,
        string typeName,
        DocumentationObject documentationObject,
        string displayName,
        ImmutableArray<RazorDiagnostic> diagnostics)
        : base(diagnostics)
    {
        Name = name;
        PropertyName = propertyName;
        TypeName = typeName;
        _documentationObject = documentationObject;
        DisplayName = displayName;

        if (typeName == typeof(string).FullName || typeName == "string")
        {
            flags |= BoundAttributeParameterFlags.IsStringProperty;
        }

        if (typeName == typeof(bool).FullName || typeName == "bool")
        {
            flags |= BoundAttributeParameterFlags.IsBooleanProperty;
        }

        _flags = flags;
    }

    private protected override void BuildChecksum(in Checksum.Builder builder)
    {
        builder.AppendData((byte)Flags);
        builder.AppendData(Name);
        builder.AppendData(PropertyName);
        builder.AppendData(TypeName);
        builder.AppendData(DisplayName);

        DocumentationObject.AppendToChecksum(in builder);
    }

    internal void SetParent(BoundAttributeDescriptor parent)
    {
        if (_parent is not null)
        {
            ThrowHelper.ThrowInvalidOperationException("Parent can only be set once.");
        }

        _parent = parent;
    }

    public BoundAttributeDescriptor Parent
        => _parent ?? Assumed.Unreachable<BoundAttributeDescriptor>($"{nameof(Parent)} not set.");

    public string? Documentation => _documentationObject.GetText();

    internal DocumentationObject DocumentationObject => _documentationObject;

    public override string ToString()
        => DisplayName ?? base.ToString()!;
}
