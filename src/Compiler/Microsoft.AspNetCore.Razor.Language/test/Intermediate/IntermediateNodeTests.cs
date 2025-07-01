// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.CommonMetadata;
using static Microsoft.AspNetCore.Razor.Language.Extensions.Constants;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public class IntermediateNodeTests
{
    [Fact]
    public void WriteRazorCompiledItemAttribute_RendersCorrectly()
    {
        // Arrange
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new RazorCompiledItemAttributeIntermediateNode(
            typeName: "Foo.Bar",
            kind: "test",
            identifier: "Foo/Bar");

        // Act
        node.WriteNode(target: null!, context);

        // Assert
        var csharpText = context.CodeWriter.GetText().ToString().TrimEnd();
        Assert.Equal($"""
            [assembly: {RazorCompiledItemAttributeTypeName}(typeof(Foo.Bar), @"test", @"Foo/Bar")]
            """,
            csharpText);
    }

    [Fact]
    public void WriteRazorSourceChecksumAttribute_RendersCorrectly()
    {
        // Arrange
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new RazorSourceChecksumAttributeIntermediateNode(
            checksum: [(byte)'t', (byte)'e', (byte)'s', (byte)'t'],
            checksumAlgorithm: SourceHashAlgorithm.Sha256,
            identifier: "Foo/Bar");

        // Act
        node.WriteNode(target: null!, context);

        // Assert
        var csharpText = context.CodeWriter.GetText().ToString().TrimEnd();
        Assert.Equal($"""
            [{RazorSourceChecksumAttributeTypeName}(@"Sha256", @"74657374", @"Foo/Bar")]
            """,
            csharpText);
    }

    [Fact]
    public void WriteRazorCompiledItemAttributeMetadata_RendersCorrectly()
    {
        // Arrange
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new RazorCompiledItemMetadataAttributeIntermediateNode(key: "key", value: "value");

        // Act
        node.WriteNode(target: null!, context);

        // Assert
        var csharpText = context.CodeWriter.GetText().ToString().TrimEnd();
        Assert.Equal($"""
            [{RazorCompiledItemMetadataAttributeTypeName}("key", "value")]
            """,
            csharpText);
    }

    [Fact]
    public void WriteRazorCompiledItemAttributeMetadata_EscapesKeysAndValuesCorrectly()
    {
        // Arrange
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new RazorCompiledItemMetadataAttributeIntermediateNode(key: @"""test"" key", value: @"""test"" value");

        // Act
        node.WriteNode(target: null!, context);

        // Assert
        var csharpText = context.CodeWriter.GetText().ToString().TrimEnd();
        Assert.Equal($"""
            [{RazorCompiledItemMetadataAttributeTypeName}("\"test\" key", "\"test\" value")]
            """,
            csharpText);
    }

    [Fact]
    public void WriteTagHelperHtmlAttributeValue_RendersCorrectly()
    {
        // Arrange
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new PreallocatedTagHelperHtmlAttributeValueIntermediateNode(
            variableName: "MyProp",
            attributeName: "Foo",
            value: "Bar",
            attributeStructure: AttributeStructure.DoubleQuotes);

        // Act
        node.WriteNode(target: null!, context);

        // Assert
        var csharpText = context.CodeWriter.GetText().ToString().TrimEnd();
        Assert.Equal($"""
            private static readonly {TagHelperAttributeTypeName} MyProp = new {TagHelperAttributeTypeName}("Foo", new {EncodedHtmlStringTypeName}("Bar"), {HtmlAttributeValueStyleTypeName}.DoubleQuotes);
            """,
            csharpText);
    }

    [Fact]
    public void WriteTagHelperHtmlAttributeValue_Minimized_RendersCorrectly()
    {
        // Arrange
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new PreallocatedTagHelperHtmlAttributeValueIntermediateNode(
            variableName: "_tagHelper1",
            attributeName: "Foo",
            value: "Bar",
            attributeStructure: AttributeStructure.Minimized);

        // Act
        node.WriteNode(target: null!, context);

        // Assert
        var csharpText = context.CodeWriter.GetText().ToString().TrimEnd();
        Assert.Equal($"""
            private static readonly {TagHelperAttributeTypeName} _tagHelper1 = new {TagHelperAttributeTypeName}("Foo");
            """,
            csharpText);
    }

    [Fact]
    public void WriteTagHelperHtmlAttribute_RendersCorrectly()
    {
        // Arrange
        using var context = TestCodeRenderingContext.CreateRuntime();

        var tagHelperNode = new TagHelperIntermediateNode();
        var node = new PreallocatedTagHelperHtmlAttributeIntermediateNode(variableName: "_tagHelper1");

        tagHelperNode.Children.Add(node);
        Push(context, tagHelperNode);

        // Act
        node.WriteNode(target: null!, context);

        // Assert
        var csharpText = context.CodeWriter.GetText().ToString().TrimEnd();
        Assert.Equal($"""
            __tagHelperExecutionContext.AddHtmlAttribute(_tagHelper1);
            """,
            csharpText);
    }

    [Fact]
    public void WriteTagHelperPropertyValue_RendersCorrectly()
    {
        // Arrange
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new PreallocatedTagHelperPropertyValueIntermediateNode(
            variableName: "_tagHelper1",
            attributeName: "Foo",
            value: "Bar",
            attributeStructure: AttributeStructure.DoubleQuotes);

        // Act
        node.WriteNode(target: null!, context);

        // Assert
        var csharpText = context.CodeWriter.GetText().ToString().TrimEnd();
        Assert.Equal($"""
            private static readonly {TagHelperAttributeTypeName} _tagHelper1 = new {TagHelperAttributeTypeName}("Foo", "Bar", {HtmlAttributeValueStyleTypeName}.DoubleQuotes);
            """,
            csharpText);
    }

    [Fact]
    public void WriteTagHelperProperty_RendersCorrectly()
    {
        // Arrange
        using var context = TestCodeRenderingContext.CreateRuntime();

        var tagHelper = TagHelperDescriptorBuilder.Create(TagHelperConventions.DefaultKind, "FooTagHelper", "Test")
            .Metadata(TypeName("FooTagHelper"))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("Foo")
                .TypeName("System.String")
                .Metadata(PropertyName("FooProp")))
            .Build();

        var boundAttribute = tagHelper.BoundAttributes[0];

        var tagHelperNode = new TagHelperIntermediateNode();

        var node = new PreallocatedTagHelperPropertyIntermediateNode(
            attributeName: boundAttribute.Name,
            attributeStructure: 0,
            boundAttribute: boundAttribute,
            fieldName: "__FooTagHelper",
            isIndexerNameMatch: false,
            propertyName: "FooProp",
            tagHelper,
            variableName: "_tagHelper1");

        tagHelperNode.Children.Add(node);
        Push(context, tagHelperNode);

        // Act
        node.WriteNode(target: null!, context);

        // Assert
        var csharpText = context.CodeWriter.GetText().ToString().TrimEnd();
        Assert.Equal($"""
            __FooTagHelper.FooProp = (string)_tagHelper1.Value;
            __tagHelperExecutionContext.AddTagHelperAttribute(_tagHelper1);
            """,
            csharpText);
    }

    [Fact]
    public void WriteSetPreallocatedTagHelperProperty_IndexerAttribute_RendersCorrectly()
    {
        // Arrange
        using var context = TestCodeRenderingContext.CreateRuntime();

        var tagHelper = TagHelperDescriptorBuilder.Create(TagHelperConventions.DefaultKind, "FooTagHelper", "Test")
            .Metadata(TypeName("FooTagHelper"))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("Foo")
                .TypeName("System.Collections.Generic.Dictionary<System.String, System.String>")
                .AsDictionaryAttribute("pre-", "System.String")
                .Metadata(PropertyName("FooProp")))
            .Build();

        var boundAttribute = tagHelper.BoundAttributes[0];

        var tagHelperNode = new TagHelperIntermediateNode();

        var node = new PreallocatedTagHelperPropertyIntermediateNode(
            attributeName: "pre-Foo",
            attributeStructure: 0,
            boundAttribute,
            fieldName: "__FooTagHelper",
            isIndexerNameMatch: true,
            propertyName: "FooProp",
            tagHelper,
            variableName: "_tagHelper1");

        tagHelperNode.Children.Add(node);
        Push(context, tagHelperNode);

        // Act
        node.WriteNode(target: null!, context);

        // Assert
        var csharpText = context.CodeWriter.GetText().ToString().TrimEnd();
        Assert.Equal($$"""
            if (__FooTagHelper.FooProp == null)
            {
                throw new InvalidOperationException(InvalidTagHelperIndexerAssignment("pre-Foo", "FooTagHelper", "FooProp"));
            }
            __FooTagHelper.FooProp["Foo"] = (string)_tagHelper1.Value;
            __tagHelperExecutionContext.AddTagHelperAttribute(_tagHelper1);
            """,
            csharpText);
    }

    [Fact]
    public void WriteSetPreallocatedTagHelperProperty_IndexerAttribute_MultipleValues()
    {
        // Arrange
        using var context = TestCodeRenderingContext.CreateRuntime();

        var tagHelper = TagHelperDescriptorBuilder.Create(TagHelperConventions.DefaultKind, "FooTagHelper", "Test")
            .Metadata(TypeName("FooTagHelper"))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("Foo")
                .TypeName("System.Collections.Generic.Dictionary<System.String, System.String>")
                .AsDictionaryAttribute("pre-", "System.String")
                .Metadata(PropertyName("FooProp")))
            .Build();

        var boundAttribute = tagHelper.BoundAttributes[0];

        var tagHelperNode = new TagHelperIntermediateNode();

        var node1 = new PreallocatedTagHelperPropertyIntermediateNode(
            attributeName: "pre-Bar",
            attributeStructure: 0,
            boundAttribute,
            fieldName: "__FooTagHelper",
            isIndexerNameMatch: true,
            propertyName: "FooProp",
            tagHelper,
            variableName: "_tagHelper0s");

        var node2 = new PreallocatedTagHelperPropertyIntermediateNode(
            attributeName: "pre-Foo",
            attributeStructure: 0,
            boundAttribute,
            fieldName: "__FooTagHelper",
            isIndexerNameMatch: true,
            propertyName: "FooProp",
            tagHelper,
            variableName: "_tagHelper1");

        tagHelperNode.Children.Add(node1);
        tagHelperNode.Children.Add(node2);
        Push(context, tagHelperNode);

        // Act
        node2.WriteNode(target: null!, context);

        // Assert
        var csharpText = context.CodeWriter.GetText().ToString().TrimEnd();
        Assert.Equal($"""
            __FooTagHelper.FooProp["Foo"] = (string)_tagHelper1.Value;
            __tagHelperExecutionContext.AddTagHelperAttribute(_tagHelper1);
            """,
            csharpText);
    }

    private static void Push(CodeRenderingContext context, TagHelperIntermediateNode node)
    {
        context.PushAncestor(node);
    }
}
