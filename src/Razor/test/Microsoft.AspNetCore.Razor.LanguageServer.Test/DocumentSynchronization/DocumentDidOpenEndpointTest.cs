﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.LanguageServer;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.LanguageServer.DocumentSynchronization;

public class DocumentDidOpenEndpointTest(ITestOutputHelper testOutput) : LanguageServerTestBase(testOutput)
{
    // This is more of an integration test to validate that all the pieces work together
    [Fact]
    public async Task Handle_DidOpenTextDocument_AddsDocument()
    {
        // Arrange
        var documentPath = "C:/path/to/document.cshtml";
        var projectService = new StrictMock<IRazorProjectService>();
        projectService
            .Setup(service => service.OpenDocumentAsync(It.IsAny<string>(), It.IsAny<SourceText>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback((string path, SourceText text, CancellationToken cancellationToken) =>
            {
                Assert.Equal("hello", text.ToString());
                Assert.Equal(documentPath, path);
            });
        var endpoint = new DocumentDidOpenEndpoint(projectService.Object);
        var request = new DidOpenTextDocumentParams()
        {
            TextDocument = new TextDocumentItem()
            {
                Text = "hello",
                DocumentUri = new(new Uri(documentPath)),
                Version = 1337,
            }
        };
        var requestContext = CreateRazorRequestContext(documentContext: null);

        // Act
        await endpoint.HandleNotificationAsync(request, requestContext, DisposalToken);

        // Assert
        projectService.VerifyAll();
    }
}
