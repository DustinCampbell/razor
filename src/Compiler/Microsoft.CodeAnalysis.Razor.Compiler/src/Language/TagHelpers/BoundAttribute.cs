// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers;

internal sealed class BoundAttribute
{
    private readonly BoundAttributeFlags _flags;
    private readonly TypeNameObject _typeNameObject;
    private readonly TypeNameObject _indexerTypeNameObject;
    private readonly DocumentationObject _documentationObject;

    public RazorSymbol Parent { get; }
    public string Name { get; }
    public string PropertyName { get; }
    public string? IndexerNamePrefix { get; }
    public string DisplayName { get; }
    public string? ContainingType { get; }

    public MetadataObject Metadata { get; }
    public ImmutableArray<BoundAttributeParameter> Parameters { get; }
    public ImmutableArray<RazorDiagnostic> Diagnostics { get; }

    public string TypeName => _typeNameObject.FullName.AssumeNotNull();
    public string? IndexerTypeName => _indexerTypeNameObject.FullName;
    public string? Documentation => _documentationObject.GetText();

    public bool CaseSensitive => _flags.IsFlagSet(BoundAttributeFlags.CaseSensitive);
    public bool HasIndexer => _flags.IsFlagSet(BoundAttributeFlags.HasIndexer);
    public bool IsIndexerStringProperty => _indexerTypeNameObject.IsString;
    public bool IsIndexerBooleanProperty => _indexerTypeNameObject.IsBoolean;
    public bool IsEnum => _flags.IsFlagSet(BoundAttributeFlags.IsEnum);
    public bool IsStringProperty => _typeNameObject.IsString;
    public bool IsBooleanProperty => _typeNameObject.IsBoolean;
    public bool IsEditorRequired => _flags.IsFlagSet(BoundAttributeFlags.IsEditorRequired);
    public bool IsDirectiveAttribute => _flags.IsFlagSet(BoundAttributeFlags.IsDirectiveAttribute);
    public bool IsWeaklyTyped => _flags.IsFlagSet(BoundAttributeFlags.IsWeaklyTyped);

    public BoundAttribute(
        RazorSymbol parent,
        ref readonly BoundAttributeData data)
    {
        Parent = parent;
        _flags = data.Flags;
        Name = data.Name;
        PropertyName = data.PropertyName;
        IndexerNamePrefix = data.IndexerNamePrefix;
        DisplayName = data.DisplayName;
        ContainingType = data.ContainingType;
        _typeNameObject = data.TypeNameObject;
        _indexerTypeNameObject = data.IndexerTypeNameObject;
        _documentationObject = data.DocumentationObject;
        Metadata = data.MetadataObject;

        Parameters = CreateParameters(data.Parameters);
        Diagnostics = data.Diagnostics.ToImmutableArray();
    }

    private ImmutableArray<BoundAttributeParameter> CreateParameters(ReadOnlySpan<BoundAttributeParameterData> parameters)
    {
        if (parameters.Length == 0)
        {
            return [];
        }

        var array = new BoundAttributeParameter[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            array[i] = new(this, in parameters[i]);
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(array);
    }
}
