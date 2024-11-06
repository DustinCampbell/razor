﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Protocol.CodeActions;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.Razor.CodeActions;

internal sealed class CodeActionsService(
    IDocumentMappingService documentMappingService,
    IEnumerable<IRazorCodeActionProvider> razorCodeActionProviders,
    IEnumerable<ICSharpCodeActionProvider> csharpCodeActionProviders,
    IEnumerable<IHtmlCodeActionProvider> htmlCodeActionProviders,
    LanguageServerFeatureOptions languageServerFeatureOptions) : ICodeActionsService
{
    private static readonly ImmutableHashSet<string> s_allAvailableCodeActionNames = GetAllAvailableCodeActionNames();

    private readonly IDocumentMappingService _documentMappingService = documentMappingService;
    private readonly IEnumerable<IRazorCodeActionProvider> _razorCodeActionProviders = razorCodeActionProviders;
    private readonly IEnumerable<ICSharpCodeActionProvider> _csharpCodeActionProviders = csharpCodeActionProviders;
    private readonly IEnumerable<IHtmlCodeActionProvider> _htmlCodeActionProviders = htmlCodeActionProviders;
    private readonly LanguageServerFeatureOptions _languageServerFeatureOptions = languageServerFeatureOptions;

    public async Task<SumType<Command, CodeAction>[]?> GetCodeActionsAsync(VSCodeActionParams request, IDocumentSnapshot documentSnapshot, RazorVSInternalCodeAction[] delegatedCodeActions, bool supportsCodeActionResolve, CancellationToken cancellationToken)
    {
        var razorCodeActionContext = await GenerateRazorCodeActionContextAsync(request, documentSnapshot, supportsCodeActionResolve, cancellationToken).ConfigureAwait(false);
        if (razorCodeActionContext is null)
        {
            return null;
        }

        delegatedCodeActions = razorCodeActionContext.LanguageKind switch
        {
            RazorLanguageKind.CSharp => ExtractCSharpCodeActionNamesFromData(delegatedCodeActions),
            RazorLanguageKind.Html => delegatedCodeActions,
            _ => []
        };

        var razorCodeActions = await GetRazorCodeActionsAsync(razorCodeActionContext, cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        var filteredCodeActions = await FilterDelegatedCodeActionsAsync(razorCodeActionContext, [.. delegatedCodeActions], cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        using var commandsOrCodeActions = new PooledArrayBuilder<SumType<Command, CodeAction>>();

        // Grouping the code actions causes VS to sort them into groups, rather than just alphabetically sorting them
        // by title. The latter is bad for us because it can put "Remove <div>" at the top in some locales, and our fully
        // qualify component code action at the bottom, depending on the users namespace.
        ConvertCodeActionsToSumType(request.TextDocument, razorCodeActions, "A-Razor");
        ConvertCodeActionsToSumType(request.TextDocument, filteredCodeActions, "B-Delegated");

        return commandsOrCodeActions.ToArray();

        void ConvertCodeActionsToSumType(VSTextDocumentIdentifier textDocument, ImmutableArray<RazorVSInternalCodeAction> codeActions, string groupName)
        {
            // We must cast the RazorCodeAction into a platform compliant code action
            // For VS (SupportsCodeActionResolve = true) this means just encapsulating the RazorCodeAction in the `CommandOrCodeAction` struct
            // For VS Code (SupportsCodeActionResolve = false) we must convert it into a CodeAction or Command before encapsulating in the `CommandOrCodeAction` struct.
            if (supportsCodeActionResolve)
            {
                foreach (var action in codeActions)
                {
                    // Make sure we honour the grouping that a delegated server may have created
                    action.Group = groupName + (action.Group ?? string.Empty);
                    commandsOrCodeActions.Add(action);
                }
            }
            else
            {
                foreach (var action in codeActions)
                {
                    commandsOrCodeActions.Add(action.AsVSCodeCommandOrCodeAction(textDocument));
                }
            }
        }
    }

    private async Task<RazorCodeActionContext?> GenerateRazorCodeActionContextAsync(
        VSCodeActionParams request,
        IDocumentSnapshot documentSnapshot,
        bool supportsCodeActionResolve,
        CancellationToken cancellationToken)
    {
        var codeDocument = await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
        if (codeDocument.IsUnsupported())
        {
            return null;
        }

        var sourceText = codeDocument.Source.Text;

        if (!sourceText.TryGetAbsoluteIndex(request.Range.Start, out var startLocation))
        {
            return null;
        }

        if (!sourceText.TryGetAbsoluteIndex(request.Range.End, out var endLocation))
        {
            endLocation = startLocation;
        }

        var languageKind = codeDocument.GetLanguageKind(startLocation, rightAssociative: false);
        var context = new RazorCodeActionContext(
            request,
            documentSnapshot,
            codeDocument,
            startLocation,
            endLocation,
            languageKind,
            sourceText,
            _languageServerFeatureOptions.SupportsFileManipulation,
            supportsCodeActionResolve);

        return context;
    }

    public async Task<VSCodeActionParams?> GetCSharpCodeActionsRequestAsync(IDocumentSnapshot documentSnapshot, VSCodeActionParams request, CancellationToken cancellationToken)
    {
        // For C# we have to map the ranges to the generated document
        var codeDocument = await documentSnapshot.GetGeneratedOutputAsync(forceDesignTimeGeneratedOutput: false, cancellationToken).ConfigureAwait(false);
        var csharpDocument = codeDocument.GetCSharpDocument();
        if (!_documentMappingService.TryMapToGeneratedDocumentRange(csharpDocument, request.Range, out var projectedRange))
        {
            return null;
        }

        var newContext = request.Context;
        if (request.Context is VSInternalCodeActionContext { SelectionRange: not null } vsContext &&
            _documentMappingService.TryMapToGeneratedDocumentRange(csharpDocument, vsContext.SelectionRange, out var selectionRange))
        {
            vsContext.SelectionRange = selectionRange;
            newContext = vsContext;
        }

        return new VSCodeActionParams
        {
            TextDocument = new VSTextDocumentIdentifier()
            {
                Uri = request.TextDocument.Uri,
                ProjectContext = request.TextDocument.ProjectContext
            },
            Context = newContext,
            Range = projectedRange,
        };
    }

    private RazorVSInternalCodeAction[] ExtractCSharpCodeActionNamesFromData(RazorVSInternalCodeAction[] codeActions)
    {
        using var actions = new PooledArrayBuilder<RazorVSInternalCodeAction>();

        foreach (var codeAction in codeActions)
        {
            if (codeAction.Data is not JsonElement jsonData ||
                !jsonData.TryGetProperty("CustomTags", out var value) ||
                value.Deserialize<string[]>() is not [..] tags)
            {
                continue;
            }

            foreach (var tag in tags)
            {
                if (s_allAvailableCodeActionNames.Contains(tag))
                {
                    codeAction.Name = tag;
                    break;
                }
            }

            if (string.IsNullOrEmpty(codeAction.Name))
            {
                continue;
            }

            actions.Add(codeAction);
        }

        return actions.ToArray();
    }

    private async Task<ImmutableArray<RazorVSInternalCodeAction>> FilterDelegatedCodeActionsAsync(
        RazorCodeActionContext context,
        ImmutableArray<RazorVSInternalCodeAction> codeActions,
        CancellationToken cancellationToken)
    {
        if (context.LanguageKind == RazorLanguageKind.Razor)
        {
            return [];
        }

        var providers = context.LanguageKind switch
        {
            RazorLanguageKind.CSharp => _csharpCodeActionProviders,
            RazorLanguageKind.Html => _htmlCodeActionProviders,
            _ => Assumed.Unreachable<IEnumerable<ICodeActionProvider>>()
        };

        cancellationToken.ThrowIfCancellationRequested();

        using var tasks = new PooledArrayBuilder<Task<ImmutableArray<RazorVSInternalCodeAction>>>();
        foreach (var provider in providers)
        {
            tasks.Add(provider.ProvideAsync(context, codeActions, cancellationToken));
        }

        return await ConsolidateCodeActionsFromProvidersAsync(tasks.ToImmutable(), cancellationToken).ConfigureAwait(false);
    }

    private async Task<ImmutableArray<RazorVSInternalCodeAction>> GetRazorCodeActionsAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var tasks = new PooledArrayBuilder<Task<ImmutableArray<RazorVSInternalCodeAction>>>();

        foreach (var provider in _razorCodeActionProviders)
        {
            tasks.Add(provider.ProvideAsync(context, cancellationToken));
        }

        return await ConsolidateCodeActionsFromProvidersAsync(tasks.ToImmutable(), cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ImmutableArray<RazorVSInternalCodeAction>> ConsolidateCodeActionsFromProvidersAsync(
        ImmutableArray<Task<ImmutableArray<RazorVSInternalCodeAction>>> tasks,
        CancellationToken cancellationToken)
    {
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        using var codeActions = new PooledArrayBuilder<RazorVSInternalCodeAction>();

        cancellationToken.ThrowIfCancellationRequested();

        foreach (var result in results)
        {
            codeActions.AddRange(result);
        }

        return codeActions.ToImmutable();
    }

    private static ImmutableHashSet<string> GetAllAvailableCodeActionNames()
    {
        using var _ = ArrayBuilderPool<string>.GetPooledObject(out var availableCodeActionNames);

        var refactoringProviderNames = typeof(RazorPredefinedCodeRefactoringProviderNames)
            .GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public)
            .Where(property => property.PropertyType == typeof(string))
            .Select(property => property.GetValue(null) as string)
            .WhereNotNull();
        var codeFixProviderNames = typeof(RazorPredefinedCodeFixProviderNames)
            .GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public)
            .Where(property => property.PropertyType == typeof(string))
            .Select(property => property.GetValue(null) as string)
            .WhereNotNull();

        availableCodeActionNames.AddRange(refactoringProviderNames);
        availableCodeActionNames.AddRange(codeFixProviderNames);
        availableCodeActionNames.Add(LanguageServerConstants.CodeActions.CodeActionFromVSCode);

        return availableCodeActionNames.ToImmutableHashSet();
    }
}