// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperCollector<T>(Compilation compilation, IAssemblySymbol? targetAssembly)
    where T : TagHelperCollector<T>
{
    private readonly Compilation _compilation = compilation;
    private readonly IAssemblySymbol? _targetAssembly = targetAssembly;

    protected abstract void Collect(IAssemblySymbol assemblySymbol, ICollection<TagHelperDescriptor> results);

    public void Collect(TagHelperDescriptorProviderContext context)
    {
        if (_targetAssembly is IAssemblySymbol targetAssembly)
        {
            Collect(targetAssembly, context.Results);
        }
        else
        {
            Collect(_compilation.Assembly, context.Results);

            foreach (var reference in _compilation.References)
            {
                if (_compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol referencedAssembly)
                {
                    if (TryCollectTagHelpersFromReference(context, referencedAssembly, out var tagHelpers))
                    {
                        foreach (var tagHelper in tagHelpers)
                        {
                            context.Results.Add(tagHelper);
                        }
                    }
                }
            }
        }
    }

    private bool TryCollectTagHelpersFromReference(
        TagHelperDescriptorProviderContext context,
        IAssemblySymbol referencedAssembly,
        [NotNullWhen(true)] out TagHelperDescriptor[]? result)
    {
        // Check to see if we already have tag helpers cached for this assembly
        // and use the cached versions if we do. Roslyn shares PE assembly symbols
        // across compilations, so this ensures that we don't produce new tag helpers
        // for the same assemblies over and over again.

        var assemblySymbolData = SymbolCache.GetAssemblySymbolData(referencedAssembly);
        if (!assemblySymbolData.MightContainTagHelpers)
        {
            result = null;
            return false;
        }

        var includeDocumentation = context.IncludeDocumentation;
        var excludeHidden = context.ExcludeHidden;

        result = assemblySymbolData.GetOrAddTagHelpers(typeof(T), includeDocumentation, excludeHidden, assembly =>
        {
            using var _ = ListPool<TagHelperDescriptor>.GetPooledObject(out var referenceTagHelpers);
            Collect(assembly, referenceTagHelpers);

            return referenceTagHelpers.ToArrayOrEmpty();
        });

        return result.Length > 0;
    }

    internal static void CollectTypes(
        IAssemblySymbol assemblySymbol,
        Func<INamedTypeSymbol, bool> predicate,
        ref PooledArrayBuilder<INamedTypeSymbol> results)
        => CollectTypes(assemblySymbol, predicate, includeNestedTypes: false, ref results);

    internal static void CollectTypes(
        IAssemblySymbol assemblySymbol,
        Func<INamedTypeSymbol, bool> predicate,
        bool includeNestedTypes,
        ref PooledArrayBuilder<INamedTypeSymbol> results)
    {
        using var stack = new PooledArrayBuilder<INamespaceOrTypeSymbol>();
        using var tempArray = new PooledArrayBuilder<INamespaceOrTypeSymbol>();

        stack.Push(assemblySymbol.GlobalNamespace);

        while (stack.Count > 0)
        {
            var namespaceOrTypeSymbol = stack.Pop();

            switch (namespaceOrTypeSymbol)
            {
                case INamespaceSymbol namespaceSymbol:
                    // Since INamespaceSymbol.GetMembers returns an IEnumerable we add
                    // them to a temporary array first so we can push them onto the stack
                    // in reverse order to process them in the correct order.
                    foreach (var member in namespaceSymbol.GetMembers())
                    {
                        tempArray.Add(member);
                    }

                    for (var i = tempArray.Count - 1; i >= 0; i--)
                    {
                        stack.Push(tempArray[i]);
                    }

                    tempArray.Clear();

                    break;

                case INamedTypeSymbol namedTypeSymbol:
                    if (predicate(namedTypeSymbol))
                    {
                        results.Add(namedTypeSymbol);
                    }

                    if (includeNestedTypes && namedTypeSymbol.DeclaredAccessibility == Accessibility.Public)
                    {
                        foreach (var member in namedTypeSymbol.GetTypeMembers())
                        {
                            stack.Push(member);
                        }
                    }

                    break;
            }
        }
    }
}
