﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.EndpointContracts;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.AspNetCore.Razor.LanguageServer.DocumentSynchronization;

[RazorLanguageServerEndpoint(Methods.TextDocumentDidSaveName)]
internal class DocumentDidSaveEndpoint(ILoggerFactory loggerFactory)
    : IRazorNotificationHandler<DidSaveTextDocumentParams>, ITextDocumentIdentifierHandler<DidSaveTextDocumentParams, TextDocumentIdentifier>
{
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<DocumentDidSaveEndpoint>();

    public bool MutatesSolutionState => false;

    public TextDocumentIdentifier GetTextDocumentIdentifier(DidSaveTextDocumentParams request)
    {
        return request.TextDocument;
    }

    public Task HandleNotificationAsync(DidSaveTextDocumentParams request, RazorRequestContext requestContext, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Saved Document {request.TextDocument.DocumentUri.GetAbsoluteOrUNCPath()}");

        return Task.CompletedTask;
    }
}
