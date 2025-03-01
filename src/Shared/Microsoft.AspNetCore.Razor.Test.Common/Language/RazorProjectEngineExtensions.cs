// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public static class RazorProjectEngineExtensions
{
    private const RazorSourceCodeKind DefaultSourceCodeKind = RazorSourceCodeKind.Legacy;

    public static RazorCodeDocument CreateEmptyCodeDocument(
        this RazorProjectEngine projectEngine)
        => projectEngine.CreateEmptyCodeDocumentCore();

    public static RazorCodeDocument CreateEmptyCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceCodeKind sourceCodeKind)
        => projectEngine.CreateEmptyCodeDocumentCore(sourceCodeKind);

    public static RazorCodeDocument CreateEmptyCodeDocument(
        this RazorProjectEngine projectEngine,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateEmptyCodeDocumentCore(importSources: importSources);

    public static RazorCodeDocument CreateEmptyCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceCodeKind sourceCodeKind,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateEmptyCodeDocumentCore(sourceCodeKind, importSources);

    public static RazorCodeDocument CreateEmptyCodeDocument(
        this RazorProjectEngine projectEngine,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
        => projectEngine.CreateEmptyCodeDocumentCore(tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateEmptyCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceCodeKind sourceCodeKind,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
        => projectEngine.CreateEmptyCodeDocumentCore(sourceCodeKind, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateEmptyCodeDocument(
        this RazorProjectEngine projectEngine,
        ImmutableArray<RazorSourceDocument> importSources,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
        => projectEngine.CreateEmptyCodeDocumentCore(importSources: importSources, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateEmptyCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceCodeKind sourceCodeKind,
        ImmutableArray<RazorSourceDocument> importSources,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
        => projectEngine.CreateEmptyCodeDocumentCore(sourceCodeKind, importSources, tagHelpers);

    private static RazorCodeDocument CreateEmptyCodeDocumentCore(
        this RazorProjectEngine projectEngine,
        RazorSourceCodeKind sourceCodeKind = DefaultSourceCodeKind,
        ImmutableArray<RazorSourceDocument> importSources = default,
        IReadOnlyList<TagHelperDescriptor>? tagHelpers = null)
        => projectEngine.CreateCodeDocumentCore(string.Empty, sourceCodeKind, importSources, tagHelpers);

    public static RazorCodeDocument CreateEmptyDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine)
        => projectEngine.CreateEmptyDesignTimeCodeDocumentCore();

    public static RazorCodeDocument CreateEmptyDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceCodeKind sourceCodeKind)
        => projectEngine.CreateEmptyDesignTimeCodeDocumentCore(sourceCodeKind);

    public static RazorCodeDocument CreateEmptyDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateEmptyDesignTimeCodeDocumentCore(importSources: importSources);

    public static RazorCodeDocument CreateEmptyDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceCodeKind sourceCodeKind,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateEmptyDesignTimeCodeDocumentCore(sourceCodeKind, importSources);

    public static RazorCodeDocument CreateEmptyDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
        => projectEngine.CreateEmptyDesignTimeCodeDocumentCore(tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateEmptyDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceCodeKind sourceCodeKind,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
        => projectEngine.CreateEmptyDesignTimeCodeDocumentCore(sourceCodeKind, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateEmptyDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        ImmutableArray<RazorSourceDocument> importSources,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
        => projectEngine.CreateEmptyDesignTimeCodeDocumentCore(importSources: importSources, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateEmptyDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceCodeKind sourceCodeKind,
        ImmutableArray<RazorSourceDocument> importSources,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
        => projectEngine.CreateEmptyDesignTimeCodeDocumentCore(sourceCodeKind, importSources, tagHelpers);

    private static RazorCodeDocument CreateEmptyDesignTimeCodeDocumentCore(
        this RazorProjectEngine projectEngine,
        RazorSourceCodeKind sourceCodeKind = DefaultSourceCodeKind,
        ImmutableArray<RazorSourceDocument> importSources = default,
        IReadOnlyList<TagHelperDescriptor>? tagHelpers = null)
        => projectEngine.CreateDesignTimeCodeDocumentCore(string.Empty, sourceCodeKind, importSources, tagHelpers);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        string content)
        => projectEngine.CreateCodeDocumentCore(content);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        RazorSourceCodeKind sourceCodeKind)
        => projectEngine.CreateCodeDocumentCore(content, sourceCodeKind);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateCodeDocumentCore(content, importSources: importSources);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        RazorSourceCodeKind sourceCodeKind,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateCodeDocumentCore(content, sourceCodeKind, importSources);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
        => projectEngine.CreateCodeDocumentCore(content, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        RazorSourceCodeKind sourceCodeKind,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
        => projectEngine.CreateCodeDocumentCore(content, sourceCodeKind, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        ImmutableArray<RazorSourceDocument> importSources,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
        => projectEngine.CreateCodeDocumentCore(content, importSources: importSources, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        RazorSourceCodeKind sourceCodeKind,
        ImmutableArray<RazorSourceDocument> importSources,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
        => projectEngine.CreateCodeDocumentCore(content, sourceCodeKind, importSources, tagHelpers);

    private static RazorCodeDocument CreateCodeDocumentCore(
        this RazorProjectEngine projectEngine,
        string content,
        RazorSourceCodeKind sourceCodeKind = DefaultSourceCodeKind,
        ImmutableArray<RazorSourceDocument> importSources = default,
        IReadOnlyList<TagHelperDescriptor>? tagHelpers = null)
    {
        var source = TestRazorSourceDocument.Create(content);

        return projectEngine.CreateCodeDocument(source, sourceCodeKind, importSources, tagHelpers, cssScope: null);
    }

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        string content)
        => projectEngine.CreateDesignTimeCodeDocumentCore(content);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        IReadOnlyList<TagHelperDescriptor>? tagHelpers)
        => projectEngine.CreateDesignTimeCodeDocumentCore(content, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        RazorSourceCodeKind sourceCodeKind)
        => projectEngine.CreateDesignTimeCodeDocumentCore(content, sourceCodeKind);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        RazorSourceCodeKind sourceCodeKind,
        IReadOnlyList<TagHelperDescriptor>? tagHelpers)
        => projectEngine.CreateDesignTimeCodeDocumentCore(content, sourceCodeKind, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateDesignTimeCodeDocumentCore(content, importSources: importSources);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        ImmutableArray<RazorSourceDocument> importSources,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
        => projectEngine.CreateDesignTimeCodeDocumentCore(content, importSources: importSources, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        RazorSourceCodeKind sourceCodeKind,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateDesignTimeCodeDocumentCore(content, sourceCodeKind, importSources);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        string content,
        RazorSourceCodeKind sourceCodeKind,
        ImmutableArray<RazorSourceDocument> importSources,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
        => projectEngine.CreateDesignTimeCodeDocumentCore(content, sourceCodeKind, importSources, tagHelpers);

    private static RazorCodeDocument CreateDesignTimeCodeDocumentCore(
        this RazorProjectEngine projectEngine,
        string content,
        RazorSourceCodeKind sourceCodeKind = DefaultSourceCodeKind,
        ImmutableArray<RazorSourceDocument> importSources = default,
        IReadOnlyList<TagHelperDescriptor>? tagHelpers = null)
    {
        var source = TestRazorSourceDocument.Create(content);

        return projectEngine.CreateDesignTimeCodeDocument(source, sourceCodeKind, importSources, tagHelpers);
    }

    public static void ExecutePhasesThrough<T>(
        this RazorProjectEngine projectEngine,
        RazorCodeDocument codeDocument)
        where T : IRazorEnginePhase
    {
        foreach (var phase in projectEngine.Engine.Phases)
        {
            phase.Execute(codeDocument);

            if (phase is T)
            {
                break;
            }
        }
    }

    public static void ExecutePhase<T>(
        this RazorProjectEngine projectEngine,
        RazorCodeDocument codeDocument)
        where T : IRazorEnginePhase, new()
    {
        projectEngine.ExecutePhase<T>(codeDocument, () => new());
    }

    public static void ExecutePhase<T>(
        this RazorProjectEngine projectEngine,
        RazorCodeDocument codeDocument,
        Func<T> phaseFactory)
        where T : IRazorEnginePhase
    {
        var pass = phaseFactory();
        pass.Initialize(projectEngine.Engine);

        pass.Execute(codeDocument);
    }

    public static void ExecutePass<T>(
        this RazorProjectEngine projectEngine,
        RazorCodeDocument codeDocument)
        where T : IntermediateNodePassBase, new()
    {
        var documentNode = codeDocument.GetDocumentIntermediateNode();
        Assert.NotNull(documentNode);

        projectEngine.ExecutePass<T>(codeDocument, documentNode);
    }

    public static void ExecutePass<T>(
        this RazorProjectEngine projectEngine,
        RazorCodeDocument codeDocument,
        Func<T> passFactory)
        where T : IntermediateNodePassBase
    {
        var documentNode = codeDocument.GetDocumentIntermediateNode();
        Assert.NotNull(documentNode);

        projectEngine.ExecutePass<T>(codeDocument, documentNode, passFactory);
    }

    public static void ExecutePass<T>(
        this RazorProjectEngine projectEngine,
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode documentNode)
        where T : IntermediateNodePassBase, new()
        => projectEngine.ExecutePass<T>(codeDocument, documentNode, () => new());

    public static void ExecutePass<T>(
        this RazorProjectEngine projectEngine,
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode documentNode,
        Func<T> passFactory)
        where T : IntermediateNodePassBase
    {
        var pass = passFactory();
        pass.Initialize(projectEngine.Engine);

        pass.Execute(codeDocument, documentNode);
    }
}
