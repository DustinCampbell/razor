// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.CommonMetadata;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public class PreallocatedAttributeTargetExtensionTest
{
    [Fact]
    public void WriteTagHelperHtmlAttributeValue_RendersCorrectly()
    {
        // Arrange
        var extension = new PreallocatedAttributeTargetExtension();
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new PreallocatedTagHelperHtmlAttributeValueIntermediateNode(
            variableName: "MyProp",
            attributeName: "Foo",
            value: "Bar",
            AttributeStructure.DoubleQuotes);

        // Act
        extension.WriteTagHelperHtmlAttributeValue(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute MyProp = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute(""Foo"", new global::Microsoft.AspNetCore.Html.HtmlString(""Bar""), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteTagHelperHtmlAttributeValue_Minimized_RendersCorrectly()
    {
        // Arrange
        var extension = new PreallocatedAttributeTargetExtension();
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new PreallocatedTagHelperHtmlAttributeValueIntermediateNode(
            variableName: "_tagHelper1",
            attributeName: "Foo",
            value: "Bar",
            AttributeStructure.Minimized);

        // Act
        extension.WriteTagHelperHtmlAttributeValue(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute _tagHelper1 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute(""Foo"");
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteTagHelperHtmlAttribute_RendersCorrectly()
    {
        // Arrange
        var extension = new PreallocatedAttributeTargetExtension();
        using var context = TestCodeRenderingContext.CreateRuntime();

        var tagHelperNode = new TagHelperIntermediateNode();
        var node = new PreallocatedTagHelperHtmlAttributeIntermediateNode()
        {
            VariableName = "_tagHelper1"
        };
        tagHelperNode.Children.Add(node);
        Push(context, tagHelperNode);

        // Act
        extension.WriteTagHelperHtmlAttribute(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"__tagHelperExecutionContext.AddHtmlAttribute(_tagHelper1);
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteTagHelperPropertyValue_RendersCorrectly()
    {
        // Arrange
        var extension = new PreallocatedAttributeTargetExtension();
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new PreallocatedTagHelperPropertyValueIntermediateNode(
            variableName: "_tagHelper1",
            attributeName: "Foo",
            value: "Bar",
            attributeStructure: AttributeStructure.DoubleQuotes);

        // Act
        extension.WriteTagHelperPropertyValue(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute _tagHelper1 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute(""Foo"", ""Bar"", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteTagHelperProperty_RendersCorrectly()
    {
        // Arrange
        var extension = new PreallocatedAttributeTargetExtension();
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
            fieldName: "__FooTagHelper",
            propertyName: "FooProp",
            variableName: "_tagHelper1",
            boundAttribute,
            tagHelper);

        tagHelperNode.Children.Add(node);
        Push(context, tagHelperNode);

        // Act
        extension.WriteTagHelperProperty(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"__FooTagHelper.FooProp = (string)_tagHelper1.Value;
__tagHelperExecutionContext.AddTagHelperAttribute(_tagHelper1);
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteSetPreallocatedTagHelperProperty_IndexerAttribute_RendersCorrectly()
    {
        // Arrange
        var extension = new PreallocatedAttributeTargetExtension();
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
            fieldName: "__FooTagHelper",
            propertyName: "FooProp",
            variableName: "_tagHelper1",
            boundAttribute,
            tagHelper,
            isIndexerNameMatch: true);

        tagHelperNode.Children.Add(node);
        Push(context, tagHelperNode);

        // Act
        extension.WriteTagHelperProperty(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"if (__FooTagHelper.FooProp == null)
{
    throw new InvalidOperationException(InvalidTagHelperIndexerAssignment(""pre-Foo"", ""FooTagHelper"", ""FooProp""));
}
__FooTagHelper.FooProp[""Foo""] = (string)_tagHelper1.Value;
__tagHelperExecutionContext.AddTagHelperAttribute(_tagHelper1);
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteSetPreallocatedTagHelperProperty_IndexerAttribute_MultipleValues()
    {
        // Arrange
        var extension = new PreallocatedAttributeTargetExtension();
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
            fieldName: "__FooTagHelper",
            propertyName: "FooProp",
            variableName: "_tagHelper0s",
            boundAttribute,
            tagHelper,
            isIndexerNameMatch: true);

        var node2 = new PreallocatedTagHelperPropertyIntermediateNode(
            attributeName: "pre-Foo",
            fieldName: "__FooTagHelper",
            propertyName: "FooProp",
            variableName: "_tagHelper1",
            boundAttribute,
            tagHelper,
            isIndexerNameMatch: true);

        tagHelperNode.Children.Add(node1);
        tagHelperNode.Children.Add(node2);
        Push(context, tagHelperNode);

        // Act
        extension.WriteTagHelperProperty(context, node2);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"__FooTagHelper.FooProp[""Foo""] = (string)_tagHelper1.Value;
__tagHelperExecutionContext.AddTagHelperAttribute(_tagHelper1);
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    private static void Push(CodeRenderingContext context, TagHelperIntermediateNode node)
    {
        context.PushAncestor(node);
    }
}
