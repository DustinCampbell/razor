// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Editor.Razor;

public class TagHelperFactsTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    public void GetTagHelperBinding_DoesNotAllowOptOutCharacterPrefix()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.Create(string.Empty, tagHelpers);

        // Act
        var binding = TagHelperFacts.GetTagHelperBinding(documentContext, "!a", ImmutableArray<KeyValuePair<string, string>>.Empty, parentTag: null, parentIsTagHelper: false);

        // Assert
        Assert.Null(binding);
    }

    [Fact]
    public void GetTagHelperBinding_WorksAsExpected()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                        .RequireTagName("a")
                        .RequireAttributeDescriptor(attribute => attribute.Name("asp-for")))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                        .Name("asp-for")
                        .TypeName(typeof(string).FullName)
                        .PropertyName("AspFor"))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                        .Name("asp-route")
                        .TypeName(typeof(IDictionary<string, string>).Namespace + "IDictionary<string, string>")
                        .PropertyName("AspRoute")
                        .AsDictionaryAttribute("asp-route-", typeof(string).FullName))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("input"))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                        .Name("asp-for")
                        .TypeName(typeof(string).FullName)
                        .PropertyName("AspFor"))
                .Build(),
        ];

        var documentContext = TagHelperDocumentContext.Create(string.Empty, tagHelpers);
        var attributes = ImmutableArray.Create(
            new KeyValuePair<string, string>("asp-for", "Name"));

        // Act
        var binding = TagHelperFacts.GetTagHelperBinding(documentContext, "a", attributes, parentTag: "p", parentIsTagHelper: false);

        // Assert
        Assert.NotNull(binding);
        var descriptor = Assert.Single(binding.TagHelpers);
        Assert.Equal(tagHelpers[0], descriptor);
        var boundRule = Assert.Single(binding.GetBoundRules(descriptor));
        Assert.Equal(tagHelpers[0].TagMatchingRules.First(), boundRule);
    }

    [Fact]
    public void GetBoundTagHelperAttributes_MatchesPrefixedAttributeName()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("a"))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                        .Name("asp-for")
                        .TypeName(typeof(string).FullName)
                        .PropertyName("AspFor"))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                        .Name("asp-route")
                        .TypeName(typeof(IDictionary<string, string>).Namespace + "IDictionary<string, string>")
                        .PropertyName("AspRoute")
                        .AsDictionaryAttribute("asp-route-", typeof(string).FullName))
                .Build()
        ];

        var expectedAttributeDescriptors = new[]
        {
            tagHelpers[0].BoundAttributes.Last()
        };

        var documentContext = TagHelperDocumentContext.Create(string.Empty, tagHelpers);
        var binding = TagHelperFacts.GetTagHelperBinding(documentContext, "a", ImmutableArray<KeyValuePair<string, string>>.Empty, parentTag: null, parentIsTagHelper: false);

        // Act
        Assert.NotNull(binding);
        var tagHelperAttributes = TagHelperFacts.GetBoundTagHelperAttributes(documentContext, "asp-route-something", binding);

        // Assert
        Assert.Equal(expectedAttributeDescriptors, tagHelperAttributes);
    }

    [Fact]
    public void GetBoundTagHelperAttributes_MatchesAttributeName()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("input"))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                        .Name("asp-for")
                        .TypeName(typeof(string).FullName)
                        .PropertyName("AspFor"))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                        .Name("asp-extra")
                        .TypeName(typeof(string).FullName)
                        .PropertyName("AspExtra"))
                .Build()
        ];

        var expectedAttributeDescriptors = new[]
        {
            tagHelpers[0].BoundAttributes.First()
        };

        var documentContext = TagHelperDocumentContext.Create(string.Empty, tagHelpers);
        var binding = TagHelperFacts.GetTagHelperBinding(documentContext, "input", ImmutableArray<KeyValuePair<string, string>>.Empty, parentTag: null, parentIsTagHelper: false);

        // Act
        Assert.NotNull(binding);
        var tagHelperAttributes = TagHelperFacts.GetBoundTagHelperAttributes(documentContext, "asp-for", binding);

        // Assert
        Assert.Equal(expectedAttributeDescriptors, tagHelperAttributes);
    }

    [Fact]
    public void GetTagHelpersGivenTag_DoesNotAllowOptOutCharacterPrefix()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.Create(string.Empty, tagHelpers);

        // Act
        var results = TagHelperFacts.GetTagHelpersGivenTag(documentContext, "!strong", parentTag: null);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void GetTagHelpersGivenTag_RequiresTagName()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("strong"))
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.Create(string.Empty, tagHelpers);

        // Act
        var results = TagHelperFacts.GetTagHelpersGivenTag(documentContext, "strong", "p");

        // Assert
        Assert.Equal<TagHelperDescriptor>(tagHelpers, results);
    }

    [Fact]
    public void GetTagHelpersGivenTag_RestrictsTagHelpersBasedOnTagName()
    {
        // Arrange
        TagHelperCollection expectedTagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(
                    rule => rule
                        .RequireTagName("a")
                        .RequireParentTag("div"))
                .Build()
        ];

        TagHelperCollection tagHelpers =
        [
            expectedTagHelpers[0],
            TagHelperDescriptorBuilder.CreateTagHelper("TestType2", "TestAssembly")
                .TagMatchingRuleDescriptor(
                    rule => rule
                        .RequireTagName("strong")
                        .RequireParentTag("div"))
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.Create(string.Empty, tagHelpers);

        // Act
        var results = TagHelperFacts.GetTagHelpersGivenTag(documentContext, "a", "div");

        // Assert
        Assert.Equal<TagHelperDescriptor>(expectedTagHelpers, results);
    }

    [Fact]
    public void GetTagHelpersGivenTag_RestrictsTagHelpersBasedOnTagHelperPrefix()
    {
        // Arrange
        TagHelperCollection expectedTagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("strong"))
                .Build()
        ];

        TagHelperCollection tagHelpers =
        [
            expectedTagHelpers[0],
            TagHelperDescriptorBuilder.CreateTagHelper("TestType2", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("thstrong"))
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.Create("th", tagHelpers);

        // Act
        var results = TagHelperFacts.GetTagHelpersGivenTag(documentContext, "thstrong", "div");

        // Assert
        Assert.Equal<TagHelperDescriptor>(expectedTagHelpers, results);
    }

    [Fact]
    public void GetTagHelpersGivenTag_RestrictsTagHelpersBasedOnParent()
    {
        // Arrange
        TagHelperCollection expectedTagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(
                    rule => rule
                        .RequireTagName("strong")
                        .RequireParentTag("div"))
                .Build()
        ];

        TagHelperCollection tagHelpers =
        [
            expectedTagHelpers[0],
            TagHelperDescriptorBuilder.CreateTagHelper("TestType2", "TestAssembly")
                .TagMatchingRuleDescriptor(
                    rule => rule
                        .RequireTagName("strong")
                        .RequireParentTag("p"))
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.Create(string.Empty, tagHelpers);

        // Act
        var results = TagHelperFacts.GetTagHelpersGivenTag(documentContext, "strong", "div");

        // Assert
        Assert.Equal<TagHelperDescriptor>(expectedTagHelpers, results);
    }

    [Fact]
    public void GetTagHelpersGivenParent_AllowsRootParentTag()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.Create(string.Empty, tagHelpers);

        // Act
        var results = TagHelperFacts.GetTagHelpersGivenParent(documentContext, parentTag: null /* root */);

        // Assert
        Assert.Equal<TagHelperDescriptor>(tagHelpers, results);
    }

    [Fact]
    public void GetTagHelpersGivenParent_AllowsRootParentTagForParentRestrictedTagHelperDescriptors()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("p")
                    .RequireParentTag("body"))
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.Create(string.Empty, tagHelpers);

        // Act
        var results = TagHelperFacts.GetTagHelpersGivenParent(documentContext, parentTag: null /* root */);

        // Assert
        var descriptor = Assert.Single(results);
        Assert.Equal(tagHelpers[0], descriptor);
    }

    [Fact]
    public void GetTagHelpersGivenParent_AllowsUnspecifiedParentTagHelpers()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.Create(string.Empty, tagHelpers);

        // Act
        var results = TagHelperFacts.GetTagHelpersGivenParent(documentContext, "p");

        // Assert
        Assert.Equal<TagHelperDescriptor>(tagHelpers, results);
    }

    [Fact]
    public void GetTagHelpersGivenParent_RestrictsTagHelpersBasedOnParent()
    {
        // Arrange
        TagHelperCollection expectedTagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestType", "TestAssembly")
                .TagMatchingRuleDescriptor(
                    rule => rule
                        .RequireTagName("p")
                        .RequireParentTag("div"))
                .Build()
        ];

        TagHelperCollection tagHelpers =
        [
            expectedTagHelpers[0],
            TagHelperDescriptorBuilder.CreateTagHelper("TestType2", "TestAssembly")
                .TagMatchingRuleDescriptor(
                    rule => rule
                        .RequireTagName("strong")
                        .RequireParentTag("p"))
                .Build()
        ];

        var documentContext = TagHelperDocumentContext.Create(string.Empty, tagHelpers);

        // Act
        var results = TagHelperFacts.GetTagHelpersGivenParent(documentContext, "div");

        // Assert
        Assert.Equal<TagHelperDescriptor>(expectedTagHelpers, results);
    }
}
