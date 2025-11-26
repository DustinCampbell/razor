// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RequiredAttributeState
{
    public RequiredAttributeFlags Flags { get; }
    public string Name { get; }
    public RequiredAttributeNameComparison NameComparison { get; }
    public string? Value { get; }
    public RequiredAttributeValueComparison ValueComparison { get; }
    public ImmutableArray<RazorDiagnostic> Diagnostics { get; }

    private RequiredAttributeState(
        RequiredAttributeFlags flags,
        string name,
        RequiredAttributeNameComparison nameComparison,
        string? value,
        RequiredAttributeValueComparison valueComparison,
        ImmutableArray<RazorDiagnostic> diagnostics)
    {
        Flags = flags;
        Name = name;
        NameComparison = nameComparison;
        Value = value;
        ValueComparison = valueComparison;
        Diagnostics = diagnostics;
    }

    public static RequiredAttributeState Create(
        string name,
        RequiredAttributeNameComparison nameComparison = default,
        string? value = null,
        RequiredAttributeValueComparison valueComparison = default,
        bool caseSensitive = false,
        bool isDirectiveAttribute = false,
        ImmutableArray<RazorDiagnostic> diagnostics = default)
    {
        var flags = RequiredAttributeFlags.None;

        if (caseSensitive)
        {
            flags |= RequiredAttributeFlags.CaseSensitive;
        }
        
        if (isDirectiveAttribute)
        {
            flags |= RequiredAttributeFlags.IsDirectiveAttribute;
        }

        diagnostics = CollectAndDedupeDiagnostics(diagnostics, name, nameComparison, isDirectiveAttribute);

        return new(flags, name, nameComparison, value, valueComparison, diagnostics);
    }

    private static ImmutableArray<RazorDiagnostic> CollectAndDedupeDiagnostics(
        ImmutableArray<RazorDiagnostic> currentDiagnostics,
        string name,
        RequiredAttributeNameComparison nameComparison,
        bool isDirectiveAttribute)
    {
        using var newDiagnostics = new PooledArrayBuilder<RazorDiagnostic>();
        CollectDiagnostics(name, nameComparison, isDirectiveAttribute, ref newDiagnostics.AsRef());

        var count = !currentDiagnostics.IsDefaultOrEmpty
            ? currentDiagnostics.Length + newDiagnostics.Count
            : newDiagnostics.Count;

        if (count == 0)
        {
            return [];
        }

        using var _ = HashSetPool<Checksum>.GetPooledObject(out var checksums);

#if NET
        checksums.EnsureCapacity(count);
#endif

        using var builder = new PooledArrayBuilder<RazorDiagnostic>(count);

        foreach (var diagnostic in newDiagnostics)
        {
            if (checksums.Add(diagnostic.Checksum))
            {
                builder.Add(diagnostic);
            }
        }

        foreach (var diagnostic in currentDiagnostics)
        {
            if (checksums.Add(diagnostic.Checksum))
            {
                builder.Add(diagnostic);
            }
        }

        return builder.ToImmutableAndClear();
    }

    private static void CollectDiagnostics(
        string name, RequiredAttributeNameComparison nameComparison, bool isDirectiveAttribute,
        ref PooledArrayBuilder<RazorDiagnostic> diagnostics)
    {
        if (name.IsNullOrWhiteSpace())
        {
            var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedAttributeNameNullOrWhitespace();

            diagnostics.Add(diagnostic);
            return;
        }

        var nameSpan = name.AsSpan();
        Debug.Assert(nameSpan.Length > 0, "Name should not be empty at this point.");

        if (isDirectiveAttribute)
        {
            if (nameSpan[0] == '@')
            {
                nameSpan = nameSpan[1..];
            }
            else
            {
                var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidRequiredDirectiveAttributeName(
                    RequiredAttributeDescriptor.GetDisplayName(name, nameComparison), name);

                diagnostics.Add(diagnostic);
            }
        }

        foreach (var ch in nameSpan)
        {
            if (char.IsWhiteSpace(ch) || HtmlConventions.IsInvalidNonWhitespaceHtmlCharacters(ch))
            {
                var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedAttributeName(name, ch);

                diagnostics.Add(diagnostic);
            }
        }
    }
}
