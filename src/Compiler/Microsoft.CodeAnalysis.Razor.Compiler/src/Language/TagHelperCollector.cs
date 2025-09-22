// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperCollector<T>(Compilation compilation, ISymbol? targetSymbol)
    where T : TagHelperCollector<T>
{
    private readonly Compilation _compilation = compilation;
    private readonly ISymbol? _targetSymbol = targetSymbol;

    protected abstract void Collect(ISymbol symbol, ICollection<TagHelperDescriptor> results);

    public void Collect(TagHelperDescriptorProviderContext context)
    {
        if (_targetSymbol is not null)
        {
            Collect(_targetSymbol, context.Results);
        }
        else
        {
            Collect(_compilation.Assembly.GlobalNamespace, context.Results);

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
            Collect(assembly.GlobalNamespace, referenceTagHelpers);

            return referenceTagHelpers.ToArrayOrEmpty();
        });

        return result.Length > 0;
    }
}
