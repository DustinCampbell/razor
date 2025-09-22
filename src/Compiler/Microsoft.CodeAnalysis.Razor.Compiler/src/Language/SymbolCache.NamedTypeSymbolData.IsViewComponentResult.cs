// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

internal partial class SymbolCache
{
    public sealed partial class NamedTypeSymbolData
    {
        private sealed class IsViewComponentResult
        {
            public bool Value { get; }
            public INamedTypeSymbol ViewComponentAttribute { get; }
            public INamedTypeSymbol? NonViewComponentAttribute { get; }

            public IsViewComponentResult(INamedTypeSymbol symbol, INamedTypeSymbol viewComponentAttribute, INamedTypeSymbol? nonViewComponentAttribute)
            {
                ViewComponentAttribute = viewComponentAttribute;
                NonViewComponentAttribute = nonViewComponentAttribute;

                Value = symbol.IsViableType() &&
                        !IsAttributeDefined(symbol, nonViewComponentAttribute) &&
                        (HasViewComponentSuffix(symbol) || IsAttributeDefined(symbol, viewComponentAttribute));
            }

            private static bool HasViewComponentSuffix(INamedTypeSymbol symbol)
                => symbol.Name.EndsWith(ViewComponentTypes.ViewComponentSuffix, StringComparison.Ordinal);

            public bool IsMatchingCache(INamedTypeSymbol viewComponentAttribute, INamedTypeSymbol? nonViewComponentAttribute)
                => SymbolEqualityComparer.Default.Equals(ViewComponentAttribute, viewComponentAttribute) &&
                   SymbolEqualityComparer.Default.Equals(NonViewComponentAttribute, nonViewComponentAttribute);

            private static bool IsAttributeDefined(INamedTypeSymbol type, INamedTypeSymbol? queryAttribute)
            {
                if (queryAttribute == null)
                {
                    return false;
                }

                var currentType = type;
                while (currentType != null)
                {
                    foreach (var attribute in currentType.GetAttributes())
                    {
                        if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, queryAttribute))
                        {
                            return true;
                        }
                    }

                    currentType = currentType.BaseType;
                }

                return false;
            }
        }
    }
}
