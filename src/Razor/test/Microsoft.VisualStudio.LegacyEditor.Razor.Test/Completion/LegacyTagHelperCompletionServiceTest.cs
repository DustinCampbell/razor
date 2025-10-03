// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.Completion;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.LegacyEditor.Razor.Completion;

public class LegacyTagHelperCompletionServiceTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1452432")]
    public void GetAttributeCompletions_OnlyIndexerNamePrefix()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("FormTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("form"))
                .BoundAttributeDescriptor(attribute => attribute
                    .TypeName("System.Collections.Generic.IDictionary<System.String, System.String>")
                    .PropertyName("RouteValues")
                    .AsDictionary("asp-route-", typeof(string).FullName))
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["asp-route-..."] = [tagHelpers[0].BoundAttributes.Last()]
        });

        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions: [],
            attributes: [],
            currentTagName: "form");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetAttributeCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_BoundDictionaryAttribute_ReturnsPrefixIndexerAndFullSetter()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("FormTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("form"))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("asp-all-route-data")
                    .TypeName("System.Collections.Generic.IDictionary<System.String, System.String>")
                    .PropertyName("RouteValues")
                    .AsDictionary("asp-route-", typeof(string).FullName))
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["asp-all-route-data"] = [tagHelpers[0].BoundAttributes.Last()],
            ["asp-route-..."] = [tagHelpers[0].BoundAttributes.Last()]
        });

        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions: [],
            attributes: [],
            currentTagName: "form");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetAttributeCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_RequiredBoundDictionaryAttribute_ReturnsPrefixIndexerAndFullSetter()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("FormTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("form")
                    .RequireAttributeDescriptor(builder =>
                    {
                        builder.Name = "asp-route-";
                        builder.NameComparison = RequiredAttributeNameComparison.PrefixMatch;
                    }))
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("form")
                    .RequireAttributeDescriptor(builder => builder.Name = "asp-all-route-data"))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("asp-all-route-data")
                    .TypeName("System.Collections.Generic.IDictionary<System.String, System.String>")
                    .PropertyName("RouteValues")
                    .AsDictionary("asp-route-", typeof(string).FullName))
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["asp-all-route-data"] = [tagHelpers[0].BoundAttributes.Last()],
            ["asp-route-..."] = [tagHelpers[0].BoundAttributes.Last()]
        });

        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions: [],
            attributes: [],
            currentTagName: "form");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetAttributeCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_DoesNotReturnCompletionsForAlreadySuppliedAttributes()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("div")
                    .RequireAttributeDescriptor(attribute => attribute.Name("repeat")))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("visible")
                    .TypeName(typeof(bool).FullName)
                    .PropertyName("Visible"))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("StyleTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("class")
                    .TypeName(typeof(string).FullName)
                    .PropertyName("Class"))
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["onclick"] = [],
            ["visible"] = [tagHelpers[0].BoundAttributes.Last()]
        });

        var existingCompletions = new[] { "onclick" };
        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions,
            attributes: [
                KeyValuePair.Create("class", "something"),
                KeyValuePair.Create("repeat", "4")],
            currentTagName: "div");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetAttributeCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_ReturnsCompletionForAlreadySuppliedAttribute_IfCurrentAttributeMatches()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("div")
                    .RequireAttributeDescriptor(attribute => attribute.Name("repeat")))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("visible")
                    .TypeName(typeof(bool).FullName)
                    .PropertyName("Visible"))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("StyleTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("class")
                    .TypeName(typeof(string).FullName)
                    .PropertyName("Class"))
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["onclick"] = [],
            ["visible"] = [tagHelpers[0].BoundAttributes.Last()]
        });

        var existingCompletions = new[] { "onclick" };
        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions,
            attributes: [
                KeyValuePair.Create("class", "something"),
                KeyValuePair.Create("repeat", "4"),
                KeyValuePair.Create("visible", "false")],
            currentTagName: "div",
            currentAttributeName: "visible");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetAttributeCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_DoesNotReturnAlreadySuppliedAttribute_IfCurrentAttributeDoesNotMatch()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("div")
                    .RequireAttributeDescriptor(attribute => attribute.Name("repeat")))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("visible")
                    .TypeName(typeof(bool).FullName)
                    .PropertyName("Visible"))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("StyleTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("class")
                    .TypeName(typeof(string).FullName)
                    .PropertyName("Class"))
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["onclick"] = []
        });

        var existingCompletions = new[] { "onclick" };
        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions,
            attributes: [
                KeyValuePair.Create("class", "something"),
                KeyValuePair.Create("repeat", "4"),
                KeyValuePair.Create("visible", "false")],
            currentTagName: "div",
            currentAttributeName: "repeat");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetAttributeCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_PossibleDescriptorsReturnUnboundRequiredAttributesWithExistingCompletions()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("div")
                    .RequireAttributeDescriptor(attribute => attribute.Name("repeat")))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("StyleTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("*")
                    .RequireAttributeDescriptor(attribute => attribute.Name("class")))
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["class"] = [],
            ["onclick"] = [],
            ["repeat"] = []
        });

        var existingCompletions = new[] { "onclick", "class" };
        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions,
            currentTagName: "div");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetAttributeCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_PossibleDescriptorsReturnBoundRequiredAttributesWithExistingCompletions()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("div")
                    .RequireAttributeDescriptor(attribute => attribute.Name("repeat")))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("repeat")
                    .TypeName(typeof(bool).FullName)
                    .PropertyName("Repeat"))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("visible")
                    .TypeName(typeof(bool).FullName)
                    .PropertyName("Visible"))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("StyleTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("*")
                    .RequireAttributeDescriptor(attribute => attribute.Name("class")))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("class")
                    .TypeName(typeof(string).FullName)
                    .PropertyName("Class"))
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["class"] = [.. tagHelpers[1].BoundAttributes],
            ["onclick"] = [],
            ["repeat"] = [tagHelpers[0].BoundAttributes.First()]
        });

        var existingCompletions = new[] { "onclick" };
        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions,
            currentTagName: "div");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetAttributeCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_AppliedDescriptorsReturnAllBoundAttributesWithExistingCompletionsForSchemaTags()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("repeat")
                    .TypeName(typeof(bool).FullName)
                    .PropertyName("Repeat"))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("visible")
                    .TypeName(typeof(bool).FullName)
                    .PropertyName("Visible"))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("StyleTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("*")
                    .RequireAttributeDescriptor(attribute => attribute.Name("class")))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("class")
                    .TypeName(typeof(string).FullName)
                    .PropertyName("Class"))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("StyleTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("visible")
                    .TypeName(typeof(bool).FullName)
                    .PropertyName("Visible"))
                .Build(),
        ];
        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["onclick"] = [],
            ["class"] = [.. tagHelpers[1].BoundAttributes],
            ["repeat"] = [tagHelpers[0].BoundAttributes.First()],
            ["visible"] = [tagHelpers[0].BoundAttributes.Last(), tagHelpers[2].BoundAttributes.First()]
        });

        var existingCompletions = new[] { "class", "onclick" };
        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions,
            currentTagName: "div");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetAttributeCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_AppliedTagOutputHintDescriptorsReturnBoundAttributesWithExistingCompletionsForNonSchemaTags()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("CustomTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("custom"))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("repeat")
                    .TypeName(typeof(bool).FullName)
                    .PropertyName("Repeat"))
                .TagOutputHint("div")
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["class"] = [],
            ["repeat"] = [.. tagHelpers[0].BoundAttributes]
        });

        var existingCompletions = new[] { "class" };
        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions,
            currentTagName: "custom");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetAttributeCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_AppliedDescriptorsReturnBoundAttributesCompletionsForNonSchemaTags()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("CustomTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("custom"))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("repeat")
                    .TypeName(typeof(bool).FullName)
                    .PropertyName("Repeat"))
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["repeat"] = [.. tagHelpers[0].BoundAttributes]
        });

        var existingCompletions = new[] { "class" };
        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions,
            currentTagName: "custom");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetAttributeCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_AppliedDescriptorsReturnBoundAttributesWithExistingCompletionsForSchemaTags()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("repeat")
                    .TypeName(typeof(bool).FullName)
                    .PropertyName("Repeat"))
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["class"] = [],
            ["repeat"] = [.. tagHelpers[0].BoundAttributes]
        });

        var existingCompletions = new[] { "class" };
        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions,
            currentTagName: "div");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetAttributeCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_NoDescriptorsReturnsExistingCompletions()
    {
        // Arrange
        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["class"] = [],
        });

        var existingCompletions = new[] { "class" };
        var completionContext = BuildAttributeCompletionContext(
            tagHelpers: [],
            existingCompletions,
            currentTagName: "div");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetAttributeCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_NoDescriptorsForUnprefixedTagReturnsExistingCompletions()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("div")
                    .RequireAttributeDescriptor(attribute => attribute.Name("special")))
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["class"] = [],
        });

        var existingCompletions = new[] { "class" };
        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions,
            currentTagName: "div",
            tagHelperPrefix: "th:");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetAttributeCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetAttributeCompletions_NoDescriptorsForTagReturnsExistingCompletions()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("MyTableTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("table")
                    .RequireAttributeDescriptor(attribute => attribute.Name("special")))
                .Build(),
        ];

        var expectedCompletions = AttributeCompletionResult.Create(new()
        {
            ["class"] = [],
        });

        var existingCompletions = new[] { "class" };
        var completionContext = BuildAttributeCompletionContext(
            tagHelpers,
            existingCompletions,
            currentTagName: "div");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetAttributeCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_IgnoresDirectiveAttributes()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("BindAttribute", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("input"))
                .BoundAttributeDescriptor(builder =>
                {
                    builder.Name = "@bind";
                    builder.IsDirectiveAttribute = true;
                })
                .TagOutputHint("table")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["table"] = [],
        });

        var existingCompletions = new[] { "table" };
        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions,
            containingTagName: "body",
            containingParentTagName: null);
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetElementCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_FiltersFullyQualifiedElementsIfShortNameExists()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("Test"))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("TestAssembly.Test"))
                .IsFullyQualifiedNameMatch(true)
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("Test2TagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("Test2Assembly.Test"))
                .IsFullyQualifiedNameMatch(true)
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["Test"] = [tagHelpers[0]],
            ["Test2Assembly.Test"] = [tagHelpers[2]],
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: [],
            containingTagName: "body",
            containingParentTagName: null);
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetElementCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_TagOutputHintDoesNotFallThroughToSchemaCheck()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("MyTableTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("my-table"))
                .TagOutputHint("table")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("MyTrTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("my-tr"))
                .TagOutputHint("tr")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["my-table"] = [tagHelpers[0]],
            ["table"] = [],
        });

        var existingCompletions = new[] { "table" };
        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions,
            containingTagName: "body",
            containingParentTagName: null);
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetElementCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_CatchAllsOnlyApplyToCompletionsStartingWithPrefix()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("CatchAllTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("LiTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("li"))
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["th:li"] = [tagHelpers[1], tagHelpers[0]],
            ["li"] = [],
        });

        var existingCompletions = new[] { "li" };
        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions,
            containingTagName: "ul",
            tagHelperPrefix: "th:");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetElementCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_TagHelperPrefixIsPrependedToTagHelperCompletions()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("SuperLiTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("superli"))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("LiTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("li"))
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["th:superli"] = [tagHelpers[0]],
            ["th:li"] = [tagHelpers[1]],
            ["li"] = [],
        });

        var existingCompletions = new[] { "li" };
        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions,
            containingTagName: "ul",
            tagHelperPrefix: "th:");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetElementCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_IsCaseSensitive()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("MyliTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("myli"))
                .SetCaseSensitive()
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("MYLITagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("MYLI"))
                .SetCaseSensitive()
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["myli"] = [tagHelpers[0]],
            ["MYLI"] = [tagHelpers[1]],
            ["li"] = [],
        });

        var existingCompletions = new[] { "li" };
        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions,
            containingTagName: "ul",
            tagHelperPrefix: null);
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetElementCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_HTMLSchemaTagName_IsCaseSensitive()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("LITagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("LI"))
                .SetCaseSensitive()
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("LiTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("li"))
                .SetCaseSensitive()
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["LI"] = [tagHelpers[0]],
            ["li"] = [tagHelpers[1]],
        });

        var existingCompletions = new[] { "li" };
        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions,
            containingTagName: "ul",
            tagHelperPrefix: null);
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetElementCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_CatchAllsApplyToOnlyTagHelperCompletions()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("SuperLiTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("superli"))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("CatchAll", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["superli"] = [tagHelpers[0], tagHelpers[1]],
            ["li"] = [],
        });

        var existingCompletions = new[] { "li" };
        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions,
            containingTagName: "ul");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetElementCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_CatchAllsApplyToNonTagHelperCompletionsIfStartsWithTagHelperPrefix()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("SuperLiTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("superli"))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("CatchAll", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["th:superli"] = [tagHelpers[0], tagHelpers[1]],
            ["th:li"] = [tagHelpers[1]],
        });

        var existingCompletions = new[] { "th:li" };
        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions,
            containingTagName: "ul",
            tagHelperPrefix: "th:");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetElementCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_AllowsMultiTargetingTagHelpers()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("BoldTagHelper1", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("strong"))
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("b"))
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("bold"))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("BoldTagHelper2", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("strong"))
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["strong"] = [tagHelpers[0], tagHelpers[1]],
            ["b"] = [tagHelpers[0]],
            ["bold"] = [tagHelpers[0]],
        });

        var existingCompletions = new[] { "strong", "b", "bold" };
        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions,
            containingTagName: "ul");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetElementCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_CombinesDescriptorsOnExistingCompletions()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("LiTagHelper1", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("li"))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("LiTagHelper2", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("li"))
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["li"] = [tagHelpers[0], tagHelpers[1]],
        });

        var existingCompletions = new[] { "li" };
        var completionContext = BuildElementCompletionContext(tagHelpers, existingCompletions, containingTagName: "ul");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetElementCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_NewCompletionsForSchemaTagsNotInExistingCompletionsAreIgnored()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("SuperLiTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("superli"))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("LiTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("li"))
                .TagOutputHint("strong")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["li"] = [tagHelpers[1]],
            ["superli"] = [tagHelpers[0]],
        });

        var existingCompletions = new[] { "li" };
        var completionContext = BuildElementCompletionContext(tagHelpers, existingCompletions, containingTagName: "ul");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetElementCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_OutputHintIsCrossReferencedWithExistingCompletions()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
                .TagOutputHint("li")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("LiTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("li"))
                .TagOutputHint("strong")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["div"] = [tagHelpers[0]],
            ["li"] = [tagHelpers[1]],
        });

        var existingCompletions = new[] { "li" };
        var completionContext = BuildElementCompletionContext(tagHelpers, existingCompletions, containingTagName: "ul");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetElementCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_EnsuresDescriptorsHaveSatisfiedParent()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("LiTagHelper1", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("li"))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("LiTagHelper2", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("li").RequireParentTag("ol"))
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["li"] = [tagHelpers[0]],
        });

        var existingCompletions = new[] { "li" };
        var completionContext = BuildElementCompletionContext(tagHelpers, existingCompletions, containingTagName: "ul");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetElementCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_NoContainingParentTag_DoesNotGetCompletionForRuleWithParentTag()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("Tag1", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("outer-child-tag"))
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("child-tag").RequireParentTag("parent-tag"))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("Tag2", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("parent-tag"))
                .AllowChildTag("child-tag")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["outer-child-tag"] = [tagHelpers[0]],
            ["parent-tag"] = [tagHelpers[1]],
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: [],
            containingTagName: null,
            containingParentTagName: null);
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetElementCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_WithContainingParentTag_GetsCompletionForRuleWithParentTag()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("Tag1", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("outer-child-tag"))
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("child-tag").RequireParentTag("parent-tag"))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("Tag2", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("parent-tag"))
                .AllowChildTag("child-tag")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["child-tag"] = [tagHelpers[0]],
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: [],
            containingTagName: "parent-tag",
            containingParentTagName: null);
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetElementCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_AllowedChildrenAreIgnoredWhenAtRoot()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("CatchAll", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
                .AllowChildTag("b")
                .AllowChildTag("bold")
                .AllowChildTag("div")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create([]);

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: [],
            containingTagName: null,
            containingParentTagName: null);
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetElementCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_DoesNotReturnExistingCompletionsWhenAllowedChildren()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("BoldParent", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
                .AllowChildTag("b")
                .AllowChildTag("bold")
                .AllowChildTag("div")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["b"] = [],
            ["bold"] = [],
            ["div"] = [tagHelpers[0]]
        });

        var existingCompletions = new[] { "p", "em" };
        var completionContext = BuildElementCompletionContext(tagHelpers, existingCompletions, containingTagName: "div", containingParentTagName: "thing");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetElementCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_CapturesAllAllowedChildTagsFromParentTagHelpers_NoneTagHelpers()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("BoldParent", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
                .AllowChildTag("b")
                .AllowChildTag("bold")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["b"] = [],
            ["bold"] = [],
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: [],
            containingTagName: "div",
            containingParentTagName: "");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetElementCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_CapturesAllAllowedChildTagsFromParentTagHelpers_SomeTagHelpers()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("BoldParent", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
                .AllowChildTag("b")
                .AllowChildTag("bold")
                .AllowChildTag("div")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["b"] = [],
            ["bold"] = [],
            ["div"] = [tagHelpers[0]]
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: [],
            containingTagName: "div",
            containingParentTagName: "");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetElementCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    [Fact]
    public void GetElementCompletions_CapturesAllAllowedChildTagsFromParentTagHelpers_AllTagHelpers()
    {
        // Arrange
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateTagHelper("BoldParentCatchAll", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
                .AllowChildTag("strong")
                .AllowChildTag("div")
                .AllowChildTag("b")
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("BoldParent", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
                .AllowChildTag("b")
                .AllowChildTag("bold")
                .Build(),
        ];

        var expectedCompletions = ElementCompletionResult.Create(new()
        {
            ["strong"] = [tagHelpers[0]],
            ["b"] = [tagHelpers[0]],
            ["bold"] = [tagHelpers[0]],
            ["div"] = [tagHelpers[0], tagHelpers[1]],
        });

        var completionContext = BuildElementCompletionContext(
            tagHelpers,
            existingCompletions: [],
            containingTagName: "div",
            containingParentTagName: "");
        var service = CreateTagHelperCompletionFactsService();

        // Act
        var completions = service.GetElementCompletions(completionContext);

        // Assert
        AssertCompletionsAreEquivalent(expectedCompletions, completions);
    }

    private static LegacyTagHelperCompletionService CreateTagHelperCompletionFactsService() => new();

    private static void AssertCompletionsAreEquivalent(ElementCompletionResult expected, ElementCompletionResult actual)
    {
        Assert.Equal(expected.Completions.Count, actual.Completions.Count);

        foreach (var expectedCompletion in expected.Completions)
        {
            var actualValue = actual.Completions[expectedCompletion.Key];
            Assert.NotNull(actualValue);
            Assert.Equal(expectedCompletion.Value, actualValue);
        }
    }

    private static void AssertCompletionsAreEquivalent(AttributeCompletionResult expected, AttributeCompletionResult actual)
    {
        Assert.Equal(expected.Completions.Count, actual.Completions.Count);

        foreach (var expectedCompletion in expected.Completions)
        {
            var actualValue = actual.Completions[expectedCompletion.Key];
            Assert.NotNull(actualValue);
            Assert.Equal(expectedCompletion.Value, actualValue);
        }
    }

    private static ElementCompletionContext BuildElementCompletionContext(
        TagHelperCollection tagHelpers,
        IEnumerable<string> existingCompletions,
        string? containingTagName,
        string? containingParentTagName = "body",
        bool containingParentIsTagHelper = false,
        string? tagHelperPrefix = "")
    {
        var documentContext = TagHelperDocumentContext.Create(tagHelperPrefix, tagHelpers);
        var completionContext = new ElementCompletionContext(
            documentContext,
            existingCompletions,
            containingTagName,
            attributes: [],
            containingParentTagName: containingParentTagName,
            containingParentIsTagHelper: containingParentIsTagHelper,
            inHTMLSchema: (tag) => tag == "strong" || tag == "b" || tag == "bold" || tag == "li" || tag == "div");

        return completionContext;
    }

    private static AttributeCompletionContext BuildAttributeCompletionContext(
        TagHelperCollection tagHelpers,
        IEnumerable<string> existingCompletions,
        string currentTagName,
        string? currentAttributeName = null,
        ImmutableArray<KeyValuePair<string, string>> attributes = default,
        string tagHelperPrefix = "")
    {
        attributes = attributes.NullToEmpty();

        var documentContext = TagHelperDocumentContext.Create(tagHelperPrefix, tagHelpers);
        var completionContext = new AttributeCompletionContext(
            documentContext,
            existingCompletions,
            currentTagName,
            currentAttributeName,
            attributes,
            currentParentTagName: "body",
            currentParentIsTagHelper: false,
            inHTMLSchema: (tag) => tag == "strong" || tag == "b" || tag == "bold" || tag == "li" || tag == "div");

        return completionContext;
    }
}
