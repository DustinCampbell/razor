// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Xunit;
using static Microsoft.AspNetCore.Razor.Language.CommonMetadata;

namespace Microsoft.AspNetCore.Razor.Language;

public class BoundAttributeDescriptorExtensionsTest
{
    [Fact]
    public void IsDefaultKind_ReturnsTrue_IfKindIsDefault()
    {
        // Arrange
        var builder = new TagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");
        builder.Metadata(TypeName("TestTagHelper"));

        builder.BindAttribute(static a =>
        {
            a.Name = "test";
            a.PropertyName = "IntProperty";
            a.TypeName = typeof(int).FullName;
        });

        var descriptor = builder.Build();

        // Act
        var isDefault = descriptor.BoundAttributes[0].IsDefaultKind();

        // Assert
        Assert.True(isDefault);
    }

    [Fact]
    public void IsDefaultKind_ReturnsFalse_IfKindIsNotDefault()
    {
        // Arrange
        var builder = new TagHelperDescriptorBuilder("other-kind", "TestTagHelper", "Test");
        builder.Metadata(TypeName("TestTagHelper"));

        builder.BindAttribute(static a =>
        {
            a.Name = "test";
            a.PropertyName = "IntProperty";
            a.TypeName = typeof(int).FullName;
        });

        var descriptor = builder.Build();

        // Act
        var isDefault = descriptor.BoundAttributes[0].IsDefaultKind();

        // Assert
        Assert.False(isDefault);
    }

    [Fact]
    public void ExpectsStringValue_ReturnsTrue_ForStringProperty()
    {
        // Arrange
        var builder = new TagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");
        builder.Metadata(TypeName("TestTagHelper"));

        builder.BindAttribute(static a =>
        {
            a.Name = "test";
            a.PropertyName = "BoundProp";
            a.TypeName = typeof(string).FullName;
        });

        var descriptor = builder.Build();

        // Act
        var result = descriptor.BoundAttributes[0].ExpectsStringValue("test");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ExpectsStringValue_ReturnsFalse_ForNonStringProperty()
    {
        // Arrange
        var builder = new TagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");
        builder.Metadata(TypeName("TestTagHelper"));

        builder.BindAttribute(static a =>
        {
            a.Name = "test";
            a.PropertyName = "BoundProp";
            a.TypeName = typeof(bool).FullName;
        });

        var descriptor = builder.Build();

        // Act
        var result = descriptor.BoundAttributes[0].ExpectsStringValue("test");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ExpectsStringValue_ReturnsTrue_StringIndexerAndNameMatch()
    {
        // Arrange
        var builder = new TagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");
        builder.Metadata(TypeName("TestTagHelper"));

        builder.BindAttribute(static a =>
        {
            a.Name = "test";
            a.PropertyName = "BoundProp";
            a.TypeName = "System.Collection.Generic.IDictionary<string, string>";
            a.AsDictionary("prefix-test-", typeof(string).FullName);
        });

        var descriptor = builder.Build();

        // Act
        var result = descriptor.BoundAttributes[0].ExpectsStringValue("prefix-test-key");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ExpectsStringValue_ReturnsFalse_StringIndexerAndNameMismatch()
    {
        // Arrange
        var builder = new TagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");
        builder.Metadata(TypeName("TestTagHelper"));

        builder.BindAttribute(static a =>
        {
            a.Name = "test";
            a.PropertyName = "BoundProp";
            a.TypeName = "System.Collection.Generic.IDictionary<string, string>";
            a.AsDictionary("prefix-test-", typeof(string).FullName);
        });

        var descriptor = builder.Build();

        // Act
        var result = descriptor.BoundAttributes[0].ExpectsStringValue("test");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ExpectsBooleanValue_ReturnsTrue_ForBooleanProperty()
    {
        // Arrange
        var builder = new TagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");
        builder.Metadata(TypeName("TestTagHelper"));

        builder.BindAttribute(static a =>
        {
            a.Name = "test";
            a.PropertyName = "BoundProp";
            a.TypeName = typeof(bool).FullName;
        });

        var descriptor = builder.Build();

        // Act
        var result = descriptor.BoundAttributes[0].ExpectsBooleanValue("test");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ExpectsBooleanValue_ReturnsFalse_ForNonBooleanProperty()
    {
        // Arrange
        var builder = new TagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");
        builder.Metadata(TypeName("TestTagHelper"));

        builder.BindAttribute(static a =>
        {
            a.Name = "test";
            a.PropertyName = "BoundProp";
            a.TypeName = typeof(int).FullName;
        });

        var descriptor = builder.Build();

        // Act
        var result = descriptor.BoundAttributes[0].ExpectsBooleanValue("test");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ExpectsBooleanValue_ReturnsTrue_BooleanIndexerAndNameMatch()
    {
        // Arrange
        var builder = new TagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");
        builder.Metadata(TypeName("TestTagHelper"));

        builder.BindAttribute(static a =>
        {
            a.Name = "test";
            a.PropertyName = "BoundProp";
            a.TypeName = "System.Collection.Generic.IDictionary<string, bool>";
            a.AsDictionary("prefix-test-", typeof(bool).FullName);
        });

        var descriptor = builder.Build();

        // Act
        var result = descriptor.BoundAttributes[0].ExpectsBooleanValue("prefix-test-key");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ExpectsBooleanValue_ReturnsFalse_BooleanIndexerAndNameMismatch()
    {
        // Arrange
        var builder = new TagHelperDescriptorBuilder(TagHelperConventions.DefaultKind, "TestTagHelper", "Test");
        builder.Metadata(TypeName("TestTagHelper"));

        builder.BindAttribute(static a =>
        {
            a.Name = "test";
            a.PropertyName = "BoundProp";
            a.TypeName = "System.Collection.Generic.IDictionary<string, bool>";
            a.AsDictionary("prefix-test-", typeof(bool).FullName);
        });

        var descriptor = builder.Build();

        // Act
        var result = descriptor.BoundAttributes[0].ExpectsBooleanValue("test");

        // Assert
        Assert.False(result);
    }
}
