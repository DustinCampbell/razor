// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorCodeDocument
{
    public RazorSourceDocument Source { get; }
    public ImmutableArray<RazorSourceDocument> Imports { get; }
    public ItemCollection Items { get; }

    public RazorParserOptions ParserOptions { get; }
    public RazorCodeGenerationOptions CodeGenerationOptions { get; }

    private TagHelperDocumentContext? _tagHelperContext;
    private TagHelpersHolder? _tagHelpers;
    private ISet<TagHelperDescriptor>? _referencedTagHelpers;

    private RazorCodeDocument(
        RazorSourceDocument source,
        ImmutableArray<RazorSourceDocument> imports,
        RazorParserOptions? parserOptions,
        RazorCodeGenerationOptions? codeGenerationOptions)
    {
        Source = source;
        Imports = imports.NullToEmpty();

        ParserOptions = parserOptions ?? RazorParserOptions.Default;
        CodeGenerationOptions = codeGenerationOptions ?? RazorCodeGenerationOptions.Default;

        Items = new ItemCollection();
    }

    public static RazorCodeDocument Create(
        RazorSourceDocument source,
        RazorParserOptions? parserOptions = null,
        RazorCodeGenerationOptions? codeGenerationOptions = null)
    {
        ArgHelper.ThrowIfNull(source);

        return new RazorCodeDocument(source, imports: [], parserOptions, codeGenerationOptions);
    }

    public static RazorCodeDocument Create(
        RazorSourceDocument source,
        ImmutableArray<RazorSourceDocument> imports,
        RazorParserOptions? parserOptions = null,
        RazorCodeGenerationOptions? codeGenerationOptions = null)
    {
        ArgHelper.ThrowIfNull(source);

        return new RazorCodeDocument(source, imports, parserOptions, codeGenerationOptions);
    }


    internal TagHelperDocumentContext? GetTagHelperContext()
        => _tagHelperContext;

    internal void SetTagHelperContext(TagHelperDocumentContext context)
    {
        ArgHelper.ThrowIfNull(context);

        _tagHelperContext = context;
    }

    internal IReadOnlyList<TagHelperDescriptor>? GetTagHelpers()
        => _tagHelpers?.TagHelpers;

    internal void SetTagHelpers(IReadOnlyList<TagHelperDescriptor>? tagHelpers)
    {
        _tagHelpers = new TagHelpersHolder(tagHelpers);
    }

    private sealed class TagHelpersHolder(IReadOnlyList<TagHelperDescriptor>? tagHelpers)
    {
        public IReadOnlyList<TagHelperDescriptor>? TagHelpers { get; } = tagHelpers;
    }

    internal ISet<TagHelperDescriptor>? GetReferencedTagHelpers()
        => _referencedTagHelpers;

    internal void SetReferencedTagHelpers(ISet<TagHelperDescriptor> tagHelpers)
    {
        ArgHelper.ThrowIfNull(tagHelpers);

        _referencedTagHelpers = tagHelpers;
    }
}
