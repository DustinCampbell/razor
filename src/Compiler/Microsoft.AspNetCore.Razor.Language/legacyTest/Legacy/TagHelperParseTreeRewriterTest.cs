// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class TagHelperParseTreeRewriterTest : TagHelperRewritingTestBase
{
    private static KeyValuePair<string, string> KVP(string key, string value)
        => KeyValuePair.Create(key, value);

    public static TheoryData<string, ImmutableArray<KeyValuePair<string, string>>> GetAttributeNameValuePairsData
    {
        get
        {
            var csharp = TagHelperParseTreeRewriter.Rewriter.InvalidAttributeValueMarker.ToString();

            // documentContent, expectedPairs
            return new()
            {
                { "<a>", [] },
                { "<a @{ } href='~/home'>", [KVP("href", "~/home")] },
                { "<a href=\"@true\">", [KVP("href", csharp)] },
                { "<a href=\"prefix @true suffix\">", [KVP("href", $"prefix{csharp} suffix")] },
                { "<a href=~/home>", [KVP("href", "~/home")] },
                { "<a href=~/home @{ } nothing='something'>", [KVP("href", "~/home"), KVP("nothing", "something")] },
                {
                    "<a href=\"@DateTime.Now::0\" class='btn btn-success' random>",
                    [KVP("href", $"{csharp}::0"), KVP("class", "btn btn-success"), KVP("random", "")]
                },
                { "<a href=>", [KVP("href", "")] },
                { "<a href='\">  ", [KVP("href", "\">  ")] },
                { "<a href'", [KVP("href'", "")] },
            };
        }
    }

    [Theory]
    [MemberData(nameof(GetAttributeNameValuePairsData))]
    public void GetAttributeNameValuePairs_ParsesPairsCorrectly(
        string documentContent,
        ImmutableArray<KeyValuePair<string, string>> expectedPairs)
    {
        // Arrange
        using var errorSink = new ErrorSink();
        var parseResult = ParseDocument(documentContent);
        var document = parseResult.Root;

        // Assert - Guard
        var rootBlock = Assert.IsType<RazorDocumentSyntax>(document);
        var rootMarkup = Assert.IsType<MarkupBlockSyntax>(rootBlock.Document);
        var childBlock = Assert.Single(rootMarkup.Children);
        var element = Assert.IsType<MarkupElementSyntax>(childBlock);
        Assert.Empty(errorSink.GetErrorsAndClear());

        // Act
        var pairs = TagHelperParseTreeRewriter.Rewriter.GetAttributeNameValuePairs(element.StartTag);

        // Assert
        Assert.Equal<KeyValuePair<string, string>>(expectedPairs, pairs);
    }

    public static readonly TagHelperCollection PartialRequiredParentTags_TagHelpers =
    [
        TagHelperDescriptorBuilder.CreateTagHelper("StrongTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("strong"))
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build(),
        TagHelperDescriptorBuilder.CreateTagHelper("CatchALlTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
            .Build(),
        TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
            .Build(),
    ];

    [Fact]
    public void UnderstandsPartialRequiredParentTags1()
    {
        EvaluateData(PartialRequiredParentTags_TagHelpers, """
            <p><strong>
            """);
    }

    [Fact]
    public void UnderstandsPartialRequiredParentTags2()
    {
        EvaluateData(PartialRequiredParentTags_TagHelpers, """
            <p><strong></strong>
            """);
    }

    [Fact]
    public void UnderstandsPartialRequiredParentTags3()
    {
        EvaluateData(PartialRequiredParentTags_TagHelpers, """
            <p><strong></p><strong>
            """);
    }

    [Fact]
    public void UnderstandsPartialRequiredParentTags4()
    {
        EvaluateData(PartialRequiredParentTags_TagHelpers, """
            <<p><<strong></</strong</strong></p>
            """);
    }

    [Fact]
    public void UnderstandsPartialRequiredParentTags5()
    {
        EvaluateData(PartialRequiredParentTags_TagHelpers, """
            <<p><<strong></</strong></strong></p>
            """);
    }

    [Fact]
    public void UnderstandsPartialRequiredParentTags6()
    {
        EvaluateData(PartialRequiredParentTags_TagHelpers, """
            <<p><<custom></<</custom></custom></p>
            """);
    }

    public static readonly TagHelperCollection NestedVoidSelfClosingRequiredParent_TagHelpers =
    [
        TagHelperDescriptorBuilder.CreateTagHelper("InputTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule
                .RequireTagName("input")
                .RequireTagStructure(TagStructure.WithoutEndTag))
            .Build(),
        TagHelperDescriptorBuilder.CreateTagHelper("StrongTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule
                .RequireTagName("strong")
                .RequireParentTag("p"))
            .TagMatchingRuleDescriptor(rule => rule
                .RequireTagName("strong")
                .RequireParentTag("input"))
            .Build(),
        TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
            .Build(),
    ];

    public static readonly TagHelperCollection CatchAllAttribute_TagHelpers =
    [
        TagHelperDescriptorBuilder.CreateEventHandler("InputTagHelper1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule
                .RequireTagName("*")
                .RequireAttributeDescriptor(b =>
                {
                    b.Name = "onclick";
                }))
            .Build(),
    ];

    [Fact]
    public void UnderstandsInvalidHtml()
    {
        EvaluateData(CatchAllAttribute_TagHelpers, """
            <a onclick="() => {}"><a/></a><strong>Miscolored!</strong>
            """);
    }

    [Fact]
    public void UnderstandsNestedVoidSelfClosingRequiredParent1()
    {
        EvaluateData(NestedVoidSelfClosingRequiredParent_TagHelpers, """
            <input><strong></strong>
            """);
    }

    [Fact]
    public void UnderstandsNestedVoidSelfClosingRequiredParent2()
    {
        EvaluateData(NestedVoidSelfClosingRequiredParent_TagHelpers, """
            <p><input><strong></strong></p>
            """);
    }

    [Fact]
    public void UnderstandsNestedVoidSelfClosingRequiredParent3()
    {
        EvaluateData(NestedVoidSelfClosingRequiredParent_TagHelpers, """
            <p><br><strong></strong></p>
            """);
    }

    [Fact]
    public void UnderstandsNestedVoidSelfClosingRequiredParent4()
    {
        EvaluateData(NestedVoidSelfClosingRequiredParent_TagHelpers, """
            <p><p><br></p><strong></strong></p>
            """);
    }

    [Fact]
    public void UnderstandsNestedVoidSelfClosingRequiredParent5()
    {
        EvaluateData(NestedVoidSelfClosingRequiredParent_TagHelpers, """
            <input><strong></strong>
            """);
    }

    [Fact]
    public void UnderstandsNestedVoidSelfClosingRequiredParent6()
    {
        EvaluateData(NestedVoidSelfClosingRequiredParent_TagHelpers, """
            <p><input /><strong /></p>
            """);
    }

    [Fact]
    public void UnderstandsNestedVoidSelfClosingRequiredParent7()
    {
        EvaluateData(NestedVoidSelfClosingRequiredParent_TagHelpers, """
            <p><br /><strong /></p>
            """);
    }

    [Fact]
    public void UnderstandsNestedVoidSelfClosingRequiredParent8()
    {
        EvaluateData(NestedVoidSelfClosingRequiredParent_TagHelpers, """
            <p><p><br /></p><strong /></p>
            """);
    }

    public static readonly TagHelperCollection NestedRequiredParent_TagHelpers =
    [
        TagHelperDescriptorBuilder.CreateTagHelper("StrongTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule =>
                rule
                .RequireTagName("strong")
                .RequireParentTag("p"))
            .TagMatchingRuleDescriptor(rule =>
                rule
                .RequireTagName("strong")
                .RequireParentTag("div"))
            .Build(),
        TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
            .Build(),
    ];

    [Fact]
    public void UnderstandsNestedRequiredParent1()
    {
        EvaluateData(NestedRequiredParent_TagHelpers, """
            <strong></strong>
            """);
    }

    [Fact]
    public void UnderstandsNestedRequiredParent2()
    {
        EvaluateData(NestedRequiredParent_TagHelpers, """
            <p><strong></strong></p>
            """);
    }

    [Fact]
    public void UnderstandsNestedRequiredParent3()
    {
        EvaluateData(NestedRequiredParent_TagHelpers, """
            <div><strong></strong></div>
            """);
    }

    [Fact]
    public void UnderstandsNestedRequiredParent4()
    {
        EvaluateData(NestedRequiredParent_TagHelpers, """
            <strong><strong></strong></strong>
            """);
    }

    [Fact]
    public void UnderstandsNestedRequiredParent5()
    {
        EvaluateData(NestedRequiredParent_TagHelpers, """
            <p><strong><strong></strong></strong></p>
            """);
    }

    [Fact]
    public void UnderstandsTagHelperPrefixAndAllowedChildren()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                .AllowChildTag("strong")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("StrongTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("strong"))
                .Build(),
        ];

        EvaluateData(tagHelpers, """
            <th:p><th:strong></th:strong></th:p>
            """,
            tagHelperPrefix: "th:");
    }

    [Fact]
    public void UnderstandsTagHelperPrefixAndAllowedChildrenAndRequireParent()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                .AllowChildTag("strong")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("StrongTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("strong").RequireParentTag("p"))
                .Build(),
        ];

        EvaluateData(tagHelpers, """
            <th:p><th:strong></th:strong></th:p>
            """,
            tagHelperPrefix: "th:");
    }

    [Fact]
    public void InvalidStructure_UnderstandsTHPrefixAndAllowedChildrenAndRequireParent()
    {
        // Rewrite_InvalidStructure_UnderstandsTagHelperPrefixAndAllowedChildrenAndRequireParent
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                .AllowChildTag("strong")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("StrongTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("strong").RequireParentTag("p"))
                .Build(),
        ];

        EvaluateData(tagHelpers, """
            <th:p></th:strong></th:p>
            """,
            tagHelperPrefix: "th:");
    }

    [Fact]
    public void NonTagHelperChild_UnderstandsTagHelperPrefixAndAllowedChildren()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                .AllowChildTag("strong")
                .Build(),
        ];

        EvaluateData(tagHelpers, """
            <th:p><strong></strong></th:p>
            """,
            tagHelperPrefix: "th:");
    }

    [Fact]
    public void DoesNotUnderstandTagHelpersInInvalidHtmlTypedScriptTags1()
    {
        RunParseTreeRewriterTest("""
            <script type><input /></script>
            """,
            tagNames: ["input"]);
    }

    [Fact]
    public void DoesNotUnderstandTagHelpersInInvalidHtmlTypedScriptTags2()
    {
        RunParseTreeRewriterTest("""
            <script types='text/html'><input /></script>
            """,
            tagNames: ["input"]);
    }

    [Fact]
    public void DoesNotUnderstandTagHelpersInInvalidHtmlTypedScriptTags3()
    {
        RunParseTreeRewriterTest("""
            <script type='text/html invalid'><input /></script>
            """,
            tagNames: ["input"]);
    }

    [Fact]
    public void DoesNotUnderstandTagHelpersInInvalidHtmlTypedScriptTags4()
    {
        RunParseTreeRewriterTest("""
            <script type='text/ng-*' type='text/html'><input /></script>
            """,
            tagNames: ["input"]);
    }

    [Fact]
    public void UnderstandsTagHelpersInHtmlTypedScriptTags1()
    {
        RunParseTreeRewriterTest("""
            <script type='text/html'><input /></script>
            """,
            tagNames: ["p", "input"]);
    }

    [Fact]
    public void UnderstandsTagHelpersInHtmlTypedScriptTags2()
    {
        RunParseTreeRewriterTest("""
            <script id='scriptTag' type='text/html' class='something'><input /></script>
            """,
            tagNames: ["p", "input"]);
    }

    [Fact]
    public void UnderstandsTagHelpersInHtmlTypedScriptTags3()
    {
        RunParseTreeRewriterTest("""
            <script type='text/html'><p><script type='text/html'><input /></script></p></script>
            """,
            tagNames: ["p", "input"]);
    }

    [Fact]
    public void UnderstandsTagHelpersInHtmlTypedScriptTags4()
    {
        RunParseTreeRewriterTest("""
            <script type='text/html'><p><script type='text/ html'><input /></script></p></script>
            """,
            tagNames: ["p", "input"]);
    }

    [Fact]
    public void CanHandleInvalidChildrenWithWhitespace()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                .AllowChildTag("br")
                .Build()
        ];

        EvaluateData(tagHelpers, """
            <p>
                <strong>
                    Hello
                </strong>
            </p>
            """);
    }

    [Fact]
    public void RecoversWhenRequiredAttributeMismatchAndRestrictedChildren()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("StrongTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("strong")
                    .RequireAttributeDescriptor(attribute => attribute.Name("required")))
                .AllowChildTag("br")
                .Build()
        ];

        EvaluateData(tagHelpers, """
            <strong required><strong></strong></strong>
            """);
    }

    [Fact]
    public void CanHandleMultipleTagHelpersWithAllowedChildren_OneNull()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper1", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                .AllowChildTag("strong")
                .AllowChildTag("br")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper2", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("StrongTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("strong"))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("BRTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("br")
                    .RequireTagStructure(TagStructure.WithoutEndTag))
                .Build(),
        ];

        EvaluateData(tagHelpers, """
            <p><strong>Hello World</strong><br></p>
            """);
    }

    [Fact]
    public void CanHandleMultipleTagHelpersWithAllowedChildren()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper1", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                .AllowChildTag("strong")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper2", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                .AllowChildTag("br")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("StrongTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("strong"))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("BRTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("br")
                    .RequireTagStructure(TagStructure.WithoutEndTag))
                .Build(),
        ];

        EvaluateData(tagHelpers, """
            <p><strong>Hello World</strong><br></p>
            """);
    }

    [Fact]
    public void UnderstandsAllowedChildren1()
    {
        var tagHelpers = GetAllowedChildrenTagHelpers(["br"]);

        EvaluateData(tagHelpers, """
            <p><br /></p>
            """);
    }

    [Fact]
    public void UnderstandsAllowedChildren2()
    {
        var tagHelpers = GetAllowedChildrenTagHelpers(["br"]);

        EvaluateData(tagHelpers, """
            <p>
            <br />
            </p>
            """);
    }

    [Fact]
    public void UnderstandsAllowedChildren3()
    {
        var tagHelpers = GetAllowedChildrenTagHelpers(["strong"]);

        EvaluateData(tagHelpers, """
            <p><br></p>
            """);
    }

    [Fact]
    public void UnderstandsAllowedChildren4()
    {
        var tagHelpers = GetAllowedChildrenTagHelpers(["strong"]);

        EvaluateData(tagHelpers, """
            <p>Hello</p>
            """);
    }

    [Fact]
    public void UnderstandsAllowedChildren5()
    {
        var tagHelpers = GetAllowedChildrenTagHelpers(["br", "strong"]);

        EvaluateData(tagHelpers, """
            <p><hr /></p>
            """);
    }

    [Fact]
    public void UnderstandsAllowedChildren6()
    {
        var tagHelpers = GetAllowedChildrenTagHelpers(["strong"]);

        EvaluateData(tagHelpers, """
            <p><br>Hello</p>
            """);
    }

    [Fact]
    public void UnderstandsAllowedChildren7()
    {
        var tagHelpers = GetAllowedChildrenTagHelpers(["strong"]);

        EvaluateData(tagHelpers, """
            <p><strong>Title:</strong><br />Something</p>
            """);
    }

    [Fact]
    public void UnderstandsAllowedChildren8()
    {
        var tagHelpers = GetAllowedChildrenTagHelpers(["strong", "br"]);

        EvaluateData(tagHelpers, """
            <p><strong>Title:</strong><br />Something</p>
            """);
    }

    [Fact]
    public void UnderstandsAllowedChildren9()
    {
        var tagHelpers = GetAllowedChildrenTagHelpers(["strong", "br"]);

        EvaluateData(tagHelpers, """
            <p>  <strong>Title:</strong>  <br />  Something</p>
            """);
    }

    [Fact]
    public void UnderstandsAllowedChildren10()
    {
        var tagHelpers = GetAllowedChildrenTagHelpers(["strong"]);

        EvaluateData(tagHelpers, """
            <p><strong>Title:<br><em>A Very Cool</em></strong><br />Something</p>
            """);
    }

    [Fact]
    public void UnderstandsAllowedChildren11()
    {
        var tagHelpers = GetAllowedChildrenTagHelpers(["custom"]);

        EvaluateData(tagHelpers, """
            <p><custom>Title:<br><em>A Very Cool</em></custom><br />Something</p>
            """);
    }

    [Fact]
    public void UnderstandsAllowedChildren12()
    {
        var tagHelpers = GetAllowedChildrenTagHelpers(["custom"]);

        EvaluateData(tagHelpers, """
            <p></</p>
            """);
    }

    [Fact]
    public void UnderstandsAllowedChildren13()
    {
        var tagHelpers = GetAllowedChildrenTagHelpers(["custom"]);

        EvaluateData(tagHelpers, """
            <p><</p>
            """);
    }

    [Fact]
    public void UnderstandsAllowedChildren14()
    {
        var tagHelpers = GetAllowedChildrenTagHelpers(["custom", "strong"]);

        EvaluateData(tagHelpers, """
            <p><custom><br>:<strong><strong>Hello</strong></strong>:<input></custom></p>
            """);
    }

    private static TagHelperCollection GetAllowedChildrenTagHelpers(string[] allowedChildren)
    {
        var pTagHelperBuilder = TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"));
        var strongTagHelperBuilder = TagHelperDescriptorBuilder.CreateTagHelper("StrongTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("strong"));

        foreach (var childTag in allowedChildren)
        {
            pTagHelperBuilder.AllowChildTag(childTag);
            strongTagHelperBuilder.AllowChildTag(childTag);
        }

        return
        [
            pTagHelperBuilder.Build(),
            strongTagHelperBuilder.Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("BRTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("br")
                    .RequireTagStructure(TagStructure.WithoutEndTag))
                .Build(),
        ];
    }

    [Fact]
    public void AllowsSimpleHtmlCommentsAsChildren()
    {
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
            .AllowChildTag("b")
            .Build();

        EvaluateData([tagHelper], """
            <p><b>asdf</b><!--Hello World--></p>
            """);
    }

    [Fact]
    public void DoesntAllowSimpleHtmlCommentsAsChildrenWhenFeatureFlagIsOff()
    {
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
            .AllowChildTag("b")
            .Build();

        EvaluateData(
            [tagHelper],
            """
            <p><!--Hello--></p>
            """,
            languageVersion: RazorLanguageVersion.Version_2_0,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public void FailsForContentWithCommentsAsChildren()
    {
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
            .AllowChildTag("b")
            .Build();

        EvaluateData([tagHelper], """
            <p><!--Hello-->asdf<!--World--></p>
            """);
    }

    [Fact]
    public void AllowsRazorCommentsAsChildren()
    {
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
            .AllowChildTag("b")
            .Build();

        EvaluateData([tagHelper], """
            <p><b>asdf</b>@*asdf*@</p>
            """);
    }

    [Fact]
    public void AllowsRazorMarkupInHtmlComment()
    {
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
            .AllowChildTag("b")
            .Build();

        EvaluateData([tagHelper], """
            <p><b>asdf</b><!--Hello @World--></p>
            """);
    }

    [Fact]
    public void UnderstandsNullTagNameWithAllowedChildrenForCatchAll()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                .AllowChildTag("custom")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("CatchAllTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
                .Build(),
        ];

        EvaluateData(tagHelpers, """
            <p></</p>
            """);
    }

    [Fact]
    public void UnderstandsNullTagNameWithAllowedChildrenForCatchAllWithPrefix()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                .AllowChildTag("custom")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("CatchAllTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
                .Build(),
        ];

        EvaluateData(tagHelpers, """
            <th:p></</th:p>
            """, "th:");
    }

    [Fact]
    public void CanHandleStartTagOnlyTagTagMode()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("InputTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("input")
                    .RequireTagStructure(TagStructure.WithoutEndTag))
                .Build()
        ];

        EvaluateData(tagHelpers, """
            <input>
            """);
    }

    [Fact]
    public void CreatesErrorForWithoutEndTagTagStructureForEndTags()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("InputTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("input")
                    .RequireTagStructure(TagStructure.WithoutEndTag))
                .Build()
        ];

        EvaluateData(tagHelpers, """
            </input>
            """);
    }

    [Fact]
    public void CreatesErrorForInconsistentTagStructures()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("InputTagHelper1", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("input")
                    .RequireTagStructure(TagStructure.WithoutEndTag))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("InputTagHelper2", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("input")
                    .RequireTagStructure(TagStructure.NormalOrSelfClosing))
                .Build()
        ];

        EvaluateData(tagHelpers, """
            <input>
            """);
    }

    public static readonly TagHelperCollection RequiredAttribute_TagHelpers =
    [
        TagHelperDescriptorBuilder.CreateTagHelper("pTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule =>
                rule
                .RequireTagName("p")
                .RequireAttributeDescriptor(attribute => attribute.Name("class")))
            .Build(),
        TagHelperDescriptorBuilder.CreateTagHelper("divTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule =>
                rule
                .RequireTagName("div")
                .RequireAttributeDescriptor(attribute => attribute.Name("class"))
                .RequireAttributeDescriptor(attribute => attribute.Name("style")))
            .Build(),
        TagHelperDescriptorBuilder.CreateTagHelper("catchAllTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule =>
                rule
                .RequireTagName("*")
                .RequireAttributeDescriptor(attribute => attribute.Name("catchAll")))
            .Build()
    ];

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly1()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <p />
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly2()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <p></p>
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly3()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <div />
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly4()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <div></div>
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly5()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <p class="btn" />
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly6()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <p class="@DateTime.Now" />
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly7()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <p class="btn">words and spaces</p>
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly8()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <p class="@DateTime.Now">words and spaces</p>
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly9()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <p class="btn">words<strong>and</strong>spaces</p>
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly10()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <strong catchAll="hi" />
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly11()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <strong catchAll="@DateTime.Now" />
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly12()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <strong catchAll="hi">words and spaces</strong>
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly13()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <strong catchAll="@DateTime.Now">words and spaces</strong>
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly14()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <div class="btn" />
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly15()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <div class="btn"></div>
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly16()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <p notRequired="a" class="btn" />
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly17()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <p notRequired="@DateTime.Now" class="btn" />
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly18()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <p notRequired="a" class="btn">words and spaces</p>
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly19()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <div style="" class="btn" />
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly20()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <div style="@DateTime.Now" class="btn" />
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly21()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <div style="" class="btn">words and spaces</div>
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly22()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <div style="@DateTime.Now" class="@DateTime.Now">words and spaces</div>
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly23()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <div style="" class="btn">words<strong>and</strong>spaces</div>
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly24()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <p class="btn" catchAll="hi" />
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly25()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <p class="btn" catchAll="hi">words and spaces</p>
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly26()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <div style="" class="btn" catchAll="hi" />
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly27()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <div style="" class="btn" catchAll="hi" >words and spaces</div>
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly28()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <div style="" class="btn" catchAll="@@hi" >words and spaces</div>
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly29()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <div style="@DateTime.Now" class="@DateTime.Now" catchAll="@DateTime.Now" >words and spaces</div>
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly30()
    {
        EvaluateData(RequiredAttribute_TagHelpers, """
            <div style="" class="btn" catchAll="hi" >words<strong>and</strong>spaces</div>
            """);
    }

    public static readonly TagHelperCollection NestedRequiredAttribute_TagHelpers =
    [
        TagHelperDescriptorBuilder.CreateTagHelper("pTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule =>
                rule
                .RequireTagName("p")
                .RequireAttributeDescriptor(attribute => attribute.Name("class")))
            .Build(),
        TagHelperDescriptorBuilder.CreateTagHelper("catchAllTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule =>
                rule
                .RequireTagName("*")
                .RequireAttributeDescriptor(attribute => attribute.Name("catchAll")))
            .Build(),
    ];

    [Fact]
    public void NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly1()
    {
        EvaluateData(NestedRequiredAttribute_TagHelpers, """
            <p class="btn"><p></p></p>
            """);
    }

    [Fact]
    public void NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly2()
    {
        EvaluateData(NestedRequiredAttribute_TagHelpers, """
            <strong catchAll="hi"><strong></strong></strong>
            """);
    }

    [Fact]
    public void NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly3()
    {
        EvaluateData(NestedRequiredAttribute_TagHelpers, """
            <p class="btn"><strong><p></p></strong></p>
            """);
    }

    [Fact]
    public void NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly4()
    {
        EvaluateData(NestedRequiredAttribute_TagHelpers, """
            <strong catchAll="hi"><p><strong></strong></p></strong>
            """);
    }

    [Fact]
    public void NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly5()
    {
        EvaluateData(NestedRequiredAttribute_TagHelpers, """
            <p class="btn"><strong catchAll="hi"><p></p></strong></p>
            """);
    }

    [Fact]
    public void NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly6()
    {
        EvaluateData(NestedRequiredAttribute_TagHelpers, """
            <strong catchAll="hi"><p class="btn"><strong></strong></p></strong>
            """);
    }

    [Fact]
    public void NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly7()
    {
        EvaluateData(NestedRequiredAttribute_TagHelpers, """
            <p class="btn"><p class="btn"><p></p></p></p>
            """);
    }

    [Fact]
    public void NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly8()
    {
        EvaluateData(NestedRequiredAttribute_TagHelpers, """
            <strong catchAll="hi"><strong catchAll="hi"><strong></strong></strong></strong>
            """);
    }

    [Fact]
    public void NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly9()
    {
        EvaluateData(NestedRequiredAttribute_TagHelpers, """
            <p class="btn"><p><p><p class="btn"><p></p></p></p></p></p>
            """);
    }

    [Fact]
    public void NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly10()
    {
        EvaluateData(NestedRequiredAttribute_TagHelpers, """
            <strong catchAll="hi"><strong><strong><strong catchAll="hi"><strong></strong></strong></strong></strong></strong>
            """);
    }

    public static readonly TagHelperCollection MalformedRequiredAttribute_TagHelpers =
    [
        TagHelperDescriptorBuilder.CreateTagHelper("pTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule =>
                rule
                .RequireTagName("p")
                .RequireAttributeDescriptor(attribute => attribute.Name("class")))
            .Build(),
    ];

    [Fact]
    public void RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly1()
    {
        EvaluateData(MalformedRequiredAttribute_TagHelpers, """<p""");
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly2()
    {
        EvaluateData(MalformedRequiredAttribute_TagHelpers, """
            <p class="btn"
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly3()
    {
        EvaluateData(MalformedRequiredAttribute_TagHelpers, """
            <p notRequired="hi" class="btn"
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly4()
    {
        EvaluateData(MalformedRequiredAttribute_TagHelpers, """
            <p></p
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly5()
    {
        EvaluateData(MalformedRequiredAttribute_TagHelpers, """
            <p class="btn"></p
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly6()
    {
        EvaluateData(MalformedRequiredAttribute_TagHelpers, """
            <p notRequired="hi" class="btn"></p
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly7()
    {
        EvaluateData(MalformedRequiredAttribute_TagHelpers, """
            <p class="btn" <p>
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly8()
    {
        EvaluateData(MalformedRequiredAttribute_TagHelpers, """
            <p notRequired="hi" class="btn" <p>
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly9()
    {
        EvaluateData(MalformedRequiredAttribute_TagHelpers, """
            <p class="btn" </p
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly10()
    {
        EvaluateData(MalformedRequiredAttribute_TagHelpers, """
            <p notRequired="hi" class="btn" </p
            """);
    }

    [Fact]
    public void RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly11()
    {
        EvaluateData(MalformedRequiredAttribute_TagHelpers, """
            <p class='foo'>@if(true){</p>}</p>
            """);
    }

    public static readonly TagHelperCollection PrefixedTagHelperColon_TagHelpers =
    [
        TagHelperDescriptorBuilder.CreateTagHelper("mythTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("myth"))
            .Build(),
        TagHelperDescriptorBuilder.CreateTagHelper("mythTagHelper2", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("myth2"))
            .BoundAttributeDescriptor(attribute =>
                attribute
                .Name("bound")
                .PropertyName("Bound")
                .TypeName(typeof(bool).FullName))
            .Build()
    ];

    public static readonly TagHelperCollection PrefixedTagHelperCatchAll_TagHelpers =
    [
        TagHelperDescriptorBuilder.CreateTagHelper("mythTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
            .Build(),
    ];

    [Fact]
    public void AllowsPrefixedTagHelpers1()
    {
        EvaluateData(PrefixedTagHelperCatchAll_TagHelpers, """
            <th: />
            """,
            tagHelperPrefix: "th:");
    }

    [Fact]
    public void AllowsPrefixedTagHelpers2()
    {
        EvaluateData(PrefixedTagHelperCatchAll_TagHelpers, """
            <th:>words and spaces</th:>
            """,
            tagHelperPrefix: "th:");
    }

    [Fact]
    public void AllowsPrefixedTagHelpers3()
    {
        EvaluateData(PrefixedTagHelperColon_TagHelpers, """
            <th:myth />
            """,
            tagHelperPrefix: "th:");
    }

    [Fact]
    public void AllowsPrefixedTagHelpers4()
    {
        EvaluateData(PrefixedTagHelperColon_TagHelpers, """
            <th:myth></th:myth>
            """,
            tagHelperPrefix: "th:");
    }

    [Fact]
    public void AllowsPrefixedTagHelpers5()
    {
        EvaluateData(PrefixedTagHelperColon_TagHelpers, """
            <th:myth><th:my2th></th:my2th></th:myth>
            """,
            tagHelperPrefix: "th:");
    }

    [Fact]
    public void AllowsPrefixedTagHelpers6()
    {
        EvaluateData(PrefixedTagHelperColon_TagHelpers, """
            <!th:myth />
            """,
            tagHelperPrefix: "th:");
    }

    [Fact]
    public void AllowsPrefixedTagHelpers7()
    {
        EvaluateData(PrefixedTagHelperColon_TagHelpers, """
            <!th:myth></!th:myth>
            """,
            tagHelperPrefix: "th:");
    }

    [Fact]
    public void AllowsPrefixedTagHelpers8()
    {
        EvaluateData(PrefixedTagHelperColon_TagHelpers, """
            <th:myth class="btn" />
            """,
            tagHelperPrefix: "th:");
    }

    [Fact]
    public void AllowsPrefixedTagHelpers9()
    {
        EvaluateData(PrefixedTagHelperColon_TagHelpers, """
            <th:myth2 class="btn" />
            """,
            tagHelperPrefix: "th:");
    }

    [Fact]
    public void AllowsPrefixedTagHelpers10()
    {
        EvaluateData(PrefixedTagHelperColon_TagHelpers, """
            <th:myth class="btn">words and spaces</th:myth>
            """,
            tagHelperPrefix: "th:");
    }

    [Fact]
    public void AllowsPrefixedTagHelpers11()
    {
        EvaluateData(PrefixedTagHelperColon_TagHelpers, """
            <th:myth2 bound="@DateTime.Now" />
            """,
            tagHelperPrefix: "th:");
    }

    [Fact]
    public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithAttrTextTag1()
    {
        RunParseTreeRewriterTest("""
            @{<!text class="btn">}
            """,
            tagNames: ["p", "text"]);
    }

    [Fact]
    public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithAttrTextTag2()
    {
        RunParseTreeRewriterTest("""
            @{<!text class="btn"></!text>}
            """,
            tagNames: ["p", "text"]);
    }

    [Fact]
    public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithAttrTextTag3()
    {
        RunParseTreeRewriterTest("""
            @{<!text class="btn">words with spaces</!text>}
            """,
            tagNames: ["p", "text"]);
    }

    [Fact]
    public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithAttrTextTag4()
    {
        RunParseTreeRewriterTest("""
            @{<!text class='btn1 btn2' class2=btn></!text>}
            """,
            tagNames: ["p", "text"]);
    }

    [Fact]
    public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithAttrTextTag5()
    {
        RunParseTreeRewriterTest("""
            @{<!text class='btn1 @DateTime.Now btn2'></!text>}
            """,
            tagNames: ["p", "text"]);
    }

    [Fact]
    public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag1()
    {
        RunParseTreeRewriterTest("""
            @{<!text>}
            """,
            tagNames: ["p", "text"]);
    }

    [Fact]
    public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag2()
    {
        RunParseTreeRewriterTest("""
            @{</!text>}
            """,
            tagNames: ["p", "text"]);
    }

    [Fact]
    public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag3()
    {
        RunParseTreeRewriterTest("""
            @{<!text></!text>}
            """,
            tagNames: ["p", "text"]);
    }

    [Fact]
    public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag4()
    {
        RunParseTreeRewriterTest("""
            @{<!text>words and spaces</!text>}
            """,
            tagNames: ["p", "text"]);
    }

    [Fact]
    public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag5()
    {
        RunParseTreeRewriterTest("""
            @{<!text></text>}
            """,
            tagNames: ["p", "text"]);
    }

    [Fact]
    public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag6()
    {
        RunParseTreeRewriterTest("""
            @{<text></!text>}
            """,
            tagNames: ["p", "text"]);
    }

    [Fact]
    public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag7()
    {
        RunParseTreeRewriterTest("""
            @{<!text><text></text></!text>}
            """,
            tagNames: ["p", "text"]);
    }

    [Fact]
    public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag8()
    {
        RunParseTreeRewriterTest("""
            @{<text><!text></!text>}
            """,
            tagNames: ["p", "text"]);
    }

    [Fact]
    public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag9()
    {
        RunParseTreeRewriterTest("""
            @{<!text></!text></text>}
            """,
            tagNames: ["p", "text"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptForIncompleteTextTagInCSharpBlock1()
    {
        RunParseTreeRewriterTest("""
            @{<!text}
            """,
            tagNames: ["text"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptForIncompleteTextTagInCSharpBlock2()
    {
        RunParseTreeRewriterTest("""
            @{<!text /}
            """,
            tagNames: ["text"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptForIncompleteTextTagInCSharpBlock3()
    {
        RunParseTreeRewriterTest("""
            @{<!text class=}
            """,
            tagNames: ["text"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptForIncompleteTextTagInCSharpBlock4()
    {
        RunParseTreeRewriterTest("""
            @{<!text class="btn}
            """,
            tagNames: ["text"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptForIncompleteTextTagInCSharpBlock5()
    {
        RunParseTreeRewriterTest("""
            @{<!text class="btn"}
            """,
            tagNames: ["text"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptForIncompleteTextTagInCSharpBlock6()
    {
        RunParseTreeRewriterTest("""
            @{<!text class="btn" /}
            """,
            tagNames: ["text"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock1()
    {
        RunParseTreeRewriterTest("""
            @{<!}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock2()
    {
        RunParseTreeRewriterTest("""
            @{<!p}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock3()
    {
        RunParseTreeRewriterTest("""
            @{<!p /}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock4()
    {
        RunParseTreeRewriterTest("""
            @{<!p class=}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock5()
    {
        RunParseTreeRewriterTest("""
            @{<!p class="btn}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock6()
    {
        RunParseTreeRewriterTest("""
            @{<!p class="btn@@}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock7()
    {
        RunParseTreeRewriterTest("""
            @{<!p class="btn"}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock8()
    {
        RunParseTreeRewriterTest("""
            @{<!p class="btn" /}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptForIncompleteHTML1()
    {
        RunParseTreeRewriterTest("""
            <!
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptForIncompleteHTML2()
    {
        RunParseTreeRewriterTest("""
            <!p
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptForIncompleteHTML3()
    {
        RunParseTreeRewriterTest("""
            <!p /
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptForIncompleteHTML4()
    {
        RunParseTreeRewriterTest("""
            <!p class=
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptForIncompleteHTML5()
    {
        RunParseTreeRewriterTest("""
            <!p class="btn
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptForIncompleteHTML6()
    {
        RunParseTreeRewriterTest("""
            <!p class="btn"
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptForIncompleteHTML7()
    {
        RunParseTreeRewriterTest("""
            <!p class="btn" /
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutCSharp_WithBlockData1()
    {
        RunParseTreeRewriterTest("""
            @{<!p>}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutCSharp_WithBlockData2()
    {
        RunParseTreeRewriterTest("""
            @{</!p>}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutCSharp_WithBlockData3()
    {
        RunParseTreeRewriterTest("""
            @{<!p></!p>}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutCSharp_WithBlockData4()
    {
        RunParseTreeRewriterTest("""
            @{<!p>words and spaces</!p>}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutCSharp_WithBlockData5()
    {
        RunParseTreeRewriterTest("""
            @{<!p></p>}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutCSharp_WithBlockData6()
    {
        RunParseTreeRewriterTest("""
            @{<p></!p>}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutCSharp_WithBlockData7()
    {
        RunParseTreeRewriterTest("""
            @{<p><!p></!p></p>}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutCSharp_WithBlockData8()
    {
        RunParseTreeRewriterTest("""
            @{<p><!p></!p>}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutCSharp_WithBlockData9()
    {
        RunParseTreeRewriterTest("""
            @{<!p></!p></p>}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutCSharp_WithBlockData10()
    {
        RunParseTreeRewriterTest("""
            @{<strong></!p></strong>}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutCSharp_WithBlockData11()
    {
        RunParseTreeRewriterTest("""
            @{<strong></strong><!p></!p>}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutCSharp_WithBlockData12()
    {
        RunParseTreeRewriterTest("""
            @{<p><strong></!strong><!p></strong></!p>}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutCSharp_WithAttributeData1()
    {
        RunParseTreeRewriterTest("""
            @{<!p class="btn">}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutCSharp_WithAttributeData2()
    {
        RunParseTreeRewriterTest("""
            @{<!p class="btn"></!p>}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutCSharp_WithAttributeData3()
    {
        RunParseTreeRewriterTest("""
            @{<!p class="btn">words with spaces</!p>}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutCSharp_WithAttributeData4()
    {
        RunParseTreeRewriterTest("""
            @{<!p class='btn1 btn2' class2=btn></!p>}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutCSharp_WithAttributeData5()
    {
        RunParseTreeRewriterTest("""
            @{<!p class='btn1 @DateTime.Now btn2'></!p>}
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutHTML_WithBlockData1()
    {
        RunParseTreeRewriterTest("""
            <!p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutHTML_WithBlockData2()
    {
        RunParseTreeRewriterTest("""
            </!p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutHTML_WithBlockData3()
    {
        RunParseTreeRewriterTest("""
            <!p></!p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutHTML_WithBlockData4()
    {
        RunParseTreeRewriterTest("""
            <!p>words and spaces</!p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutHTML_WithBlockData5()
    {
        RunParseTreeRewriterTest("<!p></p>",
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutHTML_WithBlockData6()
    {
        RunParseTreeRewriterTest("""
            <p></!p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutHTML_WithBlockData7()
    {
        RunParseTreeRewriterTest("""
            <p><!p></!p></p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutHTML_WithBlockData8()
    {
        RunParseTreeRewriterTest("""
            <p><!p></!p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutHTML_WithBlockData9()
    {
        RunParseTreeRewriterTest("""
            <!p></!p></p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutHTML_WithBlockData10()
    {
        RunParseTreeRewriterTest("""
            <strong></!p></strong>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutHTML_WithBlockData11()
    {
        RunParseTreeRewriterTest("""
            <strong></strong><!p></!p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutHTML_WithBlockData12()
    {
        RunParseTreeRewriterTest("""
            <p><strong></!strong><!p></strong></!p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutHTML_WithAttributeData1()
    {
        RunParseTreeRewriterTest("""
            <!p class="btn">
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutHTML_WithAttributeData2()
    {
        RunParseTreeRewriterTest("""
            <!p class="btn"></!p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutHTML_WithAttributeData3()
    {
        RunParseTreeRewriterTest("""
            <!p class="btn">words and spaces</!p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutHTML_WithAttributeData4()
    {
        RunParseTreeRewriterTest("""
            <!p class='btn1 btn2' class2=btn></!p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void AllowsTagHelperElementOptOutHTML_WithAttributeData5()
    {
        RunParseTreeRewriterTest("""
            <!p class='btn1 @DateTime.Now btn2'></!p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void DoesNotRewriteTextTagTransitionTagHelpers1()
    {
        RunParseTreeRewriterTest("""
            <text>Hello World</text>
            """,
            tagNames: ["p", "text"]);
    }

    [Fact]
    public void DoesNotRewriteTextTagTransitionTagHelpers2()
    {
        RunParseTreeRewriterTest("""
            @{<text>Hello World</text>}
            """,
            tagNames: ["p", "text"]);
    }

    [Fact]
    public void DoesNotRewriteTextTagTransitionTagHelpers3()
    {
        RunParseTreeRewriterTest("""
            @{<text><p>Hello World</p></text>}
            """,
            tagNames: ["p", "text"]);
    }

    [Fact]
    public void DoesNotRewriteTextTagTransitionTagHelpers4()
    {
        RunParseTreeRewriterTest("""
            @{<p><text>Hello World</text></p>}
            """,
            tagNames: ["p", "text"]);
    }

    [Fact]
    public void DoesNotRewriteSpecialTagTagHelpers1()
    {
        RunParseTreeRewriterTest("""
            <foo><!-- Hello World --></foo>
            """,
            tagNames: ["!--", "?xml", "![CDATA[", "!DOCTYPE"]);
    }

    [Fact]
    public void DoesNotRewriteSpecialTagTagHelpers2()
    {
        RunParseTreeRewriterTest("""
            <foo><!-- @foo --></foo>
            """,
            tagNames: ["!--", "?xml", "![CDATA[", "!DOCTYPE"]);
    }

    [Fact]
    public void DoesNotRewriteSpecialTagTagHelpers3()
    {
        RunParseTreeRewriterTest("""
            <foo><?xml Hello World ?></foo>
            """,
            tagNames: ["!--", "?xml", "![CDATA[", "!DOCTYPE"]);
    }

    [Fact]
    public void DoesNotRewriteSpecialTagTagHelpers4()
    {
        RunParseTreeRewriterTest("""
            <foo><?xml @foo ?></foo>
            """,
            tagNames: ["!--", "?xml", "![CDATA[", "!DOCTYPE"]);
    }

    [Fact]
    public void DoesNotRewriteSpecialTagTagHelpers5()
    {
        RunParseTreeRewriterTest("""
            <foo><!DOCTYPE @foo ></foo>
            """,
            tagNames: ["!--", "?xml", "![CDATA[", "!DOCTYPE"]);
    }

    [Fact]
    public void DoesNotRewriteSpecialTagTagHelpers6()
    {
        RunParseTreeRewriterTest("""
            <foo><!DOCTYPE hello="world" ></foo>
            """,
            tagNames: ["!--", "?xml", "![CDATA[", "!DOCTYPE"]);
    }

    [Fact]
    public void DoesNotRewriteSpecialTagTagHelpers7()
    {
        RunParseTreeRewriterTest("""
            <foo><![CDATA[ Hello World ]]></foo>
            """,
            tagNames: ["!--", "?xml", "![CDATA[", "!DOCTYPE"]);
    }

    [Fact]
    public void DoesNotRewriteSpecialTagTagHelpers8()
    {
        RunParseTreeRewriterTest("""
            <foo><![CDATA[ @foo ]]></foo>
            """,
            tagNames: ["!--", "?xml", "![CDATA[", "!DOCTYPE"]);
    }

    [Fact]
    public void RewritesNestedTagHelperTagBlocks1()
    {
        RunParseTreeRewriterTest("""
            <p><div></div></p>
            """,
            tagNames: ["p", "div"]);
    }

    [Fact]
    public void RewritesNestedTagHelperTagBlocks2()
    {
        RunParseTreeRewriterTest("""
            <p>Hello World <div></div></p>
            """,
            tagNames: ["p", "div"]);
    }

    [Fact]
    public void RewritesNestedTagHelperTagBlocks3()
    {
        RunParseTreeRewriterTest("""
            <p>Hel<p>lo</p></p> <p><div>World</div></p>
            """,
            tagNames: ["p", "div"]);
    }

    [Fact]
    public void RewritesNestedTagHelperTagBlocks4()
    {
        RunParseTreeRewriterTest("""
            <p>Hel<strong>lo</strong></p> <p><span>World</span></p>
            """,
            tagNames: ["p", "div"]);
    }

    [Fact]
    public void HandlesMalformedNestedNonTagHelperTags_Correctly()
    {
        RunParseTreeRewriterTest("""
            <div>@{</div>}
            """);
    }

    [Fact]
    public void HandlesNonTagHelperStartAndEndVoidTags_Correctly()
    {
        RunParseTreeRewriterTest("""
            <input>Foo</input>
            """);
    }

    public static readonly TagHelperCollection CaseSensitive_TagHelpers =
    [
        TagHelperDescriptorBuilder.CreateTagHelper("pTagHelper", "SomeAssembly")
            .SetCaseSensitive()
            .BoundAttributeDescriptor(attribute =>
                attribute
                .Name("bound")
                .PropertyName("Bound")
                .TypeName(typeof(bool).FullName))
            .TagMatchingRuleDescriptor(rule =>
                rule
                .RequireTagName("p")
                .RequireAttributeDescriptor(attribute => attribute.Name("class")))
            .Build(),
        TagHelperDescriptorBuilder.CreateTagHelper("catchAllTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule =>
                rule
                .RequireTagName("*")
                .RequireAttributeDescriptor(attribute => attribute.Name("catchAll")))
            .Build(),
    ];

    [Fact]
    public void HandlesCaseSensitiveTagHelpersCorrectly1()
    {
        EvaluateData(CaseSensitive_TagHelpers, """
            <p class='foo' catchAll></p>
            """);
    }

    [Fact]
    public void HandlesCaseSensitiveTagHelpersCorrectly2()
    {
        EvaluateData(CaseSensitive_TagHelpers, """
            <p CLASS='foo' CATCHAll></p>
            """);
    }

    [Fact]
    public void HandlesCaseSensitiveTagHelpersCorrectly3()
    {
        EvaluateData(CaseSensitive_TagHelpers, """
            <P class='foo' CATCHAll></P>
            """);
    }

    [Fact]
    public void HandlesCaseSensitiveTagHelpersCorrectly4()
    {
        EvaluateData(CaseSensitive_TagHelpers, """
            <P class='foo'></P>
            """);
    }

    [Fact]
    public void HandlesCaseSensitiveTagHelpersCorrectly5()
    {
        EvaluateData(CaseSensitive_TagHelpers, """
            <p Class='foo'></p>
            """);
    }
}
