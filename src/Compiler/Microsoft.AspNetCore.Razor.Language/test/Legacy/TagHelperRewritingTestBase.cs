﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class TagHelperRewritingTestBase() : ParserTestBase(layer: TestProject.Layer.Compiler)
{
    internal void RunParseTreeRewriterTest(string documentContent, ImmutableArray<string> tagNames = default)
    {
        var tagHelpers = CreateTagHelpers(tagNames);

        EvaluateData(tagHelpers, documentContent);
    }

    internal static TagHelperCollection CreateTagHelpers(ImmutableArray<string> tagNames)
    {
        if (tagNames.IsDefaultOrEmpty)
        {
            return [];
        }

        return TagHelperCollection.Build(tagNames, static (ref builder, tagNames) =>
        {
            foreach (var tagName in tagNames)
            {
                var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper(tagName + "taghelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName(tagName))
                    .Build();
                builder.Add(tagHelper);
            }
        });
    }

    internal void EvaluateData(
        TagHelperCollection tagHelpers,
        string documentContent,
        string? tagHelperPrefix = null,
        RazorLanguageVersion? languageVersion = null,
        RazorFileKind? fileKind = null,
        Action<RazorParserOptions.Builder>? configureParserOptions = null)
    {
        var syntaxTree = ParseDocument(languageVersion, documentContent, directives: default, fileKind: fileKind, configureParserOptions: configureParserOptions);

        var binder = new TagHelperBinder(tagHelperPrefix, tagHelpers);
        var rewrittenTree = TagHelperParseTreeRewriter.Rewrite(syntaxTree, binder);

        Assert.Equal(syntaxTree.Root.Width, rewrittenTree.Root.Width);

        BaselineTest(rewrittenTree);
    }
}
