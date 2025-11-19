// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

internal readonly struct BoundAttributeParameterData
{
    public BoundAttributeParameterFlags Flags { get; }
    public string Name { get; }
    public string PropertyName { get; }
    public TypeNameObject TypeNameObject { get; }
    public DocumentationObject DocumentationObject { get; }

    public BoundAttributeParameterData(
        BoundAttributeParameterFlags flags,
        string name,
        string propertyName,
        TypeNameObject typeNameObject,
        DocumentationObject documentationObject)
    {
        Flags = flags;
        Name = name;
        PropertyName = propertyName;
        TypeNameObject = typeNameObject;
        DocumentationObject = documentationObject;
    }

    internal static BoundAttributeParameterData Create<T>(
        string name,
        string propertyName,
        DocumentationObject documentation = default,
        Optional<bool> bindAttributeGetSet = default)
        => Create(name, propertyName, typeof(T).FullName, documentation, bindAttributeGetSet);

    internal static BoundAttributeParameterData Create(
        string name,
        string propertyName,
        string typeName,
        DocumentationObject documentation = default,
        Optional<bool> bindAttributeGetSet = default)
    {
        ArgHelper.ThrowIfNull(name);
        ArgHelper.ThrowIfNull(propertyName);
        ArgHelper.ThrowIfNull(typeName);

        BoundAttributeParameterFlags flags = 0;

        if (bindAttributeGetSet.HasValue && bindAttributeGetSet.Value)
        {
            flags |= BoundAttributeParameterFlags.BindAttributeGetSet;
        }

        return new(flags, name, propertyName, TypeNameObject.From(typeName), documentation);
    }

    public ImmutableArray<RazorDiagnostic> GetDiagnostics(string attributeName)
    {
        var name = Name;

        if (name.IsNullOrWhiteSpace())
        {
            var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributeParameterNullOrWhitespace(attributeName);
            return [diagnostic];
        }

        using var _ = HashSetPool<Checksum>.GetPooledObject(out var seenChecksums);
        using var diagnostics = new PooledArrayBuilder<RazorDiagnostic>();

        foreach (var character in name)
        {
            if (char.IsWhiteSpace(character) || HtmlConventions.IsInvalidNonWhitespaceHtmlCharacters(character))
            {
                var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributeParameterName(
                    attributeName,
                    name,
                    character);

                if (seenChecksums.Add(diagnostic.Checksum))
                {
                    diagnostics.Add(diagnostic);
                }
            }
        }

        return diagnostics.ToImmutableAndClear();
    }
}
