// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.CodeAnalysis.Razor;

public sealed class DefaultTagHelperDescriptorProvider : TagHelperDescriptorProviderBase
{
    public override void Execute(TagHelperDescriptorProviderContext context)
    {
        ArgHelper.ThrowIfNull(context);

        var compilation = context.Compilation;

        var tagHelperTypeSymbol = compilation.GetTypeByMetadataName(TagHelperTypes.ITagHelper);
        if (tagHelperTypeSymbol == null || tagHelperTypeSymbol.TypeKind == TypeKind.Error)
        {
            // Could not find attributes we care about in the compilation. Nothing to do.
            return;
        }

        var targetAssembly = context.TargetAssembly;
        var factory = new DefaultTagHelperDescriptorFactory(context.IncludeDocumentation, context.ExcludeHidden);
        var collector = new Collector(compilation, targetAssembly, factory, tagHelperTypeSymbol);
        collector.Collect(context);
    }

    private class Collector(
        Compilation compilation, IAssemblySymbol? targetAssembly, DefaultTagHelperDescriptorFactory factory, INamedTypeSymbol tagHelperTypeSymbol)
        : TagHelperCollector<Collector>(compilation, targetAssembly)
    {
        private readonly DefaultTagHelperDescriptorFactory _factory = factory;
        private readonly INamedTypeSymbol _tagHelperTypeSymbol = tagHelperTypeSymbol;

        private bool IsTagHelper(INamedTypeSymbol symbol)
            => symbol.TypeKind != TypeKind.Error &&
               symbol.DeclaredAccessibility == Accessibility.Public &&
               !symbol.IsAbstract &&
               !symbol.IsGenericType &&
               symbol.AllInterfaces.Contains(_tagHelperTypeSymbol);

        protected override void Collect(IAssemblySymbol assemblySymbol, ICollection<TagHelperDescriptor> results)
        {
            using var types = new PooledArrayBuilder<INamedTypeSymbol>();
            CollectTypes(assemblySymbol, IsTagHelper, ref types.AsRef());

            foreach (var type in types)
            {
                var descriptor = _factory.CreateDescriptor(type);

                if (descriptor != null)
                {
                    results.Add(descriptor);
                }
            }
        }
    }
}
