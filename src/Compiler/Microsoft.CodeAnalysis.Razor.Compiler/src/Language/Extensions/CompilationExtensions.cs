// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

internal static class CompilationExtensions
{
    public static bool HasAddComponentParameter(this Compilation compilation)
    {
        return compilation.GetAspNetRuntimeTypesByMetadataName(ComponentsApi.RenderTreeBuilder.FullTypeName)
            .Any(static t =>
                t.DeclaredAccessibility == Accessibility.Public &&
                t.GetMembers(ComponentsApi.RenderTreeBuilder.AddComponentParameter)
                    .Any(static m => m.DeclaredAccessibility == Accessibility.Public));
    }

    public static bool TryGetAssemblySymbol(
        this Compilation compilation,
        MetadataReference reference,
        [NotNullWhen(true)] out IAssemblySymbol? result)
    {
        result = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
        return result is not null;
    }

    public static INamedTypeSymbol? GetAspNetRuntimeTypeByMetadataName(this Compilation compilation, string fullyQualifiedMetadataName)
        => compilation.Assembly.GetCachedTypeByMetadataName(fullyQualifiedMetadataName) ??
           compilation.GetCachedTypeFromReferencedAssemblies(fullyQualifiedMetadataName);

    public static bool TryGetAspNetRuntimeTypeByMetadataName(
        this Compilation compilation,
        string fullyQualifiedMetadataName,
        [NotNullWhen(true)] out INamedTypeSymbol? result)
        => compilation.Assembly.TryGetCachedTypeByMetadataName(fullyQualifiedMetadataName, out result) ||
           compilation.TryGetCachedTypeFromReferencedAssemblies(fullyQualifiedMetadataName, out result);

    public static ImmutableArray<INamedTypeSymbol> GetAspNetRuntimeTypesByMetadataName(
        this Compilation compilation,
        string fullyQualifiedMetadataName)
    {
        using var builder = new PooledArrayBuilder<INamedTypeSymbol>();

        if (compilation.Assembly.TryGetCachedTypeByMetadataName(fullyQualifiedMetadataName, out var candidate))
        {
            builder.Add(candidate);
        }

        foreach (var reference in compilation.References)
        {
            if (compilation.TryGetAssemblySymbol(reference, out var assemblySymbol) &&
                assemblySymbol.TryGetCachedTypeByMetadataName(fullyQualifiedMetadataName, out candidate))
            {
                builder.Add(candidate);
            }
        }

        return builder.ToImmutableAndClear();
    }

    private static INamedTypeSymbol? GetCachedTypeFromReferencedAssemblies(this Compilation compilation, string fullyQualifiedMetadataName)
        => compilation.TryGetCachedTypeFromReferencedAssemblies(fullyQualifiedMetadataName, out var result)
            ? result
            : null;

    private static bool TryGetCachedTypeFromReferencedAssemblies(
        this Compilation compilation,
        string fullyQualifiedMetadataName,
        [NotNullWhen(true)] out INamedTypeSymbol? result)
    {
        result = null;

        foreach (var reference in compilation.References)
        {
            if (compilation.TryGetAssemblySymbol(reference, out var assemblySymbol) &&
                assemblySymbol.TryGetCachedTypeByMetadataName(fullyQualifiedMetadataName, out var candidate))
            {
                if (result is not null)
                {
                    // We found the type in multiple assemblies. This means we can't reliably
                    // pick one, so return null.
                    result = null;
                    return false;
                }

                result = candidate;
            }
        }

        return result is not null;
    }
}
