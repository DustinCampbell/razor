// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class TagHelperObject<T> : IEquatable<T>
    where T : TagHelperObject<T>
{
    internal Checksum Checksum { get; }

    public ImmutableArray<RazorDiagnostic> Diagnostics { get; }

    public bool HasErrors
        => Diagnostics.Any(static d => d.Severity == RazorDiagnosticSeverity.Error);

    private protected TagHelperObject(Checksum checksum, ImmutableArray<RazorDiagnostic> diagnostics)
    {
        Checksum = checksum;
        Diagnostics = diagnostics.NullToEmpty();
    }

    public sealed override bool Equals(object? obj)
        => obj is T other &&
           Equals(other);

    public bool Equals(T? other)
        => other is not null &&
           Checksum.Equals(other.Checksum);

    public sealed override int GetHashCode()
        => Checksum.GetHashCode();
}
