// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class TagHelperMatchingConventionsTest
{
    // requiredAttributeDescriptor, attributeName, attributeValue, expectedResult
    public static TheoryData<Action<TagMatchingRuleDescriptorBuilder>, string, string, bool> RequiredAttributeDescriptorData
    {
        get
        {
            return new()
            {
                {
                    builder => builder.AddAttribute("key"),
                    "KeY",
                    "value",
                    true
                },
                {
                    builder => builder.AddAttribute("key"),
                    "keys",
                    "value",
                    false
                },
                {
                    builder => builder.AddAttribute("route-", RequiredAttributeNameComparison.PrefixMatch),
                    "ROUTE-area",
                    "manage",
                    true
                },
                {
                    builder => builder.AddAttribute("route-", RequiredAttributeNameComparison.PrefixMatch),
                    "routearea",
                    "manage",
                    false
                },
                {
                    builder => builder.AddAttribute("route-", RequiredAttributeNameComparison.PrefixMatch),
                    "route-",
                    "manage",
                    false
                },
                {
                    builder => builder.AddAttribute("key", RequiredAttributeNameComparison.FullMatch),
                    "KeY",
                    "value",
                    true
                },
                {
                    builder => builder.AddAttribute("key", RequiredAttributeNameComparison.FullMatch),
                    "keys",
                    "value",
                    false
                },
                {
                    builder => builder.AddAttribute(
                        name: "key", RequiredAttributeNameComparison.FullMatch,
                        value: "value", RequiredAttributeValueComparison.FullMatch),
                    "key",
                    "value",
                    true
                },
                {
                    builder => builder.AddAttribute(
                        name: "key", RequiredAttributeNameComparison.FullMatch,
                        value: "value", RequiredAttributeValueComparison.FullMatch),
                    "key",
                    "Value",
                    false
                },
                {
                    builder => builder.AddAttribute(
                        name: "class", RequiredAttributeNameComparison.FullMatch,
                        value: "btn", RequiredAttributeValueComparison.PrefixMatch),
                    "class",
                    "btn btn-success",
                    true
                },
                {
                    builder => builder.AddAttribute(
                        name: "class", RequiredAttributeNameComparison.FullMatch,
                        value: "btn", RequiredAttributeValueComparison.PrefixMatch),
                    "class",
                    "BTN btn-success",
                    false
                },
                {
                    builder => builder.AddAttribute(
                        name: "href", RequiredAttributeNameComparison.FullMatch,
                        value: "#navigate", RequiredAttributeValueComparison.SuffixMatch),
                    "href",
                    "/home/index#navigate",
                    true
                },
                {
                    builder => builder.AddAttribute(
                        name: "href", RequiredAttributeNameComparison.FullMatch,
                        value: "#navigate", RequiredAttributeValueComparison.SuffixMatch),
                    "href",
                    "/home/index#NAVigate",
                    false
                },
            };
        }
    }

    [Theory]
    [MemberData(nameof(RequiredAttributeDescriptorData))]
    public void Matches_ReturnsExpectedResult(
        Action<TagMatchingRuleDescriptorBuilder> configure,
        string attributeName,
        string attributeValue,
        bool expectedResult)
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "Test")
            .TagMatchingRuleDescriptor(builder => configure(builder))
            .Build();

        var requiredAttribute = tagHelper.TagMatchingRules[0].Attributes[0];

        // Act
        var result = TagHelperMatchingConventions.SatisfiesRequiredAttribute(requiredAttribute, attributeName, attributeValue);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void CanSatisfyBoundAttribute_IndexerAttribute_ReturnsFalseIsNotMatching()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "Test")
            .BoundAttributeDescriptor(builder => builder.AsDictionary("asp-", typeof(Dictionary<string, string>).FullName))
            .Build();

        var boundAttribute = tagHelper.BoundAttributes[0];

        // Act
        var result = TagHelperMatchingConventions.CanSatisfyBoundAttribute("style", boundAttribute);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanSatisfyBoundAttribute_IndexerAttribute_ReturnsTrueIfMatching()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper("TestTagHelper", "Test")
            .BoundAttributeDescriptor(builder => builder.AsDictionary("asp-", typeof(Dictionary<string, string>).FullName))
            .Build();

        var boundAttribute = tagHelper.BoundAttributes[0];

        // Act
        var result = TagHelperMatchingConventions.CanSatisfyBoundAttribute("asp-route-controller", boundAttribute);

        // Assert
        Assert.True(result);
    }
}
