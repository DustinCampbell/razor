// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Razor.Diagnostics.Analyzers;

public abstract class AbstractAsRefAnalyzer : DiagnosticAnalyzer
{
    protected abstract string ExtensionsTypeName { get; }

    protected abstract Diagnostic CreateDiagnostic(IInvocationOperation invocation);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(context =>
        {
            var extensionsType = context.Compilation.GetTypeByMetadataName(ExtensionsTypeName);
            if (extensionsType is null)
            {
                return;
            }

            var asRefMethod = (IMethodSymbol?)extensionsType.GetMembers("AsRef").SingleOrDefault();
            if (asRefMethod is null)
            {
                return;
            }

            context.RegisterOperationAction(context => AnalyzeInvocation(context, asRefMethod), OperationKind.Invocation);
        });
    }

    private void AnalyzeInvocation(OperationAnalysisContext context, IMethodSymbol asRefMethod)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var targetMethod = invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod;
        if (!SymbolEqualityComparer.Default.Equals(targetMethod.OriginalDefinition, asRefMethod))
        {
            return;
        }

        var instance = invocation.Instance ?? invocation.Arguments.FirstOrDefault()?.Value;
        if (instance is not ILocalReferenceOperation localReference)
        {
            context.ReportDiagnostic(CreateDiagnostic(invocation));
            return;
        }

        var declaration = invocation.SemanticModel!.GetOperation(localReference.Local.DeclaringSyntaxReferences.Single().GetSyntax(context.CancellationToken), context.CancellationToken);
        if (declaration is not { Parent: IVariableDeclarationOperation { Parent: IVariableDeclarationGroupOperation { Parent: IUsingOperation or IUsingDeclarationOperation } } })
        {
            context.ReportDiagnostic(CreateDiagnostic(invocation));
            return;
        }
    }
}
