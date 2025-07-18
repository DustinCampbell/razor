// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class MethodParameter
{
    public required Content Name { get; init; }
    public required Content TypeName { get; init; }
    public ImmutableArray<Content> Modifiers { get; set => field = value.NullToEmpty(); } = [];
}
