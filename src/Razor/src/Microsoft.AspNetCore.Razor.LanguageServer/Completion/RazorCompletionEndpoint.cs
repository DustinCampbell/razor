// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.EndpointContracts;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Telemetry;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Razor.Workspaces.Telemetry;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion;

[RazorLanguageServerEndpoint(Methods.TextDocumentCompletionName)]
internal class RazorCompletionEndpoint(
    CompletionListProvider completionListProvider,
    CompletionTriggerAndCommitCharacters completionTriggerAndCommitCharacters,
    ITelemetryReporter telemetryReporter,
    RazorLSPOptionsMonitor optionsMonitor,
    ILoggerFactory loggerFactory)
    : IRazorRequestHandler<CompletionParams, VSInternalCompletionList?>, ICapabilitiesProvider
{
    private readonly CompletionListProvider _completionListProvider = completionListProvider;
    private readonly CompletionTriggerAndCommitCharacters _completionTriggerAndCommitCharacters = completionTriggerAndCommitCharacters;
    private readonly ITelemetryReporter _telemetryReporter = telemetryReporter;
    private readonly RazorLSPOptionsMonitor _optionsMonitor = optionsMonitor;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<RazorCompletionEndpoint>();

    private VSInternalClientCapabilities? _clientCapabilities;

    public bool MutatesSolutionState => false;

    public void ApplyCapabilities(VSInternalServerCapabilities serverCapabilities, VSInternalClientCapabilities clientCapabilities)
    {
        _clientCapabilities = clientCapabilities;

        serverCapabilities.CompletionProvider = new CompletionOptions()
        {
            ResolveProvider = true,
            TriggerCharacters = _completionTriggerAndCommitCharacters.AllTriggerCharacters,
            AllCommitCharacters = CompletionTriggerAndCommitCharacters.AllCommitCharacters
        };
    }

    public TextDocumentIdentifier GetTextDocumentIdentifier(CompletionParams request)
    {
        return request.TextDocument;
    }

    public async Task<VSInternalCompletionList?> HandleRequestAsync(CompletionParams request, RazorRequestContext requestContext, CancellationToken cancellationToken)
    {
        if (request.Context is not VSInternalCompletionContext completionContext)
        {
            Debug.Fail("Completion context should never be null in practice");
            return null;
        }

        var documentContext = requestContext.DocumentContext;
        if (documentContext is null)
        {
            return null;
        }

        using var pooledWatch = StopwatchPool.GetPooledObject(out var watch);
        _logger.LogDebug($"{documentContext.FilePath}: Handling Razor completion request...");

        var autoShownCompletion = completionContext.InvokeKind != VSInternalCompletionInvokeKind.Explicit;
        var options = _optionsMonitor.CurrentValue;
        if (autoShownCompletion && !options.AutoShowCompletion)
        {
            _logger.LogDebug($"");
            return null;
        }

        var sourceText = await documentContext.GetSourceTextAsync(cancellationToken).ConfigureAwait(false);
        if (!sourceText.TryGetAbsoluteIndex(request.Position, out var hostDocumentIndex))
        {
            _logger.LogDebug($"{documentContext.FilePath}: Could not absolute index of ({request.Position.Line},{request.Position.Character})");
            return null;
        }

        var correlationId = Guid.NewGuid();
        using var _ = _telemetryReporter.TrackLspRequest(
            Methods.TextDocumentCompletionName,
            LanguageServerConstants.RazorLanguageServerName,
            TelemetryThresholds.CompletionRazorTelemetryThreshold,
            correlationId);

        var razorCompletionOptions = new RazorCompletionOptions(
            SnippetsSupported: true,
            AutoInsertAttributeQuotes: options.AutoInsertAttributeQuotes,
            CommitElementsWithSpace: options.CommitElementsWithSpace);
        var completionList = await _completionListProvider.GetCompletionListAsync(
            hostDocumentIndex,
            completionContext,
            documentContext,
            _clientCapabilities!,
            razorCompletionOptions,
            correlationId,
            cancellationToken).ConfigureAwait(false);

        _logger.LogDebug($"{documentContext.FilePath}: Razor completion request finished in {watch}.");

        return completionList;
    }
}
