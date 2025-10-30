// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class TagHelperBinderTest
{
    private static KeyValuePair<string, string> KVP(string name)
        => KVP(name, "test value");

    private static KeyValuePair<string, string> KVP(string name, string value)
        => KeyValuePair.Create(name, value);

    [Fact]
    public void GetBinding_ReturnsBindingWithInformation()
    {
        // Arrange
        var divTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();
        var binder = new TagHelperBinder("th:", [divTagHelper]);

        // Act
        var binding = binder.GetBinding(
            tagName: "th:div",
            attributes: [KVP("class", "something")],
            parentTagName: "body",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(binding);
        var boundTagHelper = Assert.Single(binding.Descriptors);
        Assert.Same(divTagHelper, boundTagHelper);
        Assert.Equal("th:div", binding.TagName);
        Assert.Equal("body", binding.ParentTagName);
        Assert.Equal<KeyValuePair<string, string>>([KVP("class", "something")], binding.Attributes);
        Assert.Equal("th:", binding.TagNamePrefix);
        Assert.Equal<TagMatchingRuleDescriptor>(divTagHelper.TagMatchingRules, binding.GetBoundRules(divTagHelper));
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

        var binder = new TagHelperBinder("", [multiTagHelper]);

        TestTagName("div", multiTagHelper.TagMatchingRules[0]);
        TestTagName("a", multiTagHelper.TagMatchingRules[1]);
        TestTagName("img", multiTagHelper.TagMatchingRules[2]);
        TestTagName("p", null);
        TestTagName("*", null);

        void TestTagName(string tagName, TagMatchingRuleDescriptor? expectedBindingResult)
        {
            // Act
            var binding = binder.GetBinding(
                tagName: tagName,
                attributes: [],
                parentTagName: "body",
                parentIsTagHelper: false);

            // Assert
            if (expectedBindingResult is null)
            {
                Assert.Null(binding);
                return;
            }
            else
            {
                Assert.NotNull(binding);
                Assert.Equal<TagHelperDescriptor>([multiTagHelper], binding.Descriptors);

                Assert.Equal(tagName, binding.TagName);
                var mapping = Assert.Single(binding.GetBoundRules(multiTagHelper));
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

        var binder = new TagHelperBinder("", [multiTagHelper1, multiTagHelper2]);

        TestTagName("div", [multiTagHelper1, multiTagHelper2], [multiTagHelper1.TagMatchingRules[0], multiTagHelper2.TagMatchingRules[0]]);
        TestTagName("a", [multiTagHelper1], [multiTagHelper1.TagMatchingRules[1]]);
        TestTagName("img", [multiTagHelper1], [multiTagHelper1.TagMatchingRules[2]]);
        TestTagName("p", [multiTagHelper2], [multiTagHelper2.TagMatchingRules[1]]);
        TestTagName("table", [multiTagHelper2], [multiTagHelper2.TagMatchingRules[2]]);
        TestTagName("*", null, null);

        void TestTagName(string tagName, TagHelperDescriptor[]? expectedTagHelpers, TagMatchingRuleDescriptor[]? expectedMatchingRules)
        {
            // Act
            var binding = binder.GetBinding(
                tagName: tagName,
                attributes: [],
                parentTagName: "body",
                parentIsTagHelper: false);

            // Assert
            if (expectedTagHelpers is null)
            {
                Assert.Null(binding);
            }
            else
            {
                Assert.NotNull(binding);
                Assert.NotNull(expectedMatchingRules);

                Assert.Equal(expectedTagHelpers, binding.Descriptors);

                Assert.Equal(tagName, binding.TagName);

                for (var i = 0; i < expectedTagHelpers.Length; i++)
                {
                    var mapping = Assert.Single(binding.GetBoundRules(expectedTagHelpers[i]));
                    Assert.Equal(expectedMatchingRules[i], mapping);
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

            // string - tagName
            // string - parentTagName
            // TagHelperCollection - availableTagHelpers
            // TagHelperCollection - expectedTagHelpers

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
        var binder = new TagHelperBinder(null, availableTagHelpers);

        // Act
        var binding = binder.GetBinding(
            tagName,
            attributes: [],
            parentTagName,
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(binding);
        Assert.Equal(expectedTagHelpers, binding.Descriptors);
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

            TagHelperCollection defaultAvailableTagHelpers =
                [divDescriptor, inputDescriptor, catchAllDescriptor, catchAllDescriptor2];
            TagHelperCollection defaultWildcardTagHelpers =
                [inputWildcardPrefixDescriptor, catchAllWildcardPrefixDescriptor];

            // string - tagName
            // ImmutableArray<KeyValuePair<string, string>> - providedAttributes
            // TagHelperCollection - availableTagHelpers
            // TagHelperCollection? - expectedTagHelpers

            return new()
            {
                {
                    "div",
                    [KVP("custom")],
                    defaultAvailableTagHelpers,
                    null
                },
                {
                    "div",
                    [KVP("style")],
                    defaultAvailableTagHelpers,
                    [divDescriptor]
                },
                {
                    "div",
                    [KVP("class")],
                    defaultAvailableTagHelpers,
                    [catchAllDescriptor]
                },
                {
                    "div",
                    [KVP("class"), KVP("style")],
                    defaultAvailableTagHelpers,
                    [divDescriptor, catchAllDescriptor]
                },
                {
                    "div",
                    [KVP("class"), KVP("style"), KVP("custom")],
                    defaultAvailableTagHelpers,
                    [divDescriptor, catchAllDescriptor, catchAllDescriptor2]
                },
                {
                    "input",
                    [KVP("class"), KVP("style")],
                    defaultAvailableTagHelpers,
                    [inputDescriptor, catchAllDescriptor]
                },
                {
                    "input",
                    [KVP("nodashprefixA")],
                    defaultWildcardTagHelpers,
                    [inputWildcardPrefixDescriptor]
                },
                {
                    "input",
                    [KVP("nodashprefix-ABC-DEF"), KVP("random")],
                    defaultWildcardTagHelpers,
                    [inputWildcardPrefixDescriptor]
                },
                {
                    "input",
                    [KVP("prefixABCnodashprefix")],
                    defaultWildcardTagHelpers,
                    null
                },
                {
                    "input",
                    [KVP("prefix-")],
                    defaultWildcardTagHelpers,
                    default
                },
                {
                    "input",
                    [KVP("nodashprefix")],
                    defaultWildcardTagHelpers,
                    null
                },
                {
                    "input",
                    [KVP("prefix-A")],
                    defaultWildcardTagHelpers,
                    [catchAllWildcardPrefixDescriptor]
                },
                {
                    "input",
                    [KVP("prefix-ABC-DEF"), KVP("random")],
                    defaultWildcardTagHelpers,
                    [catchAllWildcardPrefixDescriptor]
                },
                {
                    "input",
                    [KVP("prefix-abc"), KVP("nodashprefix-def")],
                    defaultWildcardTagHelpers,
                    [inputWildcardPrefixDescriptor, catchAllWildcardPrefixDescriptor]
                },
                {
                    "input",
                    [KVP("class"), KVP("prefix-abc"), KVP("onclick"), KVP("nodashprefix-def"), KVP("style")],
                    defaultWildcardTagHelpers,
                    [inputWildcardPrefixDescriptor, catchAllWildcardPrefixDescriptor]
                }
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
        var binder = new TagHelperBinder(null, availableTagHelpers);

        // Act
        var binding = binder.GetBinding(
            tagName,
            providedAttributes,
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        if (expectedTagHelpers is null)
        {
            Assert.Null(binding);
        }
        else
        {
            Assert.NotNull(binding);
            Assert.Equal(expectedTagHelpers, binding.Descriptors);
        }
    }

    [Fact]
    public void GetBinding_ReturnsNullBindingResultPrefixAsTagName()
    {
        // Arrange
        var catchAllTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName(TagHelperMatchingConventions.ElementCatchAllName))
            .Build();

        var binder = new TagHelperBinder("th", [catchAllTagHelper]);

        // Act
        var binding = binder.GetBinding(
            tagName: "th",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.Null(binding);
    }

    [Fact]
    public void GetBinding_ReturnsBindingResultCatchAllDescriptorsForPrefixedTags()
    {
        // Arrange
        var catchAllTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName(TagHelperMatchingConventions.ElementCatchAllName))
            .Build();

        var binder = new TagHelperBinder("th:", [catchAllTagHelper]);

        // Act
        var bindingDiv = binder.GetBinding(
            tagName: "th:div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);
        var bindingSpan = binder.GetBinding(
            tagName: "th:span",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(bindingDiv);
        var tagHelper = Assert.Single(bindingDiv.Descriptors);
        Assert.Same(catchAllTagHelper, tagHelper);

        Assert.NotNull(bindingSpan);
        tagHelper = Assert.Single(bindingSpan.Descriptors);
        Assert.Same(catchAllTagHelper, tagHelper);
    }

    [Fact]
    public void GetBinding_ReturnsBindingResultDescriptorsForPrefixedTags()
    {
        // Arrange
        var divTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();

        var binder = new TagHelperBinder("th:", [divTagHelper]);

        // Act
        var binding = binder.GetBinding(
            tagName: "th:div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(binding);
        var tagHelper = Assert.Single(binding.Descriptors);
        Assert.Same(divTagHelper, tagHelper);
    }

    [Theory]
    [InlineData("*")]
    [InlineData("div")]
    public void GetBinding_ReturnsNullForUnprefixedTags(string tagName)
    {
        // Arrange
        var divTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName(tagName))
            .Build();

        var binder = new TagHelperBinder("th:", [divTagHelper]);

        // Act
        var binding = binder.GetBinding(
            tagName: "div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.Null(binding);
    }

    [Fact]
    public void GetBinding_ReturnsNothingForUnregisteredTags()
    {
        // Arrange
        var divTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();
        var spanTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo2", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("span"))
            .Build();

        var binder = new TagHelperBinder(null, [divTagHelper, spanTagHelper]);

        // Act
        var binding = binder.GetBinding(
            tagName: "foo",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.Null(binding);
    }

    [Fact]
    public void GetBinding_ReturnsCatchAllsWithEveryTagName()
    {
        // Arrange
        var divTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();
        var spanTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo2", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("span"))
            .Build();
        var catchAllTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo3", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName(TagHelperMatchingConventions.ElementCatchAllName))
            .Build();

        var binder = new TagHelperBinder(null, [divTagHelper, spanTagHelper, catchAllTagHelper]);

        // Act
        var divBinding = binder.GetBinding(
            tagName: "div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);
        var spanBinding = binder.GetBinding(
            tagName: "span",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        // For divs
        Assert.NotNull(divBinding);
        Assert.Equal(2, divBinding.Descriptors.Length);
        Assert.Contains(divTagHelper, divBinding.Descriptors);
        Assert.Contains(catchAllTagHelper, divBinding.Descriptors);

        // For spans
        Assert.NotNull(spanBinding);
        Assert.Equal(2, spanBinding.Descriptors.Length);
        Assert.Contains(spanTagHelper, spanBinding.Descriptors);
        Assert.Contains(catchAllTagHelper, spanBinding.Descriptors);
    }

    [Fact]
    public void GetBinding_DuplicateDescriptorsAreNotPartOfTagHelperDescriptorPool()
    {
        // Arrange
        var divTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();

        var binder = new TagHelperBinder(null, [divTagHelper, divTagHelper]);

        // Act
        var binding = binder.GetBinding(
            tagName: "div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(binding);
        var tagHelpers = Assert.Single(binding.Descriptors);
        Assert.Same(divTagHelper, tagHelpers);
    }

    [Fact]
    public void GetBinding_DescriptorWithMultipleRules_CorrectlySelectsMatchingRules()
    {
        // Arrange
        var multiRuleTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule
                .RequireTagName(TagHelperMatchingConventions.ElementCatchAllName)
                .RequireParentTag("body"))
            .TagMatchingRuleDescriptor(rule => rule
                .RequireTagName("div"))
            .TagMatchingRuleDescriptor(rule => rule
                .RequireTagName("span"))
            .Build();

        var binder = new TagHelperBinder(null, [multiRuleTagHelper]);

        // Act
        var binding = binder.GetBinding(
            tagName: "div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(binding);
        var boundTagHelper = Assert.Single(binding.Descriptors);
        Assert.Same(multiRuleTagHelper, boundTagHelper);
        var boundRules = binding.GetBoundRules(boundTagHelper);
        var boundRule = Assert.Single(boundRules);
        Assert.Equal("div", boundRule.TagName);
    }

    [Fact]
    public void GetBinding_PrefixedParent_ReturnsBinding()
    {
        // Arrange
        var divTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div").RequireParentTag("p"))
            .Build();
        var pTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo2", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
            .Build();

        var binder = new TagHelperBinder("th:", [divTagHelper, pTagHelper]);

        // Act
        var binding = binder.GetBinding(
            tagName: "th:div",
            attributes: [],
            parentTagName: "th:p",
            parentIsTagHelper: true);

        // Assert
        Assert.NotNull(binding);
        var boundTagHelper = Assert.Single(binding.Descriptors);
        Assert.Same(divTagHelper, boundTagHelper);
        var boundRules = binding.GetBoundRules(boundTagHelper);
        var boundRule = Assert.Single(boundRules);
        Assert.Equal("div", boundRule.TagName);
        Assert.Equal("p", boundRule.ParentTag);
    }

    [Fact]
    public void GetBinding_IsAttributeMatch_SingleAttributeMatch()
    {
        // Arrange
        var divTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .ClassifyAttributesOnly(true)
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();

        var binder = new TagHelperBinder("", [divTagHelper]);

        // Act
        var binding = binder.GetBinding(
            tagName: "div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(binding);
        Assert.True(binding.IsAttributeMatch);
    }

    [Fact]
    public void GetBinding_IsAttributeMatch_MultipleAttributeMatches()
    {
        // Arrange
        var divTagHelper1 = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .ClassifyAttributesOnly(true)
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();

        var divTagHelper2 = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .ClassifyAttributesOnly(true)
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();

        var binder = new TagHelperBinder("", [divTagHelper1, divTagHelper2]);

        // Act
        var binding = binder.GetBinding(
            tagName: "div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(binding);
        Assert.True(binding.IsAttributeMatch);
    }

    [Fact]
    public void GetBinding_IsAttributeMatch_MixedAttributeMatches()
    {
        // Arrange
        var divTagHelper1 = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .ClassifyAttributesOnly(true)
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();

        var divTagHelper2 = TagHelperDescriptorBuilder.CreateTagHelper("foo1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .Build();

        var binder = new TagHelperBinder("", [divTagHelper1, divTagHelper2]);

        // Act
        var binding = binder.GetBinding(
            tagName: "div",
            attributes: [],
            parentTagName: "p",
            parentIsTagHelper: false);

        // Assert
        Assert.NotNull(binding);
        Assert.False(binding.IsAttributeMatch);
    }

    [Fact]
    public void GetBinding_CaseSensitiveRule_CaseMismatch_ReturnsNull()
    {
        // Arrange
        var divTagHelper = TagHelperDescriptorBuilder.CreateTagHelper("DivTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
            .SetCaseSensitive()
            .Build();

        var binder = new TagHelperBinder("th:", [divTagHelper]);

        // Act
        var binding = binder.GetBinding(
            tagName: "th:Div",
            attributes: [KVP("class", "something")],
            parentTagName: "body",
            parentIsTagHelper: false);

        // Assert
        Assert.Null(binding);
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

        var binder = new TagHelperBinder(null, [divTagHelper]);

        // Act
        var binding = binder.GetBinding(
            tagName: "div",
            attributes: [KVP("CLASS", "something")],
            parentTagName: "body",
            parentIsTagHelper: false);

        // Assert
        Assert.Null(binding);
    }
}
