// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

public static class RazorProjectEngineExtensions
{
    private const RazorSourceCodeKind DefaultSourceCodeKind = RazorSourceCodeKind.Legacy;

    private static RazorSourceCodeKind ComputeFileKindIfNeeded(RazorSourceCodeKind sourceCodeKind, RazorSourceDocument source)
    {
        if (sourceCodeKind is not RazorSourceCodeKind.None)
        {
            return sourceCodeKind;
        }

        if (source.FilePath is string filePath &&
            SourceCodeFileKinds.TryGetSourceCodeKind(filePath, out var result))
        {
            return result;
        }

        return DefaultSourceCodeKind;
    }

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source)
        => projectEngine.CreateCodeDocumentCore(source);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        RazorSourceCodeKind sourceCodeKind)
        => projectEngine.CreateCodeDocumentCore(source, sourceCodeKind);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateCodeDocumentCore(source, importSources: importSources);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        RazorSourceCodeKind sourceCodeKind,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateCodeDocumentCore(source, sourceCodeKind, importSources);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
        => projectEngine.CreateCodeDocumentCore(source, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        RazorSourceCodeKind sourceCodeKind,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
        => projectEngine.CreateCodeDocumentCore(source, sourceCodeKind, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        ImmutableArray<RazorSourceDocument> importSources,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
        => projectEngine.CreateCodeDocumentCore(source, importSources: importSources, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        RazorSourceCodeKind sourceCodeKind,
        ImmutableArray<RazorSourceDocument> importSources,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
        => projectEngine.CreateCodeDocumentCore(source, sourceCodeKind, importSources, tagHelpers);

    private static RazorCodeDocument CreateCodeDocumentCore(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        RazorSourceCodeKind sourceCodeKind = RazorSourceCodeKind.None,
        ImmutableArray<RazorSourceDocument> importSources = default,
        IReadOnlyList<TagHelperDescriptor>? tagHelpers = null)
        => projectEngine.CreateCodeDocument(source, ComputeFileKindIfNeeded(sourceCodeKind, source), importSources, tagHelpers, cssScope: null);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source)
        => projectEngine.CreateDesignTimeCodeDocumentCore(source);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        RazorSourceCodeKind sourceCodeKind)
        => projectEngine.CreateDesignTimeCodeDocumentCore(source, sourceCodeKind);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateDesignTimeCodeDocumentCore(source, importSources: importSources);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        RazorSourceCodeKind sourceCodeKind,
        ImmutableArray<RazorSourceDocument> importSources)
        => projectEngine.CreateDesignTimeCodeDocumentCore(source, sourceCodeKind, importSources);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
        => projectEngine.CreateDesignTimeCodeDocumentCore(source, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        RazorSourceCodeKind sourceCodeKind,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
        => projectEngine.CreateDesignTimeCodeDocumentCore(source, sourceCodeKind, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        ImmutableArray<RazorSourceDocument> importSources,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
        => projectEngine.CreateDesignTimeCodeDocumentCore(source, importSources: importSources, tagHelpers: tagHelpers);

    public static RazorCodeDocument CreateDesignTimeCodeDocument(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        RazorSourceCodeKind sourceCodeKind,
        ImmutableArray<RazorSourceDocument> importSources,
        IReadOnlyList<TagHelperDescriptor> tagHelpers)
        => projectEngine.CreateDesignTimeCodeDocumentCore(source, sourceCodeKind, importSources, tagHelpers);

    private static RazorCodeDocument CreateDesignTimeCodeDocumentCore(
        this RazorProjectEngine projectEngine,
        RazorSourceDocument source,
        RazorSourceCodeKind sourceCodeKind = RazorSourceCodeKind.None,
        ImmutableArray<RazorSourceDocument> importSources = default,
        IReadOnlyList<TagHelperDescriptor>? tagHelpers = null)
        => projectEngine.CreateDesignTimeCodeDocument(source, ComputeFileKindIfNeeded(sourceCodeKind, source), importSources, tagHelpers);
}
