// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Telemetry;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.Razor.DynamicFiles;

// This type's purpose is to serve as a non-Razor specific document delivery mechanism for Roslyn.
// Given a DocumentSnapshot this class allows the retrieval of a TextLoader for the generated C#
// and services to help map spans and excerpts to and from the top-level Razor document to behind
// the scenes C#.
internal sealed class DefaultDynamicDocumentContainer : IDynamicDocumentContainer
{
    private readonly DocumentKey _documentKey;
    private readonly RazorCodeDocument _codeDocument;
    private readonly ILoggerFactory _loggerFactory;

    private RazorDocumentExcerptService? _excerptService;
    private RazorSpanMappingService? _spanMappingService;
    private RazorMappingService? _mappingService;

    public DefaultDynamicDocumentContainer(DocumentKey documentKey, RazorCodeDocument codeDocument, ILoggerFactory loggerFactory)
    {
        _documentKey = documentKey;
        _loggerFactory = loggerFactory;

        // The purpose of DefaultDynamicDocumentContainer is to provide a TextLoader and document services
        // that will be held by RazorDynamicFileInfo and rooted within the Roslyn workspace. To avoid
        // holding onto more memory than necessary, such as binding information, we create a new RazorCodeDocument
        // with much of that data stripped out.

        var newCodeDocument = RazorCodeDocument.Create(
            codeDocument.Source,
            codeDocument.ImportSources,
            codeDocument.ParserOptions,
            codeDocument.CodeGenerationOptions,
            codeDocument.CssScope);

        // RazorCSharpDocument references its RazorCodeDocument. So, we need create a new RazorCSharpDocument
        // that references the new RazorCodeDocument.
        var csharpDocument = codeDocument.GetRequiredCSharpDocument();
        var newCSharpDocument = new RazorCSharpDocument(
            newCodeDocument,
            csharpDocument.Text,
            csharpDocument.Options,
            csharpDocument.Diagnostics,
            csharpDocument.SourceMappings,
            csharpDocument.LinePragmas);

        newCodeDocument.SetCSharpDocument(newCSharpDocument);

        // If a C# SyntaxTree has already been parsed and stored in this RazorCodeDocument, we should
        // grab that too. This is ultimately needed by the RazorMappingService, which calls
        // RazorEditHelper.MapCSharpEditsAsync(...).
        if (codeDocument.TryGetCSharpSyntaxTree(out var syntaxTree))
        {
            newCodeDocument.SetCSharpSyntaxTree(syntaxTree);
        }

        // Finally, we'll need the RazorSyntaxTree as well.
        newCodeDocument.SetSyntaxTree(codeDocument.GetRequiredSyntaxTree());

        _codeDocument = newCodeDocument;
    }

    public string FilePath => _documentKey.FilePath;

    public bool SupportsDiagnostics => false;

    public void SetSupportsDiagnostics(bool enabled)
    {
        // This dynamic document container never supports diagnostics, so we don't allow enabling them.
    }

    public TextLoader GetTextLoader(string filePath)
        => new GeneratedDocumentTextLoader(_codeDocument, filePath);

    public RazorDocumentExcerptService GetExcerptService()
        => _excerptService ?? InterlockedOperations.Initialize(ref _excerptService,
            new RazorDocumentExcerptService(_codeDocument, GetSpanMappingService()));

    public RazorSpanMappingService GetSpanMappingService()
        => _spanMappingService ?? InterlockedOperations.Initialize(ref _spanMappingService,
            new RazorSpanMappingService(_codeDocument));

    public RazorMappingService GetMappingService()
        => _mappingService ?? InterlockedOperations.Initialize(ref _mappingService,
            new RazorMappingService(_codeDocument, NoOpTelemetryReporter.Instance, _loggerFactory));

    IRazorSpanMappingService IDynamicDocumentContainer.GetSpanMappingService()
        => GetSpanMappingService();

    IRazorMappingService IDynamicDocumentContainer.GetMappingService()
        => GetMappingService();

    IRazorDocumentExcerptServiceImplementation IDynamicDocumentContainer.GetExcerptService()
        => GetExcerptService();

    IRazorDocumentPropertiesService? IDynamicDocumentContainer.GetDocumentPropertiesService()
    {
        // DocumentPropertiesServices are used to tell Roslyn to provide C# diagnostics for LSP provided documents to be shown
        // in the editor given a specific Language Server Client. Given this type is a container for DocumentSnapshots, we don't
        // have a Language Server to associate errors with or an open document to display those errors on. We return `null` to
        // opt out of those features.
        return null;
    }
}
