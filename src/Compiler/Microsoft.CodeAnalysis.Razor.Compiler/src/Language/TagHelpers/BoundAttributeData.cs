// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers;

internal readonly struct BoundAttributeData(
    BoundAttributeFlags flags,
    string name,
    string propertyName,
    string? indexerNamePrefix,
    string displayName,
    string? containingType,
    TypeNameObject typeNameObject,
    TypeNameObject indexerTypeNameObject,
    DocumentationObject documentationObject,
    MetadataObject metadataObject,
    ReadOnlyMemory<BoundAttributeParameterData> parameters,
    ReadOnlyMemory<RazorDiagnostic> diagnostics)
{
    public BoundAttributeFlags Flags => flags;
    public string Name => name;
    public string PropertyName => propertyName;
    public string? IndexerNamePrefix => indexerNamePrefix;
    public string DisplayName => displayName;
    public string? ContainingType => containingType;
    public TypeNameObject TypeNameObject => typeNameObject;
    public TypeNameObject IndexerTypeNameObject => indexerTypeNameObject;
    public DocumentationObject DocumentationObject => documentationObject;
    public MetadataObject MetadataObject => metadataObject;

    public ReadOnlySpan<BoundAttributeParameterData> Parameters => parameters.Span;
    public ReadOnlySpan<RazorDiagnostic> Diagnostics => diagnostics.Span;

    public Checksum GetChecksum()
    {
        var builder = new Checksum.Builder();

        builder.Append((byte)flags);
        builder.Append(name);
        builder.Append(propertyName);
        builder.Append(indexerNamePrefix);
        builder.Append(displayName);
        builder.Append(containingType);

        typeNameObject.AppendToChecksum(in builder);
        indexerTypeNameObject.AppendToChecksum(in builder);
        documentationObject.AppendToChecksum(in builder);
        metadataObject.AppendToChecksum(in builder);

        foreach (var parameter in Parameters)
        {
            builder.Append(parameter.GetChecksum());
        }

        foreach (var diagnostic in Diagnostics)
        {
            builder.Append(diagnostic.Checksum);
        }

        return builder.FreeAndGetChecksum();
    }
}
