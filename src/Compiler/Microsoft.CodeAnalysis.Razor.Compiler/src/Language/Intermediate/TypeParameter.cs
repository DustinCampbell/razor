// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class TypeParameter
{
    public required Content Name { get; init; }
    public SourceSpan? NameSource { get; init; }
    public Content Constraints { get; init; }
    public SourceSpan? ConstraintsSource { get; init; }
}
