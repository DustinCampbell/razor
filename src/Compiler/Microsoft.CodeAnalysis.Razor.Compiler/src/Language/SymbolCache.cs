// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

internal static partial class SymbolCache
{
    private static readonly ConditionalWeakTable<ISymbol, Entry> s_table = new();

    private static Entry GetCacheEntry(ISymbol symbol)
        => s_table.GetValue(symbol, static s => new Entry());

    public static SymbolData GetSymbolData(ISymbol symbol)
    {
        var entry = GetCacheEntry(symbol);

        return InterlockedOperations.Initialize(ref entry.SymbolData, new(symbol));
    }

    public static AssemblySymbolData GetAssemblySymbolData(IAssemblySymbol symbol)
    {
        var entry = GetCacheEntry(symbol);

        return InterlockedOperations.Initialize(ref entry.AssemblySymbolData, new(symbol));
    }

    public static NamedTypeSymbolData GetNamedTypeSymbolData(INamedTypeSymbol symbol)
    {
        var entry = GetCacheEntry(symbol);

        return InterlockedOperations.Initialize(ref entry.NamedTypeSymbolData, new(symbol));
    }
}
