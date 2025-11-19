// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.TagHelpers;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial class TagHelperDescriptorBuilder : TagHelperObjectBuilder<TagHelperDescriptor>
{
    private TagHelperFlags _flags;
    private TagHelperKind _kind;
    private string? _name;
    private string? _assemblyName;
    private TypeNameObject _typeNameObject;
    private DocumentationObject _documentationObject;
    private MetadataObject? _metadataObject;

    private TagHelperDescriptorBuilder()
    {
    }

    internal TagHelperDescriptorBuilder(TagHelperKind kind, string name, string assemblyName)
        : this()
    {
        _kind = kind;
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _assemblyName = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));
    }

    public static TagHelperDescriptorBuilder Create(string name, string assemblyName)
        => new(TagHelperKind.ITagHelper, name, assemblyName);

    public static TagHelperDescriptorBuilder Create(TagHelperKind kind, string name, string assemblyName)
        => new(kind, name, assemblyName);

    public TagHelperKind Kind => _kind;
    public RuntimeKind RuntimeKind { get; set; }

    public string Name => _name.AssumeNotNull();
    public string AssemblyName => _assemblyName.AssumeNotNull();
    public string? DisplayName { get; set; }
    public string? TagOutputHint { get; set; }

    public string? TypeName
    {
        get => _typeNameObject.FullName;
        set => _typeNameObject = TypeNameObject.From(value);
    }

    public string? TypeNamespace => _typeNameObject.Namespace;
    public string? TypeNameIdentifier => _typeNameObject.Name;

    public bool CaseSensitive
    {
        get => _flags.IsFlagSet(TagHelperFlags.CaseSensitive);
        set => _flags.UpdateFlag(TagHelperFlags.CaseSensitive, value);
    }

    public bool IsFullyQualifiedNameMatch
    {
        get => _flags.IsFlagSet(TagHelperFlags.IsFullyQualifiedNameMatch);
        set => _flags.UpdateFlag(TagHelperFlags.IsFullyQualifiedNameMatch, value);
    }

    public bool ClassifyAttributesOnly
    {
        get => _flags.IsFlagSet(TagHelperFlags.ClassifyAttributesOnly);
        set => _flags.UpdateFlag(TagHelperFlags.ClassifyAttributesOnly, value);
    }

    public string? Documentation
    {
        get => _documentationObject.GetText();
        set => _documentationObject = new(value);
    }

    public void SetMetadata(MetadataObject metadataObject)
    {
        _metadataObject = metadataObject;
    }

    public MetadataObject MetadataObject => _metadataObject ?? MetadataObject.None;

    internal void SetTypeName(TypeNameObject typeName)
    {
        _typeNameObject = typeName;
    }

    public void SetTypeName(string fullName, string? typeNamespace, string? typeNameIdentifier)
    {
        _typeNameObject = TypeNameObject.From(fullName, typeNamespace, typeNameIdentifier);
    }

    public void SetTypeName(INamedTypeSymbol namedType)
    {
        _typeNameObject = TypeNameObject.From(namedType);
    }

    public TagHelperObjectBuilderCollection<AllowedChildTagDescriptor, AllowedChildTagDescriptorBuilder> AllowedChildTags { get; }
        = new(AllowedChildTagDescriptorBuilder.Pool);

    public TagHelperObjectBuilderCollection<BoundAttributeDescriptor, BoundAttributeDescriptorBuilder> BoundAttributes { get; }
        = new(BoundAttributeDescriptorBuilder.Pool);

    public TagHelperObjectBuilderCollection<TagMatchingRuleDescriptor, TagMatchingRuleDescriptorBuilder> TagMatchingRules { get; }
        = new(TagMatchingRuleDescriptorBuilder.Pool);

    public void AllowChildTag(Action<AllowedChildTagDescriptorBuilder> configure)
    {
        ArgHelper.ThrowIfNull(configure);

        var builder = AllowedChildTagDescriptorBuilder.GetInstance(this);
        configure(builder);
        AllowedChildTags.Add(builder);
    }

    public void BindAttribute(Action<BoundAttributeDescriptorBuilder> configure)
    {
        ArgHelper.ThrowIfNull(configure);

        var builder = BoundAttributeDescriptorBuilder.GetInstance(this);
        configure(builder);
        BoundAttributes.Add(builder);
    }

    public void TagMatchingRule(Action<TagMatchingRuleDescriptorBuilder> configure)
    {
        ArgHelper.ThrowIfNull(configure);

        var builder = TagMatchingRuleDescriptorBuilder.GetInstance(this);
        configure(builder);
        TagMatchingRules.Add(builder);
    }

    internal void SetDocumentation(string? text)
    {
        _documentationObject = new(text);
    }

    internal void SetDocumentation(DocumentationDescriptor? documentation)
    {
        _documentationObject = new(documentation);
    }

    private protected override void BuildChecksum(ref readonly Checksum.Builder builder)
    {
        GetValues(
            out var flags, out var kind, out var runtimeKind,
            out var name, out var assemblyName, out var displayName, out var tagOutputHint,
            out var typeNameObject, out var documentationObject);

        TagHelperDescriptor.AppendChecksumValues(
            in builder, flags, kind, runtimeKind, name, assemblyName,
            displayName, tagOutputHint, typeNameObject, documentationObject);

        using var _ = HashSetPool<Checksum>.GetPooledObject(out var checksums);

        foreach (var item in AllowedChildTags)
        {
            var checksum = item.GetChecksum();
            if (checksums.Add(checksum))
            {
                builder.Append(checksum);
            }
        }

        checksums.Clear();

        foreach (var item in BoundAttributes)
        {
            var checksum = item.GetChecksum();
            if (checksums.Add(checksum))
            {
                builder.Append(checksum);
            }
        }

        checksums.Clear();

        foreach (var item in TagMatchingRules)
        {
            var checksum = item.GetChecksum();
            if (checksums.Add(checksum))
            {
                builder.Append(checksum);
            }
        }

        MetadataObject.AppendToChecksum(in builder);
    }

    private protected override TagHelperDescriptor BuildCore(ImmutableArray<RazorDiagnostic> diagnostics)
    {
        // First, compute the checksum for this TagHelperDescriptor. There's no need to
        // create a TagHelperDescriptor if we already have a cached version.
        var builder = new Checksum.Builder();

        BuildChecksum(in builder);

        foreach (var diagnostic in diagnostics)
        {
            builder.Append(diagnostic.Checksum);
        }

        var checksum = builder.FreeAndGetChecksum();

        if (TagHelperCache.Default.TryGet(checksum, out var result))
        {
            return result;
        }

        GetValues(
            out var flags, out var kind, out var runtimeKind,
            out var name, out var assemblyName, out var displayName, out var tagOutputHint,
            out var typeNameObject, out var documentationObject);

        result = new(flags, kind, runtimeKind, name, assemblyName, displayName,
            typeNameObject, documentationObject, tagOutputHint,
            TagMatchingRules.BuildAll(), BoundAttributes.BuildAll(), AllowedChildTags.BuildAll(),
            MetadataObject, diagnostics, checksum);

        return TagHelperCache.Default.GetOrAdd(checksum, result);
    }

    private void GetValues(
        out TagHelperFlags flags, out TagHelperKind kind, out RuntimeKind runtimeKind,
        out string name, out string assemblyName, out string displayName, out string? tagOutputHint,
        out TypeNameObject typeNameObject, out DocumentationObject documentationObject)
    {
        flags = _flags;
        kind = Kind;
        runtimeKind = RuntimeKind;
        name = Name;
        assemblyName = AssemblyName;
        displayName = GetDisplayName();
        tagOutputHint = TagOutputHint;
        typeNameObject = _typeNameObject;
        documentationObject = _documentationObject;
    }

    internal string GetDisplayName()
    {
        return DisplayName ?? TypeName ?? Name;
    }
}
