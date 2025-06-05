// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using static Microsoft.AspNetCore.Razor.Language.CommonMetadata;

namespace Microsoft.AspNetCore.Razor.Language;

public class BoundAttributeDescriptorBuilderTests
{
    [Fact]
    public void GetPropertyName_ReturnsPropertyName()
    {
        // Arrange
        var expectedPropertyName = "IntProperty";

        var tagHelperBuilder = new TagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");
        tagHelperBuilder.Metadata(TypeName("TestTagHelper"));

        var builder = new BoundAttributeDescriptorBuilder(tagHelperBuilder, TagHelperConventions.DefaultKind);
        builder
            .Name("test")
            .PropertyName(expectedPropertyName)
            .TypeName(typeof(int).FullName);

        var descriptor = builder.Build();

        // Act
        var propertyName = descriptor.PropertyName;

        // Assert
        Assert.Equal(expectedPropertyName, propertyName);
    }

    [Fact]
    public void GetPropertyName_ReturnsNullIfNoPropertyName()
    {
        // Arrange
        var tagHelperBuilder = new TagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");
        tagHelperBuilder.Metadata(TypeName("TestTagHelper"));

        var builder = new BoundAttributeDescriptorBuilder(tagHelperBuilder, TagHelperConventions.DefaultKind);
        builder
            .Name("test")
            .TypeName(typeof(int).FullName);

        var descriptor = builder.Build();

        // Act
        var propertyName = descriptor.PropertyName;

        // Assert
        Assert.Null(propertyName);
    }
}
