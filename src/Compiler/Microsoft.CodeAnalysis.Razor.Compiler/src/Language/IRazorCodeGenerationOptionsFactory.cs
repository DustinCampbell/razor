﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

internal interface IRazorCodeGenerationOptionsFactory : IRazorEngineFeature
{
    RazorCodeGenerationOptions Create(string? fileKind = null, Action<RazorCodeGenerationOptionsBuilder>? configure = null);
}