// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// A metadata class describing a tag helper attribute.
/// </summary>
public sealed class BoundAttributeDescriptor : TagHelperObject<BoundAttributeDescriptor>
{
    private readonly BoundAttributeFlags _flags;
    private readonly DocumentationObject _documentationObject;

    private TagHelperDescriptor? _parent;

    internal BoundAttributeFlags Flags => _flags;

    public string Kind => Parent.Kind;
    public string Name { get; }
    public string? PropertyName { get; }
    public string TypeName { get; }
    public string DisplayName { get; }
    public string? ContainingType { get; }

    public string? IndexerNamePrefix { get; }
    public string? IndexerTypeName { get; }

    public bool CaseSensitive => _flags.IsFlagSet(BoundAttributeFlags.CaseSensitive);
    public bool HasIndexer => _flags.IsFlagSet(BoundAttributeFlags.HasIndexer);
    public bool IsIndexerStringProperty => _flags.IsFlagSet(BoundAttributeFlags.IsIndexerStringProperty);
    public bool IsIndexerBooleanProperty => _flags.IsFlagSet(BoundAttributeFlags.IsIndexerBooleanProperty);
    public bool IsEnum => _flags.IsFlagSet(BoundAttributeFlags.IsEnum);
    public bool IsStringProperty => _flags.IsFlagSet(BoundAttributeFlags.IsStringProperty);
    public bool IsBooleanProperty => _flags.IsFlagSet(BoundAttributeFlags.IsBooleanProperty);
    internal bool IsEditorRequired => _flags.IsFlagSet(BoundAttributeFlags.IsEditorRequired);
    public bool IsDirectiveAttribute => _flags.IsFlagSet(BoundAttributeFlags.IsDirectiveAttribute);

    public ImmutableArray<BoundAttributeParameterDescriptor> Parameters { get; }
    public MetadataCollection Metadata { get; }

    internal BoundAttributeDescriptor(
        BoundAttributeFlags flags,
        string name,
        string? propertyName,
        string typeName,
        string? indexerNamePrefix,
        string? indexerTypeName,
        DocumentationObject documentationObject,
        string displayName,
        string? containingType,
        ImmutableArray<BoundAttributeParameterDescriptor> parameters,
        MetadataCollection metadata,
        ImmutableArray<RazorDiagnostic> diagnostics)
        : base(diagnostics)
    {
        Name = name;
        PropertyName = propertyName;
        TypeName = typeName;
        IndexerNamePrefix = indexerNamePrefix;
        IndexerTypeName = indexerTypeName;
        _documentationObject = documentationObject;
        DisplayName = displayName;
        ContainingType = containingType;
        Parameters = parameters.NullToEmpty();
        Metadata = metadata ?? MetadataCollection.Empty;

        if (indexerTypeName == typeof(string).FullName || indexerTypeName == "string")
        {
            flags |= BoundAttributeFlags.IsIndexerStringProperty;
        }

        if (indexerTypeName == typeof(bool).FullName || indexerTypeName == "bool")
        {
            flags |= BoundAttributeFlags.IsIndexerBooleanProperty;
        }

        if (typeName == typeof(string).FullName || typeName == "string")
        {
            flags |= BoundAttributeFlags.IsStringProperty;
        }

        if (typeName == typeof(bool).FullName || typeName == "bool")
        {
            flags |= BoundAttributeFlags.IsBooleanProperty;
        }

        _flags = flags;

        foreach (var parameter in Parameters)
        {
            parameter.SetParent(this);
        }
    }

    private protected override void BuildChecksum(in Checksum.Builder builder)
    {
        builder.AppendData((ushort)Flags);
        builder.AppendData(Name);
        builder.AppendData(PropertyName);
        builder.AppendData(TypeName);
        builder.AppendData(IndexerNamePrefix);
        builder.AppendData(IndexerTypeName);
        builder.AppendData(DisplayName);
        builder.AppendData(ContainingType);

        DocumentationObject.AppendToChecksum(in builder);

        foreach (var descriptor in Parameters)
        {
            builder.AppendData(descriptor.Checksum);
        }

        builder.AppendData(Metadata.Checksum);
    }

    internal void SetParent(TagHelperDescriptor parent)
    {
        if (_parent is not null)
        {
            ThrowHelper.ThrowInvalidOperationException("Parent can only be set once.");
        }

        _parent = parent;
    }

    public TagHelperDescriptor Parent
        => _parent ?? Assumed.Unreachable<TagHelperDescriptor>($"{nameof(Parent)} not set.");

    public string? Documentation => _documentationObject.GetText();

    internal DocumentationObject DocumentationObject => _documentationObject;

    public IEnumerable<RazorDiagnostic> GetAllDiagnostics()
    {
        foreach (var parameter in Parameters)
        {
            foreach (var diagnostic in parameter.Diagnostics)
            {
                yield return diagnostic;
            }
        }

        foreach (var diagnostic in Diagnostics)
        {
            yield return diagnostic;
        }
    }

    public override string ToString()
    {
        return DisplayName ?? base.ToString()!;
    }
}
