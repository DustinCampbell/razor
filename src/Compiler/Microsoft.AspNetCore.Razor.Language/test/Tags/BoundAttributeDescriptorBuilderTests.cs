// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.CommonMetadata;

namespace Microsoft.AspNetCore.Razor.Language;

public class BoundAttributeDescriptorBuilderTests
{
    [Fact]
    public void DisplayName_SetsDescriptorsDisplayName()
    {
        // Arrange
        var expectedDisplayName = "ExpectedDisplayName";

        var builder = new TagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");
        builder.BindAttribute(a =>
        {
            a.PropertyName = "SomeProperty";
            a.TypeName = "System.String";
            a.DisplayName = expectedDisplayName;
        });

        // Act
        var descriptor = builder.Build();

        // Assert
        Assert.Equal(expectedDisplayName, descriptor.BoundAttributes[0].DisplayName);
    }

    [Fact]
    public void DisplayName_DefaultsToPropertyLookingDisplayName()
    {
        // Arrange
        var builder = new TagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");
        builder.Metadata(TypeName("TestTagHelper"));

        builder.BindAttribute(a =>
        {
            a.PropertyName = "SomeProperty";
            a.TypeName = typeof(int).FullName;
        });

        // Act
        var descriptor = builder.Build();

        // Assert
        Assert.Equal("int TestTagHelper.SomeProperty", descriptor.BoundAttributes[0].DisplayName);
    }

    [Fact]
    public void PropertyName_ReturnsPropertyName()
    {
        // Arrange
        var expectedPropertyName = "IntProperty";

        var builder = new TagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");
        builder.Metadata(TypeName("TestTagHelper"));

        builder.BindAttribute(a =>
        {
            a.Name = "test";
            a.PropertyName = expectedPropertyName;
            a.TypeName = typeof(int).FullName;
        });

        var descriptor = builder.Build();

        // Act
        var propertyName = descriptor.BoundAttributes[0].PropertyName;

        // Assert
        Assert.Equal(expectedPropertyName, propertyName);
    }

    [Fact]
    public void PropertyName_ReturnsNullIfNoPropertyName()
    {
        // Arrange
        var builder = new TagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");
        builder.Metadata(TypeName("TestTagHelper"));

        builder.BindAttribute(a =>
        {
            a.Name = "test";
            a.TypeName = typeof(int).FullName;
        });

        var descriptor = builder.Build();

        // Act
        var propertyName = descriptor.BoundAttributes[0].PropertyName;

        // Assert
        Assert.Null(propertyName);
    }

    [Fact]
    public void Metadata_Same()
    {
        // When SetMetadata is called on multiple builders with the same metadata collection,
        // they should share the instance.

        // Arrange
        var builder = new TagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");

        var metadata = MetadataCollection.Create(new KeyValuePair<string, string?>("Key", "Value"));

        builder.BindAttribute(a =>
        {
            a.PropertyName = "SomeProperty1";
            a.TypeName = typeof(int).FullName;
            a.SetMetadata(metadata);
        });

        builder.BindAttribute(a =>
        {
            a.PropertyName = "SomeProperty2";
            a.TypeName = typeof(int).FullName;
            a.SetMetadata(metadata);
        });

        // Act
        var descriptor = builder.Build();

        // Assert
        Assert.Same(descriptor.BoundAttributes[0].Metadata, descriptor.BoundAttributes[1].Metadata);
    }

    [Fact]
    public void Metadata_NotSame()
    {
        // When Metadata is accessed on multiple builders with the same metadata,
        // they do not share the instance.

        // Arrange
        var builder = new TagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");

        builder.BindAttribute(a =>
        {
            a.PropertyName = "SomeProperty1";
            a.TypeName = typeof(int).FullName;
            a.Metadata.Add(new KeyValuePair<string, string?>("Key", "Value"));
        });

        builder.BindAttribute(a =>
        {
            a.PropertyName = "SomeProperty2";
            a.TypeName = typeof(int).FullName;
            a.Metadata.Add(new KeyValuePair<string, string?>("Key", "Value"));
        });

        var descriptor = builder.Build();

        // Assert
        Assert.NotSame(descriptor.BoundAttributes[0].Metadata, descriptor.BoundAttributes[1].Metadata);
    }
}
