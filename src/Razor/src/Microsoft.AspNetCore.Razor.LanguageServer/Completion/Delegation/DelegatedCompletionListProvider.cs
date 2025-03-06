// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.Hosting;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Protocol.Completion;
using Microsoft.CodeAnalysis.Razor.Workspaces.Completion;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion.Delegation;

internal class DelegatedCompletionListProvider(
    IDocumentMappingService documentMappingService,
    IClientConnection clientConnection,
    CompletionListCache completionListCache,
    CompletionTriggerAndCommitCharacters triggerAndCommitCharacters)
{
    private readonly IDocumentMappingService _documentMappingService = documentMappingService;
    private readonly IClientConnection _clientConnection = clientConnection;
    private readonly CompletionListCache _completionListCache = completionListCache;
    private readonly CompletionTriggerAndCommitCharacters _triggerAndCommitCharacters = triggerAndCommitCharacters;

    // virtual for tests
    protected virtual bool IsValidTrigger(CompletionContext context)
        => _triggerAndCommitCharacters.IsValidDelegationTrigger(context);

    // virtual for tests
    public virtual async Task<VSInternalCompletionList?> GetCompletionListAsync(
        int absoluteIndex,
        VSInternalCompletionContext completionContext,
        RazorCodeDocument codeDocument,
        DocumentContext documentContext,
        VSInternalClientCapabilities clientCapabilities,
        RazorCompletionOptions razorCompletionOptions,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        if (!IsValidTrigger(completionContext))
        {
            return null;
        }

        var positionInfo = _documentMappingService.GetPositionInfo(codeDocument, absoluteIndex);

        if (positionInfo.LanguageKind == RazorLanguageKind.Razor)
        {
            // Nothing to delegate to.
            return null;
        }

        TextEdit? provisionalTextEdit = null;

        if (completionContext.TryGetProvisionalCompletionInfo(positionInfo, codeDocument, _documentMappingService, out var provisionalCompletion))
        {
            provisionalTextEdit = provisionalCompletion.ProvisionalTextEdit;
            positionInfo = provisionalCompletion.DocumentPositionInfo;
        }

        if (!completionContext.ValidateOrGetUpdatedContext(positionInfo.LanguageKind, _triggerAndCommitCharacters, out var updatedContext))
        {
            return null;
        }

        completionContext = updatedContext;

        // It's a bit confusing, but we have two different "add snippets" options - one is a part of
        // RazorCompletionOptions and becomes a part of RazorCompletionContext and is used by
        // RazorCompletionFactsService, and the second one below that's used for delegated completion
        // Their values are not related in any way.
        var shouldIncludeDelegationSnippets = DelegatedCompletionHelper.ShouldIncludeSnippets(codeDocument, absoluteIndex);

        var delegatedParams = new DelegatedCompletionParams(
            documentContext.GetTextDocumentIdentifierAndVersion(),
            positionInfo.Position,
            positionInfo.LanguageKind,
            completionContext,
            provisionalTextEdit,
            shouldIncludeDelegationSnippets,
            correlationId);

        var delegatedResponse = await _clientConnection.SendRequestAsync<DelegatedCompletionParams, VSInternalCompletionList?>(
            LanguageServerConstants.RazorCompletionEndpointName,
            delegatedParams,
            cancellationToken).ConfigureAwait(false);

        var result = delegatedResponse;

        if (result?.Items is null)
        {
            // If we don't get a response from the delegated server, we have to make sure to return an incomplete completion
            // list. When a user is typing quickly, the delegated request from the first keystroke will fail to synchronize,
            // so if we return a "complete" list then the query won't re-query us for completion once the typing stops/slows
            // so we'd only ever return Razor completion items.
            result = new VSInternalCompletionList() { IsIncomplete = true, Items = [] };
        }
        else if (delegatedParams.ProjectedKind == RazorLanguageKind.CSharp)
        {
            DelegatedCompletionHelper.UpdateCSharpCompletionList(result, codeDocument, absoluteIndex, delegatedParams.ProjectedPosition);
        }
        else
        {
            DelegatedCompletionHelper.UpdateHtmlCompletionList(result, razorCompletionOptions);
        }

        var completionCapability = clientCapabilities?.TextDocument?.Completion as VSInternalCompletionSetting;
        var resolutionContext = new DelegatedCompletionResolutionContext(delegatedParams, result.Data);
        var resultId = _completionListCache.Add(result, resolutionContext);
        result.SetResultId(resultId, completionCapability);

        return result;
    }
}
