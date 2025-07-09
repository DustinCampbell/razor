// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class TypeParameter(
    Content name,
    SourceSpan? nameSource = null,
    Content constraints = default,
    SourceSpan? constraintsSource = null)
{
    public Content Name { get; } = name;
    public SourceSpan? NameSource { get; } = nameSource;
    public Content Constraints { get; } = constraints;
    public SourceSpan? ConstraintsSource { get; } = constraintsSource;
}
