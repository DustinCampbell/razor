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

internal static partial class SymbolCache
{
    public sealed partial class AssemblySymbolData(IAssemblySymbol symbol)
    {
        private readonly object _gate = new();

        private Dictionary<Type, TagHelperCache>? _typeToTagHelperCacheMap;

        // Cache of type lookups by metadata name. We use Optional here to distinguish
        // between "not in the map" and "in the map with a null value".
        private Dictionary<string, Optional<INamedTypeSymbol?>>? _nameToTypeMap;

        public bool MightContainTagHelpers { get; } = CalculateMightContainTagHelpers(symbol);

        private static bool CalculateMightContainTagHelpers(IAssemblySymbol assembly)
        {
            // In order to contain tag helpers, components, or anything else we might want to find,
            // the assembly must start with "Microsoft.AspNetCore." or reference an assembly that
            // starts with "Microsoft.AspNetCore."
            return assembly.Name.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal) ||
                   assembly.Modules.First().ReferencedAssemblies.Any(
                       static a => a.Name.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal));
        }

        public TagHelperDescriptor[] GetOrComputeTagHelpers(
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
            if (_typeToTagHelperCacheMap is null || !_typeToTagHelperCacheMap.TryGetValue(type, out var cache))
            {
                lock (_gate)
                {
                    _typeToTagHelperCacheMap ??= [];

                    cache = _typeToTagHelperCacheMap.GetOrAdd(type, static _ => new TagHelperCache());
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

        public INamedTypeSymbol? GetTypeByMetadataName(string fullyQualifiedMetadataName)
        {
            if (_nameToTypeMap is null || !_nameToTypeMap.TryGetValue(fullyQualifiedMetadataName, out var result))
            {
                var realResult = symbol.GetTypeByMetadataName(fullyQualifiedMetadataName);

                lock (_gate)
                {
                    _nameToTypeMap ??= new(StringComparer.Ordinal);

                    result = _nameToTypeMap.GetOrAdd(fullyQualifiedMetadataName, new Optional<INamedTypeSymbol?>(realResult));
                }
            }

            return result.Value;
        }
    }
}
