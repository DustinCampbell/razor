// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;

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

    private protected override void BuildChecksum(ref readonly Checksum.Builder builder)
    {
        GetValues(out var flags, out var name, out var propertyName, out var typeNameObject, out var documentationObject);

        BoundAttributeParameterDescriptor.AppendChecksumValues(in builder, flags, name, propertyName, typeNameObject, documentationObject);
    }

    private protected override BoundAttributeParameterDescriptor BuildCore(ImmutableArray<RazorDiagnostic> diagnostics)
    {
        GetValues(out var flags, out var name, out var propertyName, out var typeNameObject, out var documentationObject);

        return new(flags, name, propertyName, typeNameObject, documentationObject, diagnostics);
    }

    private void GetValues(
        out BoundAttributeParameterFlags flags,
        out string name, out string propertyName,
        out TypeNameObject typeNameObject, out DocumentationObject documentationObject)
    {
        flags = _flags;

        if (CaseSensitive)
        {
            flags |= BoundAttributeParameterFlags.CaseSensitive;
        }

        name = Name ?? string.Empty;
        propertyName = PropertyName ?? string.Empty;

        typeNameObject = _typeNameObject;
        documentationObject = _documentationObject;
    }

    private protected override void CollectDiagnostics(ref PooledHashSet<RazorDiagnostic> diagnostics)
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
