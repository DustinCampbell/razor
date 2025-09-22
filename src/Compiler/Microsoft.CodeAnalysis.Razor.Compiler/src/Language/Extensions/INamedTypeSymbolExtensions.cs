// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

internal static partial class INamedTypeSymbolExtensions
{
    public static bool IsTagHelper(this INamedTypeSymbol symbol, INamedTypeSymbol tagHelperType)
        => symbol.IsViableType() &&
           symbol.AllInterfaces.Contains(tagHelperType);

    public static bool IsViewComponent(this INamedTypeSymbol symbol, INamedTypeSymbol viewComponentAttribute, INamedTypeSymbol? nonViewComponentAttribute)
        => symbol.IsViableType() &&
           SymbolCache.GetNamedTypeSymbolData(symbol).IsViewComponent(viewComponentAttribute, nonViewComponentAttribute);

    public static bool IsViableType(this INamedTypeSymbol symbol)
        => symbol.TypeKind != TypeKind.Error &&
           symbol.DeclaredAccessibility == Accessibility.Public &&
           !symbol.IsAbstract &&
           !symbol.IsGenericType;
}
