// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

internal static partial class SymbolCache
{
    public sealed partial class NamedTypeSymbolData
    {
        private sealed class IsViewComponentValue(
            INamedTypeSymbol symbol,
            INamedTypeSymbol viewComponentAttribute,
            INamedTypeSymbol? nonViewComponentAttribute)
        {
            public bool Value { get; } = ComputeValue(symbol, viewComponentAttribute, nonViewComponentAttribute);

            private readonly INamedTypeSymbol _viewComponentAttribute = viewComponentAttribute;
            private readonly INamedTypeSymbol? _nonViewComponentAttribute = nonViewComponentAttribute;

            private static bool ComputeValue(INamedTypeSymbol symbol, INamedTypeSymbol viewComponentAttribute, INamedTypeSymbol? nonViewComponentAttribute)
                => symbol.IsViableType() &&
                   !IsAttributeDefined(symbol, nonViewComponentAttribute) &&
                   (HasViewComponentSuffix(symbol) || IsAttributeDefined(symbol, viewComponentAttribute));

            private static bool HasViewComponentSuffix(INamedTypeSymbol symbol)
                => symbol.Name.EndsWith(ViewComponentTypes.ViewComponentSuffix, StringComparison.Ordinal);

            public bool HasSameAttributes(INamedTypeSymbol viewComponentAttribute, INamedTypeSymbol? nonViewComponentAttribute)
                => SymbolEqualityComparer.Default.Equals(_viewComponentAttribute, viewComponentAttribute) &&
                   SymbolEqualityComparer.Default.Equals(_nonViewComponentAttribute, nonViewComponentAttribute);

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
