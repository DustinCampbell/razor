// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers;

internal readonly struct BoundAttributeParameterData(
    BoundAttributeParameterFlags flags,
    string name,
    string propertyName,
    TypeNameObject typeNameObject,
    DocumentationObject documentationObject,
    ReadOnlyMemory<RazorDiagnostic> diagnostics)
{
    public BoundAttributeParameterFlags Flags => flags;
    public string Name => name;
    public string PropertyName => propertyName;
    public TypeNameObject TypeNameObject => typeNameObject;
    public DocumentationObject DocumentationObject => documentationObject;
    public ReadOnlySpan<RazorDiagnostic> Diagnostics => diagnostics.Span;

    public Checksum GetChecksum()
    {
        var builder = new Checksum.Builder();

        builder.Append((byte)flags);
        builder.Append(name);
        builder.Append(propertyName);
        typeNameObject.AppendToChecksum(in builder);
        documentationObject.AppendToChecksum(in builder);

        foreach (var diagnostic in Diagnostics)
        {
            builder.Append(diagnostic.Checksum);
        }

        return builder.FreeAndGetChecksum();
    }
}
