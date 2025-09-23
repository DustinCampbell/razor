// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.Language;

internal static partial class SymbolCache
{
    public sealed partial class SymbolData
    {
        private sealed class DisplayStringValues(ISymbol symbol)
        {
            private string? _defaultValue;
            private string? _fullNameTypeValue;
            private string? _globallyQualifiedFullNameTypeValue;

            public string Default
                => GetOrComputeDisplayString(ref _defaultValue);

            public string FullName
                => GetOrComputeDisplayString(ref _fullNameTypeValue, WellKnownSymbolDisplayFormats.FullNameTypeDisplayFormat);

            public string GloballyQualifiedFullName
                => GetOrComputeDisplayString(ref _globallyQualifiedFullNameTypeValue, WellKnownSymbolDisplayFormats.GloballyQualifiedFullNameTypeDisplayFormat);

            private string GetOrComputeDisplayString(ref string? field, SymbolDisplayFormat? format = null)
            {
#pragma warning disable RS0030 // Do not use banned APIs
                // This is the only location which should call ISymbol.ToDisplayString.
                // All callers of this method should cache the result into a field.
                return field ?? InterlockedOperations.Initialize(ref field, symbol.ToDisplayString(format));
#pragma warning restore RS0030 // Do not use banned APIs
            }
        }
    }
}
