// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Test.Common.Mef;
using Microsoft.AspNetCore.Razor.Test.Common.Workspaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;
using Nerdbank.Streams;
using StreamJsonRpc;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Test.Common.LanguageServer;

public sealed class CSharpTestLspServer : IAsyncDisposable
{
    private const string EditRangeSetting = "editRange";

    private readonly AdhocWorkspace _testWorkspace;
    private readonly ExportProvider _exportProvider;

    private readonly JsonRpc _clientRpc;
    private readonly JsonRpc _serverRpc;

    private readonly object _roslynLanguageServer;

    private readonly SystemTextJsonFormatter _clientMessageFormatter;
    private readonly SystemTextJsonFormatter _serverMessageFormatter;
    private readonly HeaderDelimitedMessageHandler _clientMessageHandler;
    private readonly HeaderDelimitedMessageHandler _serverMessageHandler;

    private readonly CancellationTokenSource _disposeTokenSource;

    private CSharpTestLspServer(
        AdhocWorkspace testWorkspace,
        ExportProvider exportProvider,
        VSInternalServerCapabilities serverCapabilities)
    {
        _testWorkspace = testWorkspace;
        _exportProvider = exportProvider;
        _disposeTokenSource = new();

        var (clientStream, serverStream) = FullDuplexStream.CreatePair();

        var languageServerFactory = exportProvider.GetExportedValue<AbstractRazorLanguageServerFactoryWrapper>();

        _serverMessageFormatter = CreateSystemTextJsonMessageFormatter(languageServerFactory);
        _serverMessageHandler = new HeaderDelimitedMessageHandler(serverStream, serverStream, _serverMessageFormatter);
        _serverRpc = new JsonRpc(_serverMessageHandler)
        {
            ExceptionStrategy = ExceptionProcessing.ISerializable,
        };

        _clientMessageFormatter = CreateSystemTextJsonMessageFormatter(languageServerFactory);
        _clientMessageHandler = new HeaderDelimitedMessageHandler(clientStream, clientStream, _clientMessageFormatter);
        _clientRpc = new JsonRpc(_clientMessageHandler)
        {
            ExceptionStrategy = ExceptionProcessing.ISerializable,
        };

        // Roslyn will call back to us to get configuration options when the server is initialized, so this is how we configure
        // what it options we need
        _clientRpc.AddLocalRpcTarget(new WorkspaceConfigurationHandler());

        _clientRpc.StartListening();

        var languageServerTarget = CreateLanguageServer(_serverRpc, _serverMessageFormatter.JsonSerializerOptions, testWorkspace, languageServerFactory, exportProvider, serverCapabilities);

        // This isn't ideal, but we need to pull the actual RoslynLanguageServer out of languageServerTarget
        // so that we can call ShutdownAsync and ExitAsync on it when dispos
        var languageServerField = languageServerTarget.GetType().GetField("_languageServer", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(languageServerField);

        var roslynLanguageServer = languageServerField.GetValue(languageServerTarget);
        Assert.NotNull(roslynLanguageServer);

        _roslynLanguageServer = roslynLanguageServer;

        static SystemTextJsonFormatter CreateSystemTextJsonMessageFormatter(AbstractRazorLanguageServerFactoryWrapper languageServerFactory)
        {
            var messageFormatter = new SystemTextJsonFormatter();

            // Roslyn has its own converters since it doesn't use MS.VS.LS.Protocol
            languageServerFactory.AddJsonConverters(messageFormatter.JsonSerializerOptions);

            return messageFormatter;
        }

        static IRazorLanguageServerTarget CreateLanguageServer(
            JsonRpc serverRpc,
            JsonSerializerOptions options,
            Workspace workspace,
            AbstractRazorLanguageServerFactoryWrapper languageServerFactory,
            ExportProvider exportProvider,
            VSInternalServerCapabilities serverCapabilities)
        {
            var capabilitiesProvider = new RazorTestCapabilitiesProvider(serverCapabilities, options);

            var registrationService = exportProvider.GetExportedValue<RazorTestWorkspaceRegistrationService>();
            registrationService.Register(workspace);

            var hostServices = workspace.Services.HostServices;
            var languageServer = languageServerFactory.CreateLanguageServer(serverRpc, options, capabilitiesProvider, hostServices);

            serverRpc.StartListening();
            return languageServer;
        }
    }

    internal static Task<CSharpTestLspServer> CreateAsync(
        SourceText csharpSourceText,
        Uri csharpDocumentUri,
        VSInternalServerCapabilities serverCapabilities,
        Action<VSInternalClientCapabilities>? capabilitiesUpdater,
        CancellationToken cancellationToken)
    {
        return CreateAsync(
            [(csharpDocumentUri, csharpSourceText)],
            serverCapabilities,
            multiTargetProject: true,
            capabilitiesUpdater,
            cancellationToken);
    }

    internal static async Task<CSharpTestLspServer> CreateAsync(
        IEnumerable<(Uri Uri, SourceText SourceText)> csharpFiles,
        VSInternalServerCapabilities serverCapabilities,
        bool multiTargetProject,
        Action<VSInternalClientCapabilities>? capabilitiesUpdater,
        CancellationToken cancellationToken)
    {
        var exportProvider = TestComposition.Roslyn.ExportProviderFactory.CreateExportProvider();

        var metadataReferences = await ReferenceAssemblies.Default.ResolveAsync(language: LanguageNames.CSharp, cancellationToken);
        metadataReferences = metadataReferences.Add(ReferenceUtil.AspNetLatestComponents);

        var workspace = CreateCSharpTestWorkspace(csharpFiles, exportProvider, metadataReferences, razorMappingService: null, multiTargetProject);

        var clientCapabilities = new VSInternalClientCapabilities
        {
            SupportsVisualStudioExtensions = true,
            TextDocument = new TextDocumentClientCapabilities
            {
                Completion = new VSInternalCompletionSetting
                {
                    CompletionListSetting = new()
                    {
                        ItemDefaults = [EditRangeSetting]
                    },
                    CompletionItem = new()
                    {
                        SnippetSupport = true
                    }
                },
                InlayHint = new()
                {
                    ResolveSupport = new InlayHintResolveSupportSetting { Properties = ["tooltip"] }
                }
            },
            SupportsDiagnosticRequests = true,
            Workspace = new()
            {
                Configuration = true
            }
        };

        capabilitiesUpdater?.Invoke(clientCapabilities);

        return await CreateAsync(workspace, exportProvider, clientCapabilities, serverCapabilities, cancellationToken);
    }

    internal static async Task<CSharpTestLspServer> CreateAsync(
        AdhocWorkspace workspace,
        ExportProvider exportProvider,
        ClientCapabilities clientCapabilities,
        VSInternalServerCapabilities serverCapabilities,
        CancellationToken cancellationToken)
    {
        var server = new CSharpTestLspServer(workspace, exportProvider, serverCapabilities);

        await server.ExecuteRequestAsync<InitializeParams, InitializeResult>(
            Methods.InitializeName,
            new InitializeParams
            {
                Capabilities = clientCapabilities,
            },
            cancellationToken);

        await server.ExecuteRequestAsync(Methods.InitializedName, new InitializedParams(), cancellationToken);

        return server;
    }

    private static AdhocWorkspace CreateCSharpTestWorkspace(
        IEnumerable<(Uri Uri, SourceText SourceText)> csharpFiles,
        ExportProvider exportProvider,
        ImmutableArray<MetadataReference> metadataReferences,
        IRazorMappingService? razorMappingService,
        bool multiTargetProject)
    {
        var workspace = TestWorkspace.CreateWithDiagnosticAnalyzers(exportProvider);

        // Add project and solution to workspace
        var projectInfoNet60 = ProjectInfo.Create(
            id: ProjectId.CreateNewId("TestProject (net6.0)"),
            version: VersionStamp.Default,
            name: "TestProject (net6.0)",
            assemblyName: "TestProject.dll",
            language: LanguageNames.CSharp,
            filePath: @"C:\TestSolution\TestProject.csproj",
            metadataReferences: metadataReferences).WithCompilationOutputInfo(new CompilationOutputInfo().WithAssemblyPath(@"C:\TestSolution\obj\TestProject.dll"));

        var projectInfoNet80 = ProjectInfo.Create(
            id: ProjectId.CreateNewId("TestProject (net8.0)"),
            version: VersionStamp.Default,
            name: "TestProject (net8.0)",
            assemblyName: "TestProject.dll",
            language: LanguageNames.CSharp,
            filePath: @"C:\TestSolution\TestProject.csproj",
            metadataReferences: metadataReferences);

        ProjectInfo[] projectInfos = multiTargetProject
            ? [projectInfoNet60, projectInfoNet80]
            : [projectInfoNet80];

        foreach (var projectInfo in projectInfos)
        {
            workspace.AddProject(projectInfo);
        }

        // Add document to workspace. We use an IVT method to create the DocumentInfo variable because there's
        // a special constructor in Roslyn that will help identify the document as belonging to Razor.
        var languageServerFactory = exportProvider.GetExportedValue<AbstractRazorLanguageServerFactoryWrapper>();

        var documentCount = 0;
        foreach (var (documentUri, csharpSourceText) in csharpFiles)
        {
            var documentFilePath = documentUri.GetDocumentFilePath();
            var textAndVersion = TextAndVersion.Create(csharpSourceText, VersionStamp.Default, documentFilePath);

            foreach (var projectInfo in projectInfos)
            {
                var documentInfo = languageServerFactory.CreateDocumentInfo(
                    id: DocumentId.CreateNewId(projectInfo.Id),
                    name: "TestDocument" + documentCount,
                    filePath: documentFilePath,
                    loader: TextLoader.From(textAndVersion),
                    razorDocumentServiceProvider: new TestRazorDocumentServiceProvider(razorMappingService));

                workspace.AddDocument(documentInfo);
            }

            documentCount++;
        }

        return workspace;
    }

    private record CSharpFile(Uri DocumentUri, SourceText CSharpSourceText);

    internal Task ExecuteRequestAsync<TRequest>(
        string methodName,
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : notnull
        => _clientRpc.InvokeWithParameterObjectAsync(
            methodName,
            request,
            cancellationToken);

    internal Task<TResponse> ExecuteRequestAsync<TRequest, TResponse>(
        string methodName,
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : notnull
        => _clientRpc.InvokeWithParameterObjectAsync<TResponse>(
            methodName,
            request,
            cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (_disposeTokenSource.IsCancellationRequested)
        {
            return;
        }

        _disposeTokenSource.Cancel();
        _disposeTokenSource.Dispose();

        // This is a bit of a hack, but we need to call ShutdownAsync and ExitAsync on the RoslynLanguageServer
        // so that it disconnects gracefully from _serverRpc. Otherwise, it'll fail if we dispose _serverRpc
        // which forcibly disconnects the JsonRpc from the RoslynLanguageServer.
        var shutdownAsyncMethod = _roslynLanguageServer.GetType()
            .GetMethod("ShutdownAsync", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(shutdownAsyncMethod);

        await (Task)shutdownAsyncMethod.Invoke(_roslynLanguageServer, parameters: [$"{nameof(CSharpTestLspServer)} shutting down"]).AssumeNotNull();

        var exitAsyncMethod = _roslynLanguageServer.GetType()
            .GetMethod("ExitAsync", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(exitAsyncMethod);

        await (Task)exitAsyncMethod.Invoke(_roslynLanguageServer, parameters: null).AssumeNotNull();

        _testWorkspace.Dispose();
        _exportProvider.Dispose();

        _clientRpc.Dispose();
        _clientMessageFormatter.Dispose();
        await _clientMessageHandler.DisposeAsync();

        _serverRpc.Dispose();
        _serverMessageFormatter.Dispose();
        await _serverMessageHandler.DisposeAsync();
    }

    #region Document Change Methods

    public async Task OpenDocumentAsync(Uri documentUri, string documentText)
    {
        var didOpenParams = new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                Text = documentText,
                Uri = documentUri
            }
        };

        await ExecuteRequestAsync<DidOpenTextDocumentParams, object>(Methods.TextDocumentDidOpenName, didOpenParams, _disposeTokenSource.Token);
    }

    internal async Task ReplaceTextAsync(Uri documentUri, params (LspRange Range, string Text)[] changes)
    {
        var didChangeParams = new DidChangeTextDocumentParams
        {
            TextDocument = new VersionedTextDocumentIdentifier
            {
                Uri = documentUri
            },
            ContentChanges = Array.ConvertAll(changes, ConvertToChangeEvent)
        };

        await ExecuteRequestAsync<DidChangeTextDocumentParams, object>(Methods.TextDocumentDidChangeName, didChangeParams, _disposeTokenSource.Token);

        static TextDocumentContentChangeEvent ConvertToChangeEvent((LspRange Range, string Text) change)
        {
            return new()
            {
                Text = change.Text,
                Range = change.Range
            };
        }
    }

    #endregion

    private class RazorTestCapabilitiesProvider(VSInternalServerCapabilities serverCapabilities, JsonSerializerOptions options) : IRazorTestCapabilitiesProvider
    {
        private readonly VSInternalServerCapabilities _serverCapabilities = serverCapabilities;
        private readonly JsonSerializerOptions _options = options;

        public string GetServerCapabilitiesJson(string clientCapabilitiesJson)
        {
            // To avoid exposing types from VS.LSP.Protocol across the Razor <-> Roslyn API boundary, and therefore
            // requiring us to agree on dependency versions, we use JSON as a transport mechanism.
            return JsonSerializer.Serialize(_serverCapabilities, _options);
        }
    }

    private class WorkspaceConfigurationHandler
    {
        [JsonRpcMethod(Methods.WorkspaceConfigurationName, UseSingleObjectParameterDeserialization = true)]
        public string[]? GetConfigurationOptions(ConfigurationParams configurationParams)
        {
            using var _ = ListPool<string>.GetPooledObject(out var values);
            values.SetCapacityIfLarger(configurationParams.Items.Length);

            foreach (var item in configurationParams.Items)
            {
                values.Add(item.Section switch
                {
                    "csharp|inlay_hints.dotnet_enable_inlay_hints_for_parameters" => "true",
                    "csharp|inlay_hints.dotnet_enable_inlay_hints_for_literal_parameters" => "true",
                    "csharp|inlay_hints.dotnet_enable_inlay_hints_for_indexer_parameters" => "true",
                    "csharp|inlay_hints.dotnet_enable_inlay_hints_for_object_creation_parameters" => "true",
                    "csharp|inlay_hints.dotnet_enable_inlay_hints_for_other_parameters" => "true",
                    "csharp|inlay_hints.dotnet_suppress_inlay_hints_for_parameters_that_differ_only_by_suffix" => "false",
                    "csharp|inlay_hints.dotnet_suppress_inlay_hints_for_parameters_that_match_method_intent" => "false",
                    "csharp|inlay_hints.dotnet_suppress_inlay_hints_for_parameters_that_match_argument_name" => "false",
                    "csharp|inlay_hints.csharp_enable_inlay_hints_for_types" => "true",
                    "csharp|inlay_hints.csharp_enable_inlay_hints_for_implicit_variable_types" => "true",
                    "csharp|inlay_hints.csharp_enable_inlay_hints_for_lambda_parameter_types" => "true",
                    "csharp|inlay_hints.csharp_enable_inlay_hints_for_implicit_object_creation" => "true",
                    _ => ""
                });
            }

            return values.ToArray();
        }
    }
}
