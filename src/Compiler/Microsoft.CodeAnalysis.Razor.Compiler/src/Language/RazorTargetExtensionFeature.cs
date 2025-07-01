// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class RazorTargetExtensionFeature : RazorEngineFeatureBase, IRazorTargetExtensionFeature
{
    private readonly ImmutableArray<ICodeTargetExtension>.Builder _builder = ImmutableArray.CreateBuilder<ICodeTargetExtension>();
    private ImmutableArray<ICodeTargetExtension>? _targetExtensions;

    public ImmutableArray<ICodeTargetExtension> TargetExtensions
        => _targetExtensions ??= _builder.ToImmutable();

    public void AddTargetExtension(ICodeTargetExtension extension)
    {
        _builder.Add(extension);
        _targetExtensions = null;
    }
}
