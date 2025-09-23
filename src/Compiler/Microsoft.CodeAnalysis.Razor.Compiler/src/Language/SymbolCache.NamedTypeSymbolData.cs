// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

internal static partial class SymbolCache
{
    public sealed partial class NamedTypeSymbolData(INamedTypeSymbol symbol)
    {
        private IsViewComponentValue? _isViewComponent;

        public bool IsViewComponent(INamedTypeSymbol viewComponentAttribute, INamedTypeSymbol? nonViewComponentAttribute)
        {
            var result = _isViewComponent;

            if (result is null || !result.HasSameAttributes(viewComponentAttribute, nonViewComponentAttribute))
            {
                result = new IsViewComponentValue(symbol, viewComponentAttribute, nonViewComponentAttribute);
                _isViewComponent = result;
            }

            return result.Value;
        }
    }
}
