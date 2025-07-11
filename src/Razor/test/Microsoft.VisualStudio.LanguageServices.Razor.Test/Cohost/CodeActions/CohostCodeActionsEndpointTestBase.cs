﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Protocol.CodeActions;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.CodeAnalysis.Razor.Utilities;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Remote.Razor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Razor.Settings;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public abstract class CohostCodeActionsEndpointTestBase(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    private protected async Task VerifyCodeActionAsync(
        TestCode input,
        string? expected,
        string codeActionName,
        int childActionIndex = 0,
        RazorFileKind? fileKind = null,
        (string filePath, string contents)[]? additionalFiles = null,
        (Uri fileUri, string contents)[]? additionalExpectedFiles = null)
    {
        var document = CreateRazorDocument(input, fileKind, additionalFiles);

        var codeAction = await VerifyCodeActionRequestAsync(document, input, codeActionName, childActionIndex, expectOffer: expected is not null);

        if (codeAction is null)
        {
            Assert.Null(expected);
            return;
        }

        var workspaceEdit = codeAction.Data is null
            ? codeAction.Edit.AssumeNotNull()
            : await ResolveCodeActionAsync(document, codeAction);

        await VerifyCodeActionResultAsync(document, workspaceEdit, expected, additionalExpectedFiles);
    }

    private protected TextDocument CreateRazorDocument(TestCode input, RazorFileKind? fileKind = null, (string filePath, string contents)[]? additionalFiles = null)
    {
        var fileSystem = (RemoteFileSystem)OOPExportProvider.GetExportedValue<IFileSystem>();
        fileSystem.GetTestAccessor().SetFileSystem(new TestFileSystem(additionalFiles));

        UpdateClientLSPInitializationOptions(options =>
        {
            options.ClientCapabilities.TextDocument = new()
            {
                CodeAction = new()
                {
                    ResolveSupport = new()
                }
            };

            return options;
        });

        return CreateProjectAndRazorDocument(input.Text, fileKind, createSeparateRemoteAndLocalWorkspaces: true, additionalFiles: additionalFiles);
    }

    private async Task<CodeAction?> VerifyCodeActionRequestAsync(TextDocument document, TestCode input, string codeActionName, int childActionIndex, bool expectOffer)
    {
        var result = await GetCodeActionsAsync(document, input);
        if (result is null)
        {
            return null;
        }

        var codeActionToRun = (VSInternalCodeAction?)result.SingleOrDefault(e => ((RazorVSInternalCodeAction)e.Value!).Name == codeActionName).Value;

        if (!expectOffer)
        {
            Assert.Null(codeActionToRun);
            return null;
        }

        AssertEx.NotNull(codeActionToRun, $"""
            Could not find code action with name '{codeActionName}'.

            Available:
                {string.Join(Environment.NewLine + "    ", result.Select(e => ((RazorVSInternalCodeAction)e.Value!).Name))}
            """);

        if (codeActionToRun.Children?.Length > 0)
        {
            codeActionToRun = codeActionToRun.Children[childActionIndex];
        }

        Assert.NotNull(codeActionToRun);
        return codeActionToRun;
    }

    private protected async Task<SumType<Command, CodeAction>[]?> GetCodeActionsAsync(TextDocument document, TestCode input)
    {
        var requestInvoker = new TestHtmlRequestInvoker();
        var endpoint = new CohostCodeActionsEndpoint(IncompatibleProjectService, RemoteServiceInvoker, ClientCapabilitiesService, requestInvoker, NoOpTelemetryReporter.Instance);
        var inputText = await document.GetTextAsync(DisposalToken);

        using var diagnostics = new PooledArrayBuilder<LspDiagnostic>();
        foreach (var (code, spans) in input.NamedSpans)
        {
            if (code.Length == 0)
            {
                continue;
            }

            foreach (var diagnosticSpan in spans)
            {
                diagnostics.Add(new LspDiagnostic
                {
                    Code = code,
                    Range = inputText.GetRange(diagnosticSpan)
                });
            }
        }

        var range = input.HasSpans
            ? inputText.GetRange(input.Span)
            : inputText.GetRange(input.Position, input.Position);

        var request = new VSCodeActionParams
        {
            TextDocument = new VSTextDocumentIdentifier { DocumentUri = document.CreateDocumentUri() },
            Range = range,
            Context = new VSInternalCodeActionContext() { Diagnostics = diagnostics.ToArray() }
        };

        if (input.TryGetNamedSpans("selection", out var selectionSpans))
        {
            // Simulate VS range vs selection range
            request.Context.SelectionRange = inputText.GetRange(selectionSpans.Single());
        }

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(document, request, DisposalToken);
        if (result is null)
        {
            return null;
        }

        Assert.NotEmpty(result);
        return result;
    }

    private async Task VerifyCodeActionResultAsync(TextDocument document, WorkspaceEdit workspaceEdit, string? expected, (Uri fileUri, string contents)[]? additionalExpectedFiles = null)
    {
        var solution = document.Project.Solution;
        var validated = false;

        if (workspaceEdit.DocumentChanges?.Value is SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[] sumTypeArray)
        {
            using var builder = new PooledArrayBuilder<TextDocumentEdit>();
            foreach (var sumType in sumTypeArray)
            {
                if (sumType.Value is CreateFile createFile)
                {
                    validated = true;
                    Assert.Single(additionalExpectedFiles.AssumeNotNull(), f => f.fileUri == createFile.DocumentUri.GetRequiredParsedUri());
                    var documentId = DocumentId.CreateNewId(document.Project.Id);
                    var filePath = createFile.DocumentUri.GetRequiredParsedUri().GetDocumentFilePath();
                    var documentInfo = DocumentInfo.Create(documentId, filePath, filePath: filePath);
                    solution = solution.AddDocument(documentInfo);
                }
            }
        }

        if (workspaceEdit.TryGetTextDocumentEdits(out var documentEdits))
        {
            foreach (var edit in documentEdits)
            {
                var textDocument = solution.GetTextDocuments(edit.TextDocument.DocumentUri.GetRequiredParsedUri()).First();
                var text = await textDocument.GetTextAsync(DisposalToken).ConfigureAwait(false);
                if (textDocument is Document)
                {
                    solution = solution.WithDocumentText(textDocument.Id, text.WithChanges(edit.Edits.Select(e => text.GetTextChange((TextEdit)e))));
                }
                else
                {
                    solution = solution.WithAdditionalDocumentText(textDocument.Id, text.WithChanges(edit.Edits.Select(e => text.GetTextChange((TextEdit)e))));
                }
            }

            if (additionalExpectedFiles is not null)
            {
                foreach (var (uri, contents) in additionalExpectedFiles)
                {
                    var additionalDocument = solution.GetTextDocuments(uri).First();
                    var text = await additionalDocument.GetTextAsync(DisposalToken).ConfigureAwait(false);
                    AssertEx.EqualOrDiff(contents, text.ToString());
                }
            }

            validated = true;
            var actual = await solution.GetAdditionalDocument(document.Id).AssumeNotNull().GetTextAsync(DisposalToken).ConfigureAwait(false);
            AssertEx.EqualOrDiff(expected, actual.ToString());
        }

        Assert.True(validated, "Test did not validate anything. Code action response type is presumably not supported.");
    }

    private async Task<WorkspaceEdit> ResolveCodeActionAsync(CodeAnalysis.TextDocument document, CodeAction codeAction)
    {
        var requestInvoker = new TestHtmlRequestInvoker();
        var clientSettingsManager = new ClientSettingsManager(changeTriggers: []);
        var endpoint = new CohostCodeActionsResolveEndpoint(IncompatibleProjectService, RemoteServiceInvoker, ClientCapabilitiesService, clientSettingsManager, requestInvoker);

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(document, codeAction, DisposalToken);

        Assert.NotNull(result?.Edit);
        return result.Edit;
    }

    private class TestFileSystem((string filePath, string contents)[]? files) : IFileSystem
    {
        public bool FileExists(string filePath)
            => files?.Any(f => FilePathNormalizingComparer.Instance.Equals(f.filePath, filePath)) ?? false;

        public string ReadFile(string filePath)
            => files.AssumeNotNull().Single(f => FilePathNormalizingComparer.Instance.Equals(f.filePath, filePath)).contents;

        public Stream OpenReadStream(string filePath)
            => new MemoryStream(Encoding.UTF8.GetBytes(ReadFile(filePath)));

        public IEnumerable<string> GetDirectories(string workspaceDirectory)
            => throw new NotImplementedException();

        public IEnumerable<string> GetFiles(string workspaceDirectory, string searchPattern, SearchOption searchOption)
            => throw new NotImplementedException();
    }
}
