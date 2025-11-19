// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers;

internal sealed class AllowedChildTag
{
    public RazorSymbol Parent { get; }
    public string Name { get; }
    public ImmutableArray<RazorDiagnostic> Diagnostics { get; }

    public AllowedChildTag(RazorSymbol parent, ref readonly AllowedChildTagData data)
    {
        Parent = parent;
        Name = data.Name;
        Diagnostics = data.Diagnostics.ToImmutableArray();
    }

    public override string ToString()
        => Name;
}
