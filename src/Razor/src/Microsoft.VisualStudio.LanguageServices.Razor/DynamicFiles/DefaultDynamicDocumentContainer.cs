﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Telemetry;

namespace Microsoft.VisualStudio.Razor.DynamicFiles;

// This types purpose is to serve as a non-Razor specific document delivery mechanism for Roslyn.
// Given a DocumentSnapshot this class allows the retrieval of a TextLoader for the generated C#
// and services to help map spans and excerpts to and from the top-level Razor document to behind
// the scenes C#.
internal sealed class DefaultDynamicDocumentContainer(IDocumentSnapshot documentSnapshot, ILoggerFactory loggerFactory) : IDynamicDocumentContainer
{
    private readonly IDocumentSnapshot _documentSnapshot = documentSnapshot ?? throw new ArgumentNullException(nameof(documentSnapshot));
    private RazorDocumentExcerptService? _excerptService;
    private RazorMappingService? _mappingService;

    public string FilePath => _documentSnapshot.FilePath;

    public bool SupportsDiagnostics => false;

    public void SetSupportsDiagnostics(bool enabled)
    {
        // This dynamic document container never supports diagnostics, so we don't allow enabling them.
    }

    public TextLoader GetTextLoader(string filePath)
        => new GeneratedDocumentTextLoader(_documentSnapshot, filePath);

    public IRazorDocumentExcerptServiceImplementation GetExcerptService()
        => _excerptService ?? InterlockedOperations.Initialize(ref _excerptService,
            new RazorDocumentExcerptService(_documentSnapshot, GetMappingService()));

    public IRazorDocumentPropertiesService GetDocumentPropertiesService()
    {
        // DocumentPropertiesServices are used to tell Roslyn to provide C# diagnostics for LSP provided documents to be shown
        // in the editor given a specific Language Server Client. Given this type is a container for DocumentSnapshots, we don't
        // have a Language Server to associate errors with or an open document to display those errors on. We return `null` to
        // opt out of those features.
        return null!;
    }

    public IRazorMappingService GetMappingService()
        => _mappingService ?? InterlockedOperations.Initialize(ref _mappingService,
            new RazorMappingService(_documentSnapshot, NoOpTelemetryReporter.Instance, loggerFactory));
}
