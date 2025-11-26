// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.Serialization.MessagePack.Resolvers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Serialization;

public class TagHelperDeltaResultSerializationTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private static readonly MessagePackSerializerOptions s_options = MessagePackSerializerOptions.Standard
        .WithResolver(CompositeResolver.Create(
            TagHelperDeltaResultResolver.Instance,
            StandardResolver.Instance));

    [Fact]
    public void TagHelperResolutionResult_DefaultBlazorServerProject_RoundTrips()
    {
        // Arrange
        var tagHelpers = RazorTestResources.BlazorServerAppTagHelpers;
        var checksums = tagHelpers.SelectAsArray(t => t.Checksum);

        var expectedResult = new TagHelperDeltaResult(
            IsDelta: true,
            ResultId: 1,
            Added: checksums,
            Removed: checksums);

        // Act

        // Serialize the result to bytes
        var bytes = MessagePackConvert.Serialize(expectedResult, s_options);

        // Deserialize the bytes we just serialized.
        var actualResult = MessagePackConvert.Deserialize<TagHelperDeltaResult?>(bytes, s_options);

        // Assert
        Assert.NotNull(actualResult);
        Assert.Equal(expectedResult, actualResult);
    }

    [Fact]
    public void TagHelperDescriptor_RoundTripsProperly()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper(TagHelperKind.ITagHelper, "type name", "assembly name")
            .AllowChildTag("allowed-child-one")
            .BoundAttribute(name: "test-attribute", propertyName: "TestAttribute", typeName: "string")
            .AddTagMatchingRule(tagName: "tag-name", parentTagName: "parent-name", tagStructure: TagStructure.WithoutEndTag, builder => builder
                .AddAttribute("required-attribute-one", RequiredAttributeNameComparison.PrefixMatch)
                .AddAttribute(
                    name: "required-attribute-two", RequiredAttributeNameComparison.FullMatch,
                    value: "something", RequiredAttributeValueComparison.PrefixMatch))
            .Build();

        var expectedResult = new TagHelperDeltaResult(
            IsDelta: true,
            ResultId: 1,
            Added: [tagHelper.Checksum],
            Removed: [tagHelper.Checksum]);

        // Act
        var bytes = MessagePackConvert.Serialize(expectedResult, s_options);
        var actualResult = MessagePackConvert.Deserialize<TagHelperDeltaResult>(bytes, s_options);

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }

    [Fact]
    public void ViewComponentTagHelperDescriptor_RoundTripsProperly()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper(TagHelperKind.ViewComponent, "type name", "assembly name")
            .AllowChildTag("allowed-child-one")
            .BoundAttribute(name: "test-attribute", propertyName: "TestAttribute", typeName: "string")
            .AddTagMatchingRule("tag-name", parentTagName: "parent-name", tagStructure: TagStructure.WithoutEndTag, builder => builder
                .AddAttribute("required-attribute-one", RequiredAttributeNameComparison.PrefixMatch)
                .AddAttribute(
                    name: "required-attribute-two", RequiredAttributeNameComparison.FullMatch,
                    value: "something", RequiredAttributeValueComparison.PrefixMatch))
            .Build();

        var expectedResult = new TagHelperDeltaResult(
            IsDelta: true,
            ResultId: 1,
            Added: [tagHelper.Checksum],
            Removed: [tagHelper.Checksum]);

        // Act
        var bytes = MessagePackConvert.Serialize(expectedResult, s_options);
        var actualResult = MessagePackConvert.Deserialize<TagHelperDeltaResult>(bytes, s_options);

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }

    [Fact]
    public void TagHelperDescriptor_WithDiagnostic_RoundTripsProperly()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper(TagHelperKind.ITagHelper, "type name", "assembly name")
            .AllowChildTag("allowed-child-one")
            .BoundAttribute(name: "test-attribute", propertyName: "TestAttribute", typeName: "string")
            .TagMatchingRule("tag-name", parentTagName: "parent-name", builder => builder
                .AddAttribute("required-attribute-one", RequiredAttributeNameComparison.PrefixMatch)
                .AddAttribute(name:
                    "required-attribute-two", RequiredAttributeNameComparison.FullMatch,
                    value: "something", RequiredAttributeValueComparison.PrefixMatch))
            .AddDiagnostic(RazorDiagnostic.Create(
                new RazorDiagnosticDescriptor("id", "Test Message", RazorDiagnosticSeverity.Error), new SourceSpan(null, 10, 20, 30, 40)))
            .Build();

        var expectedResult = new TagHelperDeltaResult(
            IsDelta: true,
            ResultId: 1,
            Added: [tagHelper.Checksum],
            Removed: [tagHelper.Checksum]);

        // Act
        var bytes = MessagePackConvert.Serialize(expectedResult, s_options);
        var actualResult = MessagePackConvert.Deserialize<TagHelperDeltaResult>(bytes, s_options);

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }

    [Fact]
    public void TagHelperDescriptor_WithIndexerAttributes_RoundTripsProperly()
    {
        // Arrange
        var tagHelper = TagHelperDescriptorBuilder.CreateTagHelper(TagHelperKind.ITagHelper, "type name", "assembly name")
            .AllowChildTag("allowed-child-one")
            .TagOutputHint("Hint")
            .BoundAttributeDescriptor(builder => builder
                .Name("test-attribute")
                .PropertyName("TestAttribute")
                .TypeName("SomeEnum")
                .AsEnum()
                .Documentation("Summary"))
            .BoundAttributeDescriptor(builder => builder
                .Name("test-attribute2")
                .PropertyName("TestAttribute2")
                .TypeName("SomeDictionary")
                .AsDictionaryAttribute("dict-prefix-", "string"))
            .TagMatchingRule("tag-name", builder => builder
                .AddAttribute("required-attribute-one", RequiredAttributeNameComparison.PrefixMatch))
            .Build();

        var expectedResult = new TagHelperDeltaResult(
            IsDelta: true,
            ResultId: 1,
            Added: [tagHelper.Checksum],
            Removed: [tagHelper.Checksum]);

        // Act
        var bytes = MessagePackConvert.Serialize(expectedResult, s_options);
        var actualResult = MessagePackConvert.Deserialize<TagHelperDeltaResult>(bytes, s_options);

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }
}
