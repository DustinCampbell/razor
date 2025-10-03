// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class TagHelperBinderTest
{
    [Fact]
    public void GetBinding_ReturnsBindingWithInformation()
    {
        // Arrange
        var divTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();
        TagHelperCollection expectedDescriptors = [divTagHelper];
        var expectedAttributes = ImmutableArray.Create(
            new KeyValuePair<string, string>("class", "something"));
        var tagHelperBinder = new TagHelperBinder("th:", expectedDescriptors);

        // Act
        var bindingResult = tagHelperBinder.GetBinding(
            tagName: "th:div",
            attributes: expectedAttributes,
            parentTagName: "body",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(bindingResult);
        Assert.Equal<TagHelperDescriptor>(expectedDescriptors, bindingResult.TagHelpers);
        Assert.Equal("th:div", bindingResult.TagName);
        Assert.Equal("body", bindingResult.ParentTagName);
        Assert.Equal<KeyValuePair<string, string>>(expectedAttributes, bindingResult.Attributes);
        Assert.Equal("th:", bindingResult.TagNamePrefix);
        Assert.Equal<TagMatchingRuleDescriptor>(divTagHelper.TagMatchingRules, bindingResult.GetBoundRules(divTagHelper));
    }

    [Fact]
    public void GetBinding_With_Multiple_TagNameRules_SingleHelper()
    {
        // Arrange
        var multiTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("MultiTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("a"))
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("img"))
            .Build();
        TagHelperCollection expectedDescriptors = [multiTagHelper];
        var tagHelperBinder = new TagHelperBinder("", expectedDescriptors);

        TestTagName("div", multiTagHelper.TagMatchingRules[0]);
        TestTagName("a", multiTagHelper.TagMatchingRules[1]);
        TestTagName("img", multiTagHelper.TagMatchingRules[2]);
        TestTagName("p", null);
        TestTagName("*", null);

        void TestTagName(string tagName, TagMatchingRuleDescriptor? expectedBindingResult)
        {
            // Act
            var bindingResult = tagHelperBinder.GetBinding(
                tagName: tagName,
                attributes: [],
                parentTagName: "body",
                parentIsTagHelper: false);

            // Assert
            if (expectedBindingResult == null)
            {
                Assert.Null(bindingResult);
                return;
            }
            else
            {
                Assert.NotNull(bindingResult);
                Assert.Equal<TagHelperDescriptor>(expectedDescriptors, bindingResult.TagHelpers);

                Assert.Equal(tagName, bindingResult.TagName);
                var mapping = Assert.Single(bindingResult.GetBoundRules(multiTagHelper));
                Assert.Equal(expectedBindingResult, mapping);
            }
        }
    }

    [Fact]
    public void GetBinding_With_Multiple_TagNameRules_MultipleHelpers()
    {
        // Arrange
        var multiTagHelper1 = TagHelperDescriptorBuilder.CreateTagHelper("MultiTagHelper1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("a"))
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("img"))
            .Build();

        var multiTagHelper2 = TagHelperDescriptorBuilder.CreateTagHelper("MultiTagHelper2", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("table"))
            .Build();

        var tagHelperBinder = new TagHelperBinder("", [multiTagHelper1, multiTagHelper2]);

        TestTagName("div", [multiTagHelper1, multiTagHelper2], [multiTagHelper1.TagMatchingRules[0], multiTagHelper2.TagMatchingRules[0]]);
        TestTagName("a", [multiTagHelper1], [multiTagHelper1.TagMatchingRules[1]]);
        TestTagName("img", [multiTagHelper1], [multiTagHelper1.TagMatchingRules[2]]);
        TestTagName("p", [multiTagHelper2], [multiTagHelper2.TagMatchingRules[1]]);
        TestTagName("table", [multiTagHelper2], [multiTagHelper2.TagMatchingRules[2]]);
        TestTagName("*", null, null);


        void TestTagName(string tagName, TagHelperDescriptor[]? expectedDescriptors, TagMatchingRuleDescriptor[]? expectedBindingResults)
        {
            // Act
            var bindingResult = tagHelperBinder.GetBinding(
                tagName: tagName,
                attributes: [],
                parentTagName: "body",
                parentIsTagHelper: false);

            // Assert
            if (expectedDescriptors is null)
            {
                Assert.Null(bindingResult);
            }
            else
            {
                Assert.NotNull(bindingResult);
                Assert.Equal(expectedDescriptors, bindingResult.TagHelpers);

                Assert.Equal(tagName, bindingResult.TagName);

                Assert.NotNull(expectedBindingResults);

                for (var i = 0; i < expectedDescriptors.Length; i++)
                {
                    var mapping = Assert.Single(bindingResult.GetBoundRules(expectedDescriptors[i]));
                    Assert.Equal(expectedBindingResults[i], mapping);
                }
            }
        }
    }

    public static TheoryData<string, string, TagHelperCollection, TagHelperCollection> RequiredParentData
    {
        get
        {
            var strongPDivParent = TagHelperDescriptorBuilder.CreateTagHelper("StrongTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("strong")
                    .RequireParentTag("p"))
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("strong")
                    .RequireParentTag("div"))
                .Build();
            var catchAllPParent = TagHelperDescriptorBuilder.CreateTagHelper("CatchAllTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("*")
                    .RequireParentTag("p"))
                .Build();

            // tagName, parentTagName, availableTagHelpers, expectedTagHelpers
            return new()
                {
                    {
                        "strong",
                        "p",
                        [strongPDivParent],
                        [strongPDivParent]
                    },
                    {
                        "strong",
                        "div",
                        [strongPDivParent, catchAllPParent],
                        [strongPDivParent]
                    },
                    {
                        "strong",
                        "p",
                        [strongPDivParent, catchAllPParent],
                        [strongPDivParent, catchAllPParent]
                    },
                    {
                        "custom",
                        "p",
                        [strongPDivParent, catchAllPParent],
                        [catchAllPParent]
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(RequiredParentData))]
    public void GetBinding_ReturnsBindingResultWithDescriptorsParentTags(
        string tagName,
        string parentTagName,
        TagHelperCollection availableTagHelpers,
        TagHelperCollection expectedTagHelpers)
    {
        // Arrange
        var tagHelperBinder = new TagHelperBinder(null, availableTagHelpers);

        // Act
        var bindingResult = tagHelperBinder.GetBinding(
            tagName,
            attributes: [],
            parentTagName: parentTagName,
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(bindingResult);
        Assert.Equal<TagHelperDescriptor>(expectedTagHelpers, bindingResult.TagHelpers);
    }

    public static TheoryData<string, ImmutableArray<KeyValuePair<string, string>>, TagHelperCollection, TagHelperCollection?> RequiredAttributeData
    {
        get
        {
            var divDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("div")
                    .RequireAttributeDescriptor(attribute => attribute.Name("style")))
                .Build();
            var inputDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("InputTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("input")
                    .RequireAttributeDescriptor(attribute => attribute.Name("class"))
                    .RequireAttributeDescriptor(attribute => attribute.Name("style")))
                .Build();
            var inputWildcardPrefixDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("InputWildCardAttribute", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("input")
                    .RequireAttributeDescriptor(attribute => attribute
                        .Name("nodashprefix", RequiredAttributeNameComparison.PrefixMatch)))
                .Build();
            var catchAllDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("CatchAllTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName(TagHelperMatchingConventions.ElementCatchAllName)
                    .RequireAttributeDescriptor(attribute => attribute.Name("class")))
                .Build();
            var catchAllDescriptor2 = TagHelperDescriptorBuilder.CreateTagHelper("CatchAllTagHelper2", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName(TagHelperMatchingConventions.ElementCatchAllName)
                    .RequireAttributeDescriptor(attribute => attribute.Name("custom"))
                    .RequireAttributeDescriptor(attribute => attribute.Name("class")))
                .Build();
            var catchAllWildcardPrefixDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("CatchAllWildCardAttribute", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName(TagHelperMatchingConventions.ElementCatchAllName)
                    .RequireAttributeDescriptor(attribute => attribute
                        .Name("prefix-", RequiredAttributeNameComparison.PrefixMatch)))
                .Build();
            TagHelperCollection defaultAvailableDescriptors =
                [divDescriptor, inputDescriptor, catchAllDescriptor, catchAllDescriptor2];
            TagHelperCollection defaultWildcardDescriptors =
                [inputWildcardPrefixDescriptor, catchAllWildcardPrefixDescriptor];
            Func<string, KeyValuePair<string, string>> kvp =
                (name) => KeyValuePair.Create(name, "test value");

            // tagName, providedAttribute, availableTagHelpers, expectedTagHelpers
            return new()
                {
                    {
                        "div",
                        ImmutableArray.Create(kvp("custom")),
                        defaultAvailableDescriptors,
                        null
                    },
                    { "div", ImmutableArray.Create(kvp("style")), defaultAvailableDescriptors, [divDescriptor] },
                    { "div", ImmutableArray.Create(kvp("class")), defaultAvailableDescriptors, [catchAllDescriptor] },
                    {
                        "div",
                        ImmutableArray.Create(kvp("class"), kvp("style")),
                        defaultAvailableDescriptors,
                        [divDescriptor, catchAllDescriptor]
                    },
                    {
                        "div",
                        ImmutableArray.Create(kvp("class"), kvp("style"), kvp("custom")),
                        defaultAvailableDescriptors,
                        [divDescriptor, catchAllDescriptor, catchAllDescriptor2]
                    },
                    {
                        "input",
                        ImmutableArray.Create(kvp("class"), kvp("style")),
                        defaultAvailableDescriptors,
                        [inputDescriptor, catchAllDescriptor]
                    },
                    {
                        "input",
                        ImmutableArray.Create(kvp("nodashprefixA")),
                        defaultWildcardDescriptors,
                        [inputWildcardPrefixDescriptor]
                    },
                    {
                        "input",
                        ImmutableArray.Create(kvp("nodashprefix-ABC-DEF"), kvp("random")),
                        defaultWildcardDescriptors,
                        [inputWildcardPrefixDescriptor]
                    },
                    {
                        "input",
                        ImmutableArray.Create(kvp("prefixABCnodashprefix")),
                        defaultWildcardDescriptors,
                        null
                    },
                    {
                        "input",
                        ImmutableArray.Create(kvp("prefix-")),
                        defaultWildcardDescriptors,
                        null
                    },
                    {
                        "input",
                        ImmutableArray.Create(kvp("nodashprefix")),
                        defaultWildcardDescriptors,
                        null
                    },
                    {
                        "input",
                        ImmutableArray.Create(kvp("prefix-A")),
                        defaultWildcardDescriptors,
                        [catchAllWildcardPrefixDescriptor]
                    },
                    {
                        "input",
                        ImmutableArray.Create(kvp("prefix-ABC-DEF"), kvp("random")),
                        defaultWildcardDescriptors,
                        [catchAllWildcardPrefixDescriptor]
                    },
                    {
                        "input",
                        ImmutableArray.Create(kvp("prefix-abc"), kvp("nodashprefix-def")),
                        defaultWildcardDescriptors,
                        [inputWildcardPrefixDescriptor, catchAllWildcardPrefixDescriptor]
                    },
                    {
                        "input",
                        ImmutableArray.Create(kvp("class"), kvp("prefix-abc"), kvp("onclick"), kvp("nodashprefix-def"), kvp("style")),
                        defaultWildcardDescriptors,
                        [inputWildcardPrefixDescriptor, catchAllWildcardPrefixDescriptor]
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(RequiredAttributeData))]
    public void GetBinding_ReturnsBindingResultDescriptorsWithRequiredAttributes(
        string tagName,
        ImmutableArray<KeyValuePair<string, string>> providedAttributes,
        TagHelperCollection availableTagHelpers,
        TagHelperCollection? expectedTagHelpers)
    {
        // Arrange
        var tagHelperBinder = new TagHelperBinder(null, availableTagHelpers);

        // Act
        var bindingResult = tagHelperBinder.GetBinding(tagName, providedAttributes, parentTagName: "p", parentIsTagHelper: false);
        var tagHelpers = bindingResult?.TagHelpers;

        // Assert
        if (expectedTagHelpers is null)
        {
            Assert.Null(tagHelpers);
        }
        else
        {
            Assert.Equal<TagHelperDescriptor>(expectedTagHelpers, tagHelpers);
        }
    }

    [Fact]
    public void GetBinding_ReturnsNullBindingResultPrefixAsTagName()
    {
        // Arrange
        var catchAllDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName(TagHelperMatchingConventions.ElementCatchAllName))
            .Build();
        var tagHelperBinder = new TagHelperBinder("th", [catchAllDescriptor]);

        // Act
        var bindingResult = tagHelperBinder.GetBinding(
            tagName: "th",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.Null(bindingResult);
    }

    [Fact]
    public void GetBinding_ReturnsBindingResultCatchAllDescriptorsForPrefixedTags()
    {
        // Arrange
        var catchAllDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName(TagHelperMatchingConventions.ElementCatchAllName))
            .Build();
        var tagHelperBinder = new TagHelperBinder("th:", [catchAllDescriptor]);

        // Act
        var bindingResultDiv = tagHelperBinder.GetBinding(
            tagName: "th:div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);
        var bindingResultSpan = tagHelperBinder.GetBinding(
            tagName: "th:span",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(bindingResultDiv);
        var descriptor = Assert.Single(bindingResultDiv.TagHelpers);
        Assert.Same(catchAllDescriptor, descriptor);

        Assert.NotNull(bindingResultSpan);
        descriptor = Assert.Single(bindingResultSpan.TagHelpers);
        Assert.Same(catchAllDescriptor, descriptor);
    }

    [Fact]
    public void GetBinding_ReturnsBindingResultDescriptorsForPrefixedTags()
    {
        // Arrange
        var divDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();
        var tagHelperBinder = new TagHelperBinder("th:", [divDescriptor]);

        // Act
        var bindingResult = tagHelperBinder.GetBinding(
            tagName: "th:div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(bindingResult);
        var descriptor = Assert.Single(bindingResult.TagHelpers);
        Assert.Same(divDescriptor, descriptor);
    }

    [Theory]
    [InlineData("*")]
    [InlineData("div")]
    public void GetBinding_ReturnsNullForUnprefixedTags(string tagName)
    {
        // Arrange
        var divDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName(tagName))
            .Build();
        var tagHelperBinder = new TagHelperBinder("th:", [divDescriptor]);

        // Act
        var bindingResult = tagHelperBinder.GetBinding(
            tagName: "div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.Null(bindingResult);
    }

    [Fact]
    public void GetDescriptors_ReturnsNothingForUnregisteredTags()
    {
        // Arrange
        var divDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();
        var spanDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("foo2", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("span"))
            .Build();
        var tagHelperBinder = new TagHelperBinder(null, [divDescriptor, spanDescriptor]);

        // Act
        var tagHelperBinding = tagHelperBinder.GetBinding(
            tagName: "foo",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.Null(tagHelperBinding);
    }

    [Fact]
    public void GetDescriptors_ReturnsCatchAllsWithEveryTagName()
    {
        // Arrange
        var divDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();
        var spanDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("foo2", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("span"))
            .Build();
        var catchAllDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("foo3", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName(TagHelperMatchingConventions.ElementCatchAllName))
            .Build();
        var tagHelperBinder = new TagHelperBinder(null, [divDescriptor, spanDescriptor, catchAllDescriptor]);

        // Act
        var divBinding = tagHelperBinder.GetBinding(
            tagName: "div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);
        var spanBinding = tagHelperBinder.GetBinding(
            tagName: "span",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        // For divs
        Assert.NotNull(divBinding);
        Assert.Equal(2, divBinding.TagHelpers.Count);
        Assert.Contains(divDescriptor, divBinding.TagHelpers);
        Assert.Contains(catchAllDescriptor, divBinding.TagHelpers);

        // For spans
        Assert.NotNull(spanBinding);
        Assert.Equal(2, spanBinding.TagHelpers.Count);
        Assert.Contains(spanDescriptor, spanBinding.TagHelpers);
        Assert.Contains(catchAllDescriptor, spanBinding.TagHelpers);
    }

    [Fact]
    public void GetDescriptors_DuplicateDescriptorsAreNotPartOfTagHelperDescriptorPool()
    {
        // Arrange
        var divDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();

        var tagHelperBinder = new TagHelperBinder(null, [divDescriptor, divDescriptor]);

        // Act
        var bindingResult = tagHelperBinder.GetBinding(
            tagName: "div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(bindingResult);
        var descriptor = Assert.Single(bindingResult.TagHelpers);
        Assert.Same(divDescriptor, descriptor);
    }

    [Fact]
    public void GetBinding_DescriptorWithMultipleRules_CorrectlySelectsMatchingRules()
    {
        // Arrange
        var multiRuleDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("foo", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule
                .RequireTagName(TagHelperMatchingConventions.ElementCatchAllName)
                .RequireParentTag("body"))
            .TagMatchingRuleDescriptor(rule => rule
                .RequireTagName("div"))
            .TagMatchingRuleDescriptor(rule => rule
                .RequireTagName("span"))
            .Build();

        var tagHelperBinder = new TagHelperBinder(null, [multiRuleDescriptor]);

        // Act
        var binding = tagHelperBinder.GetBinding(
            tagName: "div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(binding);
        var boundDescriptor = Assert.Single(binding.TagHelpers);
        Assert.Same(multiRuleDescriptor, boundDescriptor);
        var boundRules = binding.GetBoundRules(boundDescriptor);
        var boundRule = Assert.Single(boundRules);
        Assert.Equal("div", boundRule.TagName);
    }

    [Fact]
    public void GetBinding_PrefixedParent_ReturnsBinding()
    {
        // Arrange
        var divDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div").RequireParentTag("p"))
            .Build();
        var pDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("foo2", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
            .Build();

        var tagHelperBinder = new TagHelperBinder("th:", [divDescriptor, pDescriptor]);

        // Act
        var bindingResult = tagHelperBinder.GetBinding(
            tagName: "th:div",
            attributes: [],
            parentTagName: "th:p",
            parentIsTagHelper: true);

        // Assert
        Assert.NotNull(bindingResult);
        var boundDescriptor = Assert.Single(bindingResult.TagHelpers);
        Assert.Same(divDescriptor, boundDescriptor);
        var boundRules = bindingResult.GetBoundRules(boundDescriptor);
        var boundRule = Assert.Single(boundRules);
        Assert.Equal("div", boundRule.TagName);
        Assert.Equal("p", boundRule.ParentTag);
    }

    [Fact]
    public void GetBinding_IsAttributeMatch_SingleAttributeMatch()
    {
        // Arrange
        var divDescriptor = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .ClassifyAttributesOnly(true)
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();

        var tagHelperBinder = new TagHelperBinder("", [divDescriptor]);

        // Act
        var bindingResult = tagHelperBinder.GetBinding(
            tagName: "div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(bindingResult);
        Assert.True(bindingResult.IsAttributeMatch);
    }

    [Fact]
    public void GetBinding_IsAttributeMatch_MultipleAttributeMatches()
    {
        // Arrange
        var divDescriptor1 = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .ClassifyAttributesOnly(true)
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();

        var divDescriptor2 = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .ClassifyAttributesOnly(true)
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();

        var tagHelperBinder = new TagHelperBinder("", [divDescriptor1, divDescriptor2]);

        // Act
        var bindingResult = tagHelperBinder.GetBinding(
            tagName: "div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(bindingResult);
        Assert.True(bindingResult.IsAttributeMatch);
    }

    [Fact]
    public void GetBinding_IsAttributeMatch_MixedAttributeMatches()
    {
        // Arrange
        var divDescriptor1 = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .ClassifyAttributesOnly(true)
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();

        var divDescriptor2 = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();

        var tagHelperBinder = new TagHelperBinder("", [divDescriptor1, divDescriptor2]);

        // Act
        var bindingResult = tagHelperBinder.GetBinding(
            tagName: "div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(bindingResult);
        Assert.False(bindingResult.IsAttributeMatch);
    }

    [Fact]
    public void GetBinding_CaseSensitiveRule_CaseMismatch_ReturnsNull()
    {
        // Arrange
        var divTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .SetCaseSensitive()
            .Build();
        var tagHelperBinder = new TagHelperBinder("th:", [divTagHelper]);

        // Act
        var bindingResult = tagHelperBinder.GetBinding(
            tagName: "th:Div",
            attributes: [new KeyValuePair<string, string>("class", "something")],
            parentTagName: "body",
            parentIsTagHelper: false);

        // Assert
        Assert.Null(bindingResult);
    }

    [Fact]
    public void GetBinding_CaseSensitiveRequiredAttribute_CaseMismatch_ReturnsNull()
    {
        // Arrange
        var divTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule
                .RequireTagName("div")
                .RequireAttributeDescriptor(attribute => attribute.Name("class")))
            .SetCaseSensitive()
            .Build();

        var tagHelperBinder = new TagHelperBinder(null, [divTagHelper]);

        // Act
        var bindingResult = tagHelperBinder.GetBinding(
            tagName: "div",
            attributes: [KeyValuePair.Create("CLASS", "something")],
            parentTagName: "body",
            parentIsTagHelper: false);

        // Assert
        Assert.Null(bindingResult);
    }
}
