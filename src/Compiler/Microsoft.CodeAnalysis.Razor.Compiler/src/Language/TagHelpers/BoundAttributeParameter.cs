// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers;

internal sealed class BoundAttributeParameter
{
    private readonly BoundAttributeParameterFlags _flags;
    private readonly TypeNameObject _typeNameObject;
    private readonly DocumentationObject _documentationObject;

    public BoundAttribute Parent { get; }

    public string Name { get; }
    public string PropertyName { get; }

    public ImmutableArray<RazorDiagnostic> Diagnostics { get; }

    public string TypeName => _typeNameObject.FullName.AssumeNotNull();
    public string? Documentation => _documentationObject.GetText();

    public string DisplayName => field ??= $":{Name}";

    public bool CaseSensitive => _flags.IsFlagSet(BoundAttributeParameterFlags.CaseSensitive);
    public bool IsEnum => _flags.IsFlagSet(BoundAttributeParameterFlags.IsEnum);
    public bool IsStringProperty => _typeNameObject.IsString;
    public bool IsBooleanProperty => _typeNameObject.IsBoolean;
    public bool BindAttributeGetSet => _flags.IsFlagSet(BoundAttributeParameterFlags.BindAttributeGetSet);

    public BoundAttributeParameter(
        BoundAttribute parent,
        ref readonly BoundAttributeParameterData data)
    {
        Parent = parent;
        _flags = data.Flags;
        Name = data.Name;
        PropertyName = data.PropertyName;
        _typeNameObject = data.TypeNameObject;
        _documentationObject = data.DocumentationObject;
        Diagnostics = data.Diagnostics.ToImmutableArray();
    }

    public override string ToString()
        => DisplayName;
}
