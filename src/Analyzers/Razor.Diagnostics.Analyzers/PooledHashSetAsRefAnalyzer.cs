// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using static Razor.Diagnostics.Analyzers.Resources;

namespace Razor.Diagnostics.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PooledHashSetAsRefAnalyzer : AbstractAsRefAnalyzer
{
    internal static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.PooledHashSetAsRef,
        CreateLocalizableResourceString(nameof(PooledHashSetAsRefTitle)),
        CreateLocalizableResourceString(nameof(PooledHashSetAsRefMessage)),
        DiagnosticCategory.Reliability,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateLocalizableResourceString(nameof(PooledHashSetAsRefDescription)));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Rule];

    protected override string ExtensionsTypeName => WellKnownTypeNames.PooledHashSetExtensions;

    protected override Diagnostic CreateDiagnostic(IInvocationOperation invocation)
        => invocation.CreateDiagnostic(Rule);
}
