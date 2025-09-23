// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

internal static class IAssemblySymbolExtensions
{
    public static INamedTypeSymbol? GetCachedTypeByMetadataName(this IAssemblySymbol assemblySymbol, string fullyQualifiedTypeName)
        => SymbolCache.GetAssemblySymbolData(assemblySymbol).GetTypeByMetadataName(fullyQualifiedTypeName);

    public static bool TryGetCachedTypeByMetadataName(
        this IAssemblySymbol assemblySymbol,
        string fullyQualifiedMetadataName,
        [NotNullWhen(true)] out INamedTypeSymbol? result)
    {
        result = assemblySymbol.GetCachedTypeByMetadataName(fullyQualifiedMetadataName);
        return result is not null;
    }
}
