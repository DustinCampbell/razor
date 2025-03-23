// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorCodeDocument
{
    public RazorSourceDocument Source { get; }
    public ImmutableArray<RazorSourceDocument> Imports { get; }
    public ItemCollection Items { get; }

    public RazorParserOptions ParserOptions { get; }
    public RazorCodeGenerationOptions CodeGenerationOptions { get; }

    private RazorSyntaxTree? _preTagHelperSyntaxTree;
    private TagHelperDocumentContext? _tagHelperContext;

    public string FileKind => ParserOptions.FileKind;

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
}
