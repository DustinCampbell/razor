// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers;

internal static class ChecksumFactory
{
    public static Checksum Compute(TagHelperDescriptor tagHelper)
        => ComputeForTagHelper(
            tagHelper.Flags, tagHelper.Kind, tagHelper.RuntimeKind,
            tagHelper.Name, tagHelper.AssemblyName, tagHelper.DisplayName,
            tagHelper.TypeNameObject, tagHelper.DocumentationObject, tagHelper.TagOutputHint,
            tagHelper.AllowedChildTags, tagHelper.BoundAttributes, tagHelper.TagMatchingRules,
            tagHelper.Metadata, tagHelper.Diagnostics);

    public static Checksum ComputeForTagHelper(
        TagHelperFlags flags,
        TagHelperKind kind,
        RuntimeKind runtimeKind,
        string name,
        string assemblyName,
        string displayName,
        TypeNameObject typeNameObject,
        DocumentationObject documentationObject,
        string? tagOutputHint,
        ImmutableArray<AllowedChildTagDescriptor> allowedChildTags,
        ImmutableArray<BoundAttributeDescriptor> boundAttributes,
        ImmutableArray<TagMatchingRuleDescriptor> tagMatchingRules,
        MetadataObject metadata,
        ImmutableArray<RazorDiagnostic> diagnostics)
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

        foreach (var descriptor in allowedChildTags)
        {
            builder.Append(descriptor.Checksum);
        }

        foreach (var descriptor in boundAttributes)
        {
            builder.Append(descriptor.Checksum);
        }

        foreach (var descriptor in tagMatchingRules)
        {
            builder.Append(descriptor.Checksum);
        }

        metadata.AppendToChecksum(in builder);

        foreach (var diagnostic in diagnostics)
        {
            builder.Append(diagnostic.Checksum);
        }

        return builder.FreeAndGetChecksum();
    }

    public static Checksum ComputeForAllowedChildTag(string name, string displayName, ImmutableArray<RazorDiagnostic> diagnostics)
    {
        var builder = new Checksum.Builder();

        builder.Append(name);
        builder.Append(displayName);

        foreach (var diagnostic in diagnostics)
        {
            builder.Append(diagnostic.Checksum);
        }

        return builder.FreeAndGetChecksum();
    }

    public static Checksum ComputeForBoundAttribute(
        BoundAttributeFlags flags,
        string name,
        string propertyName,
        TypeNameObject typeNameObject,
        string? indexerNamePrefix,
        TypeNameObject indexerTypeNameObject,
        DocumentationObject documentationObject,
        string displayName,
        string? containingType,
        ImmutableArray<BoundAttributeParameterDescriptor> parameters,
        MetadataObject metadata,
        ImmutableArray<RazorDiagnostic> diagnostics)
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

        foreach (var descriptor in parameters)
        {
            builder.Append(descriptor.Checksum);
        }

        metadata.AppendToChecksum(in builder);

        foreach (var diagnostic in diagnostics)
        {
            builder.Append(diagnostic.Checksum);
        }

        return builder.FreeAndGetChecksum();
    }

    public static Checksum ComputeForBoundAttributeParameter(
        BoundAttributeParameterFlags flags,
        string name,
        string propertyName,
        TypeNameObject typeNameObject,
        DocumentationObject documentationObject,
        ImmutableArray<RazorDiagnostic> diagnostics)
    {
        var builder = new Checksum.Builder();

        builder.Append((byte)flags);
        builder.Append(name);
        builder.Append(propertyName);

        typeNameObject.AppendToChecksum(in builder);
        documentationObject.AppendToChecksum(in builder);

        foreach (var diagnostic in diagnostics)
        {
            builder.Append(diagnostic.Checksum);
        }

        return builder.FreeAndGetChecksum();
    }

    public static Checksum ComputeForTagMatchingRule(
        string tagName,
        string? parentTag,
        TagStructure tagStructure,
        bool caseSensitive,
        ImmutableArray<RequiredAttributeDescriptor> attributes,
        ImmutableArray<RazorDiagnostic> diagnostics)
    {
        var builder = new Checksum.Builder();

        builder.Append(tagName);
        builder.Append(parentTag);
        builder.Append((int)tagStructure);

        builder.Append(caseSensitive);

        foreach (var attribute in attributes)
        {
            builder.Append(attribute.Checksum);
        }

        foreach (var diagnostic in diagnostics)
        {
            builder.Append(diagnostic.Checksum);
        }

        return builder.FreeAndGetChecksum();
    }

    public static Checksum ComputeForRequiredAttribute(
        RequiredAttributeFlags flags,
        string name,
        RequiredAttributeNameComparison nameComparison,
        string? value,
        RequiredAttributeValueComparison valueComparison,
        ImmutableArray<RazorDiagnostic> diagnostics)
    {
        var builder = new Checksum.Builder();

        builder.Append((int)flags);
        builder.Append(name);
        builder.Append((int)nameComparison);
        builder.Append(value);
        builder.Append((int)valueComparison);

        foreach (var diagnostic in diagnostics)
        {
            builder.Append(diagnostic.Checksum);
        }

        return builder.FreeAndGetChecksum();
    }
}
