// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.Serialization.Json;

public class TagHelperDescriptorSerializationTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    public void TagHelperDescriptor_DefaultBlazorServerProject_RoundTrips()
    {
        // Arrange
        var expectedTagHelpers = RazorTestResources.BlazorServerAppTagHelpers;

        // Act

        using var writeStream = new MemoryStream();

        // Serialize the tag helpers to a stream
        using (var writer = new StreamWriter(writeStream, Encoding.UTF8, bufferSize: 4096, leaveOpen: true))
        {
            JsonDataConvert.Serialize(expectedTagHelpers, writer);
        }

        // Deserialize the tag helpers from the stream we just serialized to.
        writeStream.Seek(0, SeekOrigin.Begin);

        ImmutableArray<TagHelperDescriptor> actualTagHelpers;

        using (var reader = new StreamReader(writeStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true))
        {
            actualTagHelpers = JsonDataConvert.DeserializeTagHelperArray(reader);
        }

        // Assert
        Assert.Equal<TagHelperDescriptor>(expectedTagHelpers, actualTagHelpers);
    }

    [Fact]
    public void TagHelperDescriptor_RoundTripsProperly()
    {
        // Arrange
        var expectedTagHelper = TagHelperDescriptorBuilder.CreateTagHelper(TagHelperKind.ITagHelper, "type name", "assembly name")
            .AllowChildTag("allowed-child-one")
            .BoundAttribute(name: "test-attribute", propertyName: "TestAttribute", typeName: "string")
            .AddTagMatchingRule(tagName: "tag-name", parentTagName: "parent-name", tagStructure: TagStructure.WithoutEndTag, builder => builder
                .AddAttribute("required-attribute-one", RequiredAttributeNameComparison.PrefixMatch)
                .AddAttribute(
                    name: "required-attribute-two", RequiredAttributeNameComparison.FullMatch,
                    value: "something", RequiredAttributeValueComparison.PrefixMatch))
            .Build();

        // Act
        var json = JsonDataConvert.Serialize(expectedTagHelper);
        var tagHelper = JsonDataConvert.DeserializeTagHelper(json);

        // Assert
        Assert.Equal(expectedTagHelper, tagHelper);
    }

    [Fact]
    public void ViewComponentTagHelperDescriptor_RoundTripsProperly()
    {
        // Arrange
        var expectedTagHelper = TagHelperDescriptorBuilder.CreateTagHelper(TagHelperKind.ViewComponent, "type name", "assembly name")
            .AllowChildTag("allowed-child-one")
            .BoundAttribute(name: "test-attribute", propertyName: "TestAttribute", typeName: "string")
            .AddTagMatchingRule(tagName: "tag-name", parentTagName: "parent-name", tagStructure: TagStructure.WithoutEndTag, builder => builder
                .AddAttribute("required-attribute-one", RequiredAttributeNameComparison.PrefixMatch)
                .AddAttribute(
                    name: "required-attribute-two", RequiredAttributeNameComparison.FullMatch,
                    value: "something", RequiredAttributeValueComparison.PrefixMatch))
            .Build();

        // Act
        var json = JsonDataConvert.Serialize(expectedTagHelper);
        var tagHelper = JsonDataConvert.DeserializeTagHelper(json);

        // Assert
        Assert.Equal(expectedTagHelper, tagHelper);
    }

    [Fact]
    public void TagHelperDescriptor_WithDiagnostic_RoundTripsProperly()
    {
        // Arrange
        var expectedTagHelper = TagHelperDescriptorBuilder.CreateTagHelper(TagHelperKind.ITagHelper, "type name", "assembly name")
            .AllowChildTag("allowed-child-one")
            .BoundAttribute(name: "test-attribute", propertyName: "TestAttribute", typeName: "string")
            .TagMatchingRule(tagName: "tag-name", parentTagName: "parent-name", builder => builder
                .AddAttribute("required-attribute-one", RequiredAttributeNameComparison.PrefixMatch)
                .AddAttribute(
                    name: "required-attribute-two", RequiredAttributeNameComparison.FullMatch,
                    value: "something", RequiredAttributeValueComparison.PrefixMatch))
            .AddDiagnostic(RazorDiagnostic.Create(
                new RazorDiagnosticDescriptor("id", "Test Message", RazorDiagnosticSeverity.Error), new SourceSpan(null, 10, 20, 30, 40)))
            .Build();

        // Act
        var json = JsonDataConvert.Serialize(expectedTagHelper);
        var tagHelper = JsonDataConvert.DeserializeTagHelper(json);

        // Assert
        Assert.Equal(expectedTagHelper, tagHelper);
    }

    [Fact]
    public void TagHelperDescriptor_WithIndexerAttributes_RoundTripsProperly()
    {
        // Arrange
        var expectedTagHelper = TagHelperDescriptorBuilder.CreateTagHelper(TagHelperKind.ITagHelper, "type name", "assembly name")
            .AllowChildTag("allowed-child-one")
            .TagOutputHint("Hint")
            .BoundAttribute(name: "test-attribute", propertyName: "TestAttribute", typeName: "SomeEnum", builder => builder
                .AsEnum()
                .Documentation("Summary"))
            .BoundAttribute(name: "test-attribute2", propertyName: "TestAttribute2", typeName: "SomeDictionary", builder => builder
                .AsDictionaryAttribute("dict-prefix-", "string"))
            .TagMatchingRule(tagName: "tag-name", parentTagName: "parent-name", builder => builder
                .AddAttribute("required-attribute-one", RequiredAttributeNameComparison.PrefixMatch))
            .AddDiagnostic(RazorDiagnostic.Create(
                new RazorDiagnosticDescriptor("id", "Test Message", RazorDiagnosticSeverity.Error), new SourceSpan(null, 10, 20, 30, 40)))
            .Build();

        // Act
        var json = JsonDataConvert.Serialize(expectedTagHelper);
        var tagHelper = JsonDataConvert.DeserializeTagHelper(json);

        // Assert
        Assert.Equal(expectedTagHelper, tagHelper);
    }

    [Fact]
    public void TagHelperDescriptor_WithoutEditorRequired_RoundTripsProperly()
    {
        // Arrange
        var expectedTagHelper = TagHelperDescriptorBuilder.CreateTagHelper(TagHelperKind.ITagHelper, "type name", "assembly name")
            .BoundAttribute(name: "test-attribute", propertyName: "TestAttribute", typeName: "string")
            .AddTagMatchingRule(tagName: "tag-name2")
            .Build();

        // Act
        var json = JsonDataConvert.Serialize(expectedTagHelper);
        var tagHelper = JsonDataConvert.DeserializeTagHelper(json);

        // Assert
        Assert.NotNull(tagHelper);
        Assert.Equal(expectedTagHelper, tagHelper);

        var boundAttribute = Assert.Single(tagHelper.BoundAttributes);
        Assert.False(boundAttribute.IsEditorRequired);
    }

    [Fact]
    public void TagHelperDescriptor_WithEditorRequired_RoundTripsProperly()
    {
        // Arrange
        var expectedTagHelper = TagHelperDescriptorBuilder.CreateTagHelper(TagHelperKind.ITagHelper, "type name", "assembly name")
            .BoundAttribute(name: "test-attribute", propertyName: "TestAttribute", typeName: "string", builder =>
            {
                builder.IsEditorRequired = true;
            })
            .AddTagMatchingRule(tagName: "tag-name3")
            .Build();

        // Act
        var json = JsonDataConvert.Serialize(expectedTagHelper);
        var tagHelper = JsonDataConvert.DeserializeTagHelper(json);

        // Assert
        Assert.NotNull(tagHelper);
        Assert.Equal(expectedTagHelper, tagHelper);

        var boundAttribute = Assert.Single(tagHelper.BoundAttributes);
        Assert.True(boundAttribute.IsEditorRequired);
    }
}
