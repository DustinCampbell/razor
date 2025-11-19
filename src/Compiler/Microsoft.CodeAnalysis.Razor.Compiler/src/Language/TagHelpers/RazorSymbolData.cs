// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers;

internal readonly struct RazorSymbolData(
    RazorSymbolFlags flags,
    RazorSymbolKind kind,
    RuntimeKind runtimeKind,
    string name,
    string assemblyName,
    string displayName,
    string? tagOutputHint,
    TypeNameObject typeNameObject,
    DocumentationObject documentationObject,
    MetadataObject metadataObject,
    ReadOnlyMemory<AllowedChildTagData> allowedChildTags,
    ReadOnlyMemory<BoundAttributeData> attributes,
    ReadOnlyMemory<TagMatchingRuleData> matchingRules,
    ReadOnlyMemory<RazorDiagnostic> diagnostics)
{
    public RazorSymbolFlags Flags => flags;
    public RazorSymbolKind Kind => kind;
    public RuntimeKind RuntimeKind => runtimeKind;
    public string Name => name;
    public string AssemblyName => assemblyName;
    public string DisplayName => displayName;
    public string? TagOutputHint => tagOutputHint;
    public TypeNameObject TypeNameObject => typeNameObject;
    public DocumentationObject DocumentationObject => documentationObject;
    public MetadataObject MetadataObject => metadataObject;

    public ReadOnlySpan<AllowedChildTagData> AllowedChildTags => allowedChildTags.Span;
    public ReadOnlySpan<BoundAttributeData> Attributes => attributes.Span;
    public ReadOnlySpan<TagMatchingRuleData> MatchingRules => matchingRules.Span;
    public ReadOnlySpan<RazorDiagnostic> Diagnostics => diagnostics.Span;

    public Checksum GetChecksum()
    {
        var builder = new Checksum.Builder();

        builder.Append((byte)flags);
        builder.Append((byte)kind);
        builder.Append((byte)runtimeKind);
        builder.Append(name);
        builder.Append(assemblyName);
        builder.Append(displayName);
        builder.Append(tagOutputHint);

        typeNameObject.AppendToChecksum(in builder);
        documentationObject.AppendToChecksum(in builder);
        metadataObject.AppendToChecksum(in builder);

        foreach (var childTag in AllowedChildTags)
        {
            builder.Append(childTag.GetChecksum());
        }

        foreach (var attribute in Attributes)
        {
            builder.Append(attribute.GetChecksum());
        }

        foreach (var rule in MatchingRules)
        {
            builder.Append(rule.GetChecksum());
        }

        foreach (var diagnostic in Diagnostics)
        {
            builder.Append(diagnostic.Checksum);
        }

        return builder.FreeAndGetChecksum();
    }
}
