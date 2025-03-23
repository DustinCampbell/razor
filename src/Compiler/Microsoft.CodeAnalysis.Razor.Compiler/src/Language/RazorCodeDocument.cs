// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorCodeDocument
{
    public RazorSourceDocument Source { get; }
    public ImmutableArray<RazorSourceDocument> ImportSources { get; }
    public ItemCollection Items { get; }

    public RazorParserOptions ParserOptions { get; }
    public RazorCodeGenerationOptions CodeGenerationOptions { get; }

    public string? CssScope { get; }

    private RazorCSharpDocument? _csharpDocument;
    private DocumentIntermediateNode? _documentNode;
    private ImmutableArray<RazorSyntaxTree> _importSyntaxTrees;
    private RazorSyntaxTree? _preTagHelperSyntaxTree;
    private ISet<TagHelperDescriptor>? _referencedTagHelpers;
    private RazorSyntaxTree? _syntaxTree;
    private TagHelperDocumentContext? _tagHelperContext;
    private IReadOnlyList<TagHelperDescriptor>? _tagHelpers;

    public RazorLanguageVersion LanguageVersion => ParserOptions.LanguageVersion;
    public string FileKind => ParserOptions.FileKind;

    private RazorCodeDocument(
        RazorSourceDocument source,
        ImmutableArray<RazorSourceDocument> importSources,
        RazorParserOptions? parserOptions,
        RazorCodeGenerationOptions? codeGenerationOptions,
        string? cssScope)
    {
        Source = source;
        ImportSources = importSources.NullToEmpty();

        ParserOptions = parserOptions ?? RazorParserOptions.Default;
        CodeGenerationOptions = codeGenerationOptions ?? RazorCodeGenerationOptions.Default;
        CssScope = cssScope;

        Items = new ItemCollection();
    }

    public static RazorCodeDocument Create(
        RazorSourceDocument source,
        RazorParserOptions? parserOptions = null,
        RazorCodeGenerationOptions? codeGenerationOptions = null,
        string? cssScope = null)
    {
        ArgHelper.ThrowIfNull(source);

        return new(source, importSources: [], parserOptions, codeGenerationOptions, cssScope);
    }

    public static RazorCodeDocument Create(
        RazorSourceDocument source,
        ImmutableArray<RazorSourceDocument> importSources,
        RazorParserOptions? parserOptions = null,
        RazorCodeGenerationOptions? codeGenerationOptions = null,
        string? cssScope = null)
    {
        ArgHelper.ThrowIfNull(source);

        return new(source, importSources, parserOptions, codeGenerationOptions, cssScope);
    }

    public bool TryGetCSharpDocument([NotNullWhen(true)] out RazorCSharpDocument? csharpDocument)
    {
        csharpDocument = _csharpDocument;
        return csharpDocument != null;
    }

    internal void SetCSharpDocument(RazorCSharpDocument csharpDocument)
    {
        ArgHelper.ThrowIfNull(csharpDocument);

        _csharpDocument = csharpDocument;
    }

    internal bool TryGetDocumentIntermediateNode([NotNullWhen(true)] out DocumentIntermediateNode? documentNode)
    {
        documentNode = _documentNode;
        return documentNode != null;
    }

    internal void SetDocumentIntermediateNode(DocumentIntermediateNode documentNode)
    {
        ArgHelper.ThrowIfNull(documentNode);

        _documentNode = documentNode;
    }

    public bool TryGetImportSyntaxTrees(out ImmutableArray<RazorSyntaxTree> importSyntaxTrees)
    {
        importSyntaxTrees = _importSyntaxTrees;
        return !importSyntaxTrees.IsDefault;
    }

    public void SetImportSyntaxTrees(ImmutableArray<RazorSyntaxTree> importSyntaxTrees)
    {
        if (importSyntaxTrees.IsDefault)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(importSyntaxTrees));
        }

        _importSyntaxTrees = importSyntaxTrees;
    }

    internal bool TryGetPreTagHelperSyntaxTree([NotNullWhen(true)] out RazorSyntaxTree? syntaxTree)
    {
        syntaxTree = _preTagHelperSyntaxTree;
        return syntaxTree != null;
    }

    internal void SetPreTagHelperSyntaxTree(RazorSyntaxTree syntaxTree)
    {
        ArgHelper.ThrowIfNull(syntaxTree);

        _preTagHelperSyntaxTree = syntaxTree;
    }

    internal bool TryGetReferencedTagHelpers([NotNullWhen(true)] out ISet<TagHelperDescriptor>? referencedTagHelpers)
    {
        referencedTagHelpers = _referencedTagHelpers;
        return referencedTagHelpers != null;
    }

    internal void SetReferencedTagHelpers(ISet<TagHelperDescriptor> referencedTagHelpers)
    {
        ArgHelper.ThrowIfNull(referencedTagHelpers);

        _referencedTagHelpers = referencedTagHelpers;
    }

    internal bool TryGetSyntaxTree([NotNullWhen(true)] out RazorSyntaxTree? syntaxTree)
    {
        syntaxTree = _syntaxTree;
        return syntaxTree != null;
    }

    internal void SetSyntaxTree(RazorSyntaxTree syntaxTree)
    {
        ArgHelper.ThrowIfNull(syntaxTree);

        _syntaxTree = syntaxTree;
    }

    internal bool TryGetTagHelperContext([NotNullWhen(true)] out TagHelperDocumentContext? context)
    {
        context = _tagHelperContext;
        return context != null;
    }

    internal void SetTagHelperContext(TagHelperDocumentContext context)
    {
        ArgHelper.ThrowIfNull(context);

        _tagHelperContext = context;
    }

    internal bool TryGetTagHelpers([NotNullWhen(true)] out IReadOnlyList<TagHelperDescriptor>? tagHelpers)
    {
        tagHelpers = _tagHelpers;
        return tagHelpers != null;
    }

    internal void SetTagHelpers(IReadOnlyList<TagHelperDescriptor>? tagHelpers)
    {
        _tagHelpers = tagHelpers;
    }
}
