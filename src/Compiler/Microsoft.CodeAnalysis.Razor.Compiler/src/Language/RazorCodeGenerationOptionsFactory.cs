﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class RazorCodeGenerationOptionsFactory : RazorEngineFeatureBase, IRazorCodeGenerationOptionsFactory
{
    private ImmutableArray<IConfigureRazorCodeGenerationOptionsFeature> _configureOptions;

    protected override void OnInitialized()
    {
        _configureOptions = Engine.GetFeatures<IConfigureRazorCodeGenerationOptionsFeature>();
    }

    public RazorCodeGenerationOptions Create(string? fileKind = null, Action<RazorCodeGenerationOptionsBuilder>? configure = null)
    {
        var builder = new DefaultRazorCodeGenerationOptionsBuilder(Engine.Configuration, fileKind);

        configure?.Invoke(builder);

        foreach (var option in _configureOptions)
        {
            option.Configure(builder);
        }

        return builder.Build();
    }
}