// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
}
