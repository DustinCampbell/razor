// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using static Razor.Diagnostics.Analyzers.Resources;

namespace Razor.Diagnostics.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InterpolatedStringToContentAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor Rule = new(
    DiagnosticIds.InterpolatedStringToContent,
    CreateLocalizableResourceString(nameof(InterpolatedStringToContentTitle)),
    CreateLocalizableResourceString(nameof(InterpolatedStringToContentMessage)),
    DiagnosticCategory.Reliability,
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true,
    description: CreateLocalizableResourceString(nameof(InterpolatedStringToContentDescription)));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(context =>
        {
            var contentType = context.Compilation.GetTypeByMetadataName(WellKnownTypeNames.Content);
            if (contentType is null)
            {
                return;
            }

            context.RegisterOperationAction(context => AnalyzeInvocation(context, contentType), OperationKind.Conversion);
        });
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol contentType)
    {
        var conversion = (IConversionOperation)context.Operation;
        if (!SymbolEqualityComparer.Default.Equals(conversion.Type, contentType))
        {
            return;
        }

        if (!conversion.IsImplicit || conversion.Operand is not IInterpolatedStringOperation)
        {
            return;
        }

        context.ReportDiagnostic(conversion.CreateDiagnostic(Rule));
    }
}
