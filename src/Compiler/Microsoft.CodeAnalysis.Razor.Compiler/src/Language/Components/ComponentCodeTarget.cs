// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal class ComponentCodeTarget : CodeTarget
{
    private readonly RazorCodeGenerationOptions _options;
    private readonly RazorLanguageVersion _version;

    public ComponentCodeTarget(RazorCodeGenerationOptions options, RazorLanguageVersion version, ImmutableArray<ICodeTargetExtension> extensions)
    {
        _options = options;
        _version = version;

        // Components provide some built-in target extensions that don't apply to
        // legacy documents.
        Extensions = [new ComponentTemplateTargetExtension(), .. extensions,];
    }

    public ImmutableArray<ICodeTargetExtension> Extensions { get; }

    public override IntermediateNodeWriter CreateNodeWriter()
    {
        return _options.DesignTime
            ? new ComponentDesignTimeNodeWriter(_version)
            : new ComponentRuntimeNodeWriter(_version);
    }

    public override TExtension GetExtension<TExtension>()
    {
        foreach (var extension in Extensions)
        {
            if (extension is TExtension match)
            {
                return match;
            }
        }

        return null!;
    }

    public override bool HasExtension<TExtension>()
    {
        foreach (var extension in Extensions)
        {
            if (extension is TExtension)
            {
                return true;
            }
        }

        return false;
    }
}
