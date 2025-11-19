// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers;

internal sealed class RazorSymbol
{
    private readonly RazorSymbolFlags _flags;
    private readonly TypeNameObject _typeNameObject;
    private readonly DocumentationObject _documentationObject;

    public RazorSymbolKind Kind { get; }
    public RuntimeKind RuntimeKind { get; }

    public string Name { get; }
    public string AssemblyName { get; }
    public string DisplayName { get; }
    public string? TagOutputHint { get; }
    public MetadataObject Metadata { get; }

    public ImmutableArray<AllowedChildTag> AllowedChildTags { get; }
    public ImmutableArray<BoundAttribute> Attributes { get; }
    public ImmutableArray<TagMatchingRule> MatchingRules { get; }
    public ImmutableArray<RazorDiagnostic> Diagnostics { get; }

    public Checksum Checksum { get; }

    public string TypeName => _typeNameObject.FullName.AssumeNotNull();
    public string? TypeNamespace => _typeNameObject.Namespace;
    public string? TypeNameIdentifier => _typeNameObject.Name;

    public string? Documentation => _documentationObject.GetText();

    public bool CaseSensitive => _flags.IsFlagSet(RazorSymbolFlags.CaseSensitive);

    /// <summary>
    /// Gets whether the component matches a tag with a fully qualified name.
    /// </summary>
    internal bool IsFullyQualifiedNameMatch => _flags.IsFlagSet(RazorSymbolFlags.IsFullyQualifiedNameMatch);

    public bool ClassifyAttributesOnly => _flags.IsFlagSet(RazorSymbolFlags.ClassifyAttributesOnly);

    public RazorSymbol(ref readonly RazorSymbolData data)
    {
        _flags = data.Flags;
        Kind = data.Kind;
        RuntimeKind = data.RuntimeKind;
        Name = data.Name;
        AssemblyName = data.AssemblyName;
        DisplayName = data.DisplayName;
        TagOutputHint = data.TagOutputHint;
        _typeNameObject = data.TypeNameObject;
        _documentationObject = data.DocumentationObject;
        Metadata = data.MetadataObject;

        AllowedChildTags = CreateAllowedChildTags(data.AllowedChildTags);
        Attributes = CreateAttributes(data.Attributes);
        MatchingRules = CreateMatchingRules(data.MatchingRules);
        Diagnostics = data.Diagnostics.ToImmutableArray();

        Checksum = data.GetChecksum();
    }

    private ImmutableArray<AllowedChildTag> CreateAllowedChildTags(ReadOnlySpan<AllowedChildTagData> allowedChildTags)
    {
        if (allowedChildTags.Length == 0)
        {
            return [];
        }

        var array = new AllowedChildTag[allowedChildTags.Length];

        for (var i = 0; i < allowedChildTags.Length; i++)
        {
            array[i] = new(this, in allowedChildTags[i]);
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(array);
    }

    private ImmutableArray<BoundAttribute> CreateAttributes(ReadOnlySpan<BoundAttributeData> attributes)
    {
        if (attributes.Length == 0)
        {
            return [];
        }

        var array = new BoundAttribute[attributes.Length];

        for (var i = 0; i < attributes.Length; i++)
        {
            array[i] = new(this, in attributes[i]);
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(array);
    }

    private ImmutableArray<TagMatchingRule> CreateMatchingRules(ReadOnlySpan<TagMatchingRuleData> matchingRules)
    {
        if (matchingRules.Length == 0)
        {
            return [];
        }

        var array = new TagMatchingRule[matchingRules.Length];

        for (var i = 0; i < matchingRules.Length; i++)
        {
            array[i] = new(this, in matchingRules[i]);
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(array);
    }

}
