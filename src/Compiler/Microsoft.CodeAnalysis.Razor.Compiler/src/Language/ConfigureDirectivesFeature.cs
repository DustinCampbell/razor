// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class ConfigureDirectivesFeature : RazorEngineFeatureBase, IConfigureRazorParserOptionsFeature
{
    private const int MaxIndex = (int)RazorFileKinds.MaxValue - 1;

    public int Order => 100;

    private readonly ImmutableArray<DirectiveDescriptor>.Builder?[] _fileKindToDirectivesMap =
        new ImmutableArray<DirectiveDescriptor>.Builder?[MaxIndex + 1];

    public void AddDirective(DirectiveDescriptor directive, params ReadOnlySpan<RazorFileKind> fileKinds)
    {
        // To maintain backwards compatibility, FileKinds.Legacy is assumed when a file kind is not specified.
        if (fileKinds.IsEmpty)
        {
            fileKinds = [RazorFileKind.Legacy];
        }

        lock (_fileKindToDirectivesMap)
        {
            foreach (var fileKind in fileKinds)
            {
                if (!TryGetIndex(fileKind, out var index))
                {
                    continue;
                }

                var directives = _fileKindToDirectivesMap.GetOrSet(index, () => ImmutableArray.CreateBuilder<DirectiveDescriptor>());
                directives.Add(directive);
            }
        }
    }

    public ImmutableArray<DirectiveDescriptor> GetDirectives()
    {
        // To maintain backwards compatibility, FileKinds.Legacy is assumed when a file kind is not specified.
        return GetDirectives(RazorFileKind.Legacy);
    }

    public ImmutableArray<DirectiveDescriptor> GetDirectives(RazorFileKind fileKind)
    {
        if (!TryGetIndex(fileKind, out var index))
        {
            return [];
        }

        lock (_fileKindToDirectivesMap)
        {
            return _fileKindToDirectivesMap[index]?.ToImmutable() ?? [];
        }
    }

    private static bool TryGetIndex(RazorFileKind fileKind, out int index)
    {
        // Note: By using fileKind - 1, we filter out RazorFileKind.None because the index be -1.
        index = (int)fileKind - 1;

        if (index < 0 || index > MaxIndex)
        {
            index = 0;
            return false;
        }

        return true;
    }

    void IConfigureRazorParserOptionsFeature.Configure(RazorParserOptions.Builder builder)
    {
        var newKind = RazorFileKinds.FromString(builder.FileKind);
        builder.Directives = GetDirectives(newKind);
    }
}
