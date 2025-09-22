// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

internal partial class SymbolCache
{
    public sealed partial class AssemblySymbolData(IAssemblySymbol symbol)
    {
        private readonly object _gate = new();

        private Dictionary<Type, TagHelperCache>? _tagHelperCacheMap;

        public bool MightContainTagHelpers { get; } = CalculateMightContainTagHelpers(symbol);

        private static bool CalculateMightContainTagHelpers(IAssemblySymbol assembly)
        {
            // In order to contain tag helpers, components, or anything else we might want to find,
            // the assembly must start with "Microsoft.AspNetCore." or reference an assembly that
            // starts with "Microsoft.AspNetCore."
            return assembly.Name.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal) ||
                    assembly.Modules.First().ReferencedAssemblies.Any(
                        a => a.Name.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal));
        }

        public TagHelperDescriptor[] GetOrAddTagHelpers(
            Type type,
            bool includeDocumentation,
            bool excludeHidden,
            Func<IAssemblySymbol, TagHelperDescriptor[]> factory)
        {
            if (!MightContainTagHelpers)
            {
                return [];
            }

            // Double-checked locking to avoid taking the lock unnecessarily.
            if (_tagHelperCacheMap is null || !_tagHelperCacheMap.TryGetValue(type, out var cache))
            {
                lock (_gate)
                {
                    _tagHelperCacheMap ??= [];

                    cache = _tagHelperCacheMap.GetOrAdd(type, static _ => new TagHelperCache());
                }
            }

            if (cache.TryGet(includeDocumentation, excludeHidden, out var tagHelpers))
            {
                return tagHelpers;
            }

            // We don't have a cached value, so we need to create one.
            tagHelpers = factory(symbol);

            return cache.Add(tagHelpers, includeDocumentation, excludeHidden);
        }
    }

    private sealed class TagHelperCache
    {
        private const int IncludeDocumentation = 1 << 0;
        private const int ExcludeHidden = 1 << 1;

        // The cache needs to be large enough to handle all combinations of options.
        private const int CacheSize = (IncludeDocumentation | ExcludeHidden) + 1;

        private readonly TagHelperDescriptor[]?[] _tagHelpers = new TagHelperDescriptor[CacheSize][];

        public bool TryGet(bool includeDocumentation, bool excludeHidden, [NotNullWhen(true)] out TagHelperDescriptor[]? tagHelpers)
        {
            var index = CalculateIndex(includeDocumentation, excludeHidden);

            tagHelpers = Volatile.Read(ref _tagHelpers[index]);
            return tagHelpers is not null;
        }

        public TagHelperDescriptor[] Add(TagHelperDescriptor[] tagHelpers, bool includeDocumentation, bool excludeHidden)
        {
            var index = CalculateIndex(includeDocumentation, excludeHidden);

            return InterlockedOperations.Initialize(ref _tagHelpers[index], tagHelpers);
        }

        private static int CalculateIndex(bool includeDocumentation, bool excludeHidden)
        {
            var index = 0;

            if (includeDocumentation)
            {
                index |= IncludeDocumentation;
            }

            if (excludeHidden)
            {
                index |= ExcludeHidden;
            }

            return index;
        }
    }
}
