// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language.TagHelpers;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial class BoundAttributeParameterDescriptorBuilder : TagHelperObjectBuilder<BoundAttributeParameterDescriptor>
{
    [AllowNull]
    private BoundAttributeDescriptorBuilder _parent;
    private BoundAttributeParameterFlags _flags;
    private DocumentationObject _documentationObject;
    private TypeNameObject _typeNameObject;

    private BoundAttributeParameterDescriptorBuilder()
    {
    }

    internal BoundAttributeParameterDescriptorBuilder(BoundAttributeDescriptorBuilder parent)
    {
        _parent = parent;
    }

    public string? Name { get; set; }
    public string? PropertyName { get; set; }

    public string? TypeName
    {
        get => _typeNameObject.FullName;
        set => _typeNameObject = TypeNameObject.From(value);
    }

    public bool IsEnum
    {
        get => _flags.IsFlagSet(BoundAttributeParameterFlags.IsEnum);
        set => _flags.UpdateFlag(BoundAttributeParameterFlags.IsEnum, value);
    }

    public bool BindAttributeGetSet
    {
        get => _flags.IsFlagSet(BoundAttributeParameterFlags.BindAttributeGetSet);
        set => _flags.UpdateFlag(BoundAttributeParameterFlags.BindAttributeGetSet, value);
    }

    public string? Documentation
    {
        get => _documentationObject.GetText();
        set => _documentationObject = new(value);
    }

    internal bool CaseSensitive => _parent.CaseSensitive;

    internal void SetDocumentation(string? text)
    {
        _documentationObject = new(text);
    }

    internal void SetDocumentation(DocumentationDescriptor? documentation)
    {
        _documentationObject = new(documentation);
    }

    private protected override BoundAttributeParameterDescriptor BuildCore(ImmutableArray<RazorDiagnostic> diagnostics)
    {
        var flags = _flags;

        if (CaseSensitive)
        {
            flags |= BoundAttributeParameterFlags.CaseSensitive;
        }

        var name = Name ?? string.Empty;
        var propertyName = PropertyName ?? string.Empty;
        var typeNameObject = _typeNameObject;
        var documentationObject = _documentationObject;

        var checksum = ChecksumFactory.ComputeForBoundAttributeParameter(
            flags, name, propertyName, typeNameObject, documentationObject, diagnostics);

        return new BoundAttributeParameterDescriptor(
            flags, name, propertyName, typeNameObject, documentationObject, checksum, diagnostics);
    }

    private protected override void CollectDiagnostics(ref PooledArrayBuilder<RazorDiagnostic> diagnostics)
    {
        if (Name.IsNullOrWhiteSpace())
        {
            var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributeParameterNullOrWhitespace(_parent.Name);

            diagnostics.Add(diagnostic);
        }
        else
        {
            foreach (var character in Name)
            {
                if (char.IsWhiteSpace(character) || HtmlConventions.IsInvalidNonWhitespaceHtmlCharacters(character))
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributeParameterName(
                        _parent.Name,
                        Name,
                        character);

                    diagnostics.Add(diagnostic);
                }
            }
        }
    }
}
