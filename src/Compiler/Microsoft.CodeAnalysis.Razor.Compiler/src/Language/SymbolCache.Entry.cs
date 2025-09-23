// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

internal static partial class SymbolCache
{
    private sealed class Entry
    {
        public SymbolData? SymbolData;
        public NamedTypeSymbolData? NamedTypeSymbolData;
        public AssemblySymbolData? AssemblySymbolData;
    }
}
