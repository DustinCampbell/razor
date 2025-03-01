// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class ConfigureDirectivesFeature : RazorEngineFeatureBase, IConfigureRazorParserOptionsFeature
{
    private const int MaxIndex = (int)SourceCodeFileKinds.MaxValue - 1;

    public int Order => 100;

    private readonly ImmutableArray<DirectiveDescriptor>.Builder?[] _sourceCodeKindToDirectivesMap =
        new ImmutableArray<DirectiveDescriptor>.Builder?[MaxIndex + 1];

    public void AddDirective(DirectiveDescriptor directive, params ReadOnlySpan<RazorSourceCodeKind> sourceCodeKinds)
    {
        // To maintain backwards compatibility, RazorSourceCodeKind.Legacy is assumed when a source code kind is not specified.
        if (sourceCodeKinds.IsEmpty)
        {
            sourceCodeKinds = [RazorSourceCodeKind.Legacy];
        }

        lock (_sourceCodeKindToDirectivesMap)
        {
            foreach (var sourceCodeKind in sourceCodeKinds)
            {
                if (!TryGetIndex(sourceCodeKind, out var index))
                {
                    continue;
                }

                var directives = _sourceCodeKindToDirectivesMap.GetOrSet(index, () => ImmutableArray.CreateBuilder<DirectiveDescriptor>());
                directives.Add(directive);
            }
        }
    }

    public ImmutableArray<DirectiveDescriptor> GetDirectives()
    {
        // To maintain backwards compatibility, RazorSourceCodeKind.Legacy is assumed when a source code kind is not specified.
        return GetDirectives(RazorSourceCodeKind.Legacy);
    }

    public ImmutableArray<DirectiveDescriptor> GetDirectives(RazorSourceCodeKind sourceCodeKind)
    {
        if (!TryGetIndex(sourceCodeKind, out var index))
        {
            return [];
        }

        lock (_sourceCodeKindToDirectivesMap)
        {
            return _sourceCodeKindToDirectivesMap[index]?.ToImmutable() ?? [];
        }
    }

    private static bool TryGetIndex(RazorSourceCodeKind sourceCodeKind, out int index)
    {
        // Note: By using sourceCodeKind - 1, we filter out RazorSourceCodeKind.None because the index be -1.
        index = (int)sourceCodeKind - 1;

        if (index < 0 || index > MaxIndex)
        {
            index = 0;
            return false;
        }

        return true;
    }

    void IConfigureRazorParserOptionsFeature.Configure(RazorParserOptions.Builder builder)
    {
        builder.Directives = GetDirectives(builder.SourceCodeKind);
    }
}
