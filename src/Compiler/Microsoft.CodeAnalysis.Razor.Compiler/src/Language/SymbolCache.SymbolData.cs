// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

internal static partial class SymbolCache
{
    public sealed partial class SymbolData(ISymbol symbol)
    {
        private DisplayStringValues? _displayStrings;

        private DisplayStringValues GetDisplayStrings()
            => _displayStrings ?? InterlockedOperations.Initialize(ref _displayStrings, new DisplayStringValues(symbol));

        public string GetDefaultDisplayString()
            => GetDisplayStrings().Default;

        public string GetFullName()
            => GetDisplayStrings().FullName;

        public string GetGloballyQualifiedFullName()
            => GetDisplayStrings().GloballyQualifiedFullName;
    }
}
