// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.Extensions.MetadataAttributeTargetExtension;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public class MetadataAttributeTargetExtensionTest
{
    [Fact]
    public void WriteRazorCompiledItemAttribute_RendersCorrectly()
    {
        // Arrange
        var extension = new MetadataAttributeTargetExtension();
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new RazorCompiledItemAttributeIntermediateNode(
            typeName: "Foo.Bar",
            kind: "test",
            identifier: "Foo/Bar");

        // Act
        extension.WriteRazorCompiledItemAttribute(context, node);

        // Assert
        var csharpText = context.CodeWriter.GetText().ToString().TrimEnd();
        Assert.Equal($"""
            [assembly: {CompiledItemAttributeName}(typeof(Foo.Bar), @"test", @"Foo/Bar")]
            """,
            csharpText);
    }

    [Fact]
    public void WriteRazorSourceChecksumAttribute_RendersCorrectly()
    {
        // Arrange
        var extension = new MetadataAttributeTargetExtension();
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new RazorSourceChecksumAttributeIntermediateNode(
            checksum: [(byte)'t', (byte)'e', (byte)'s', (byte)'t'],
            checksumAlgorithm: SourceHashAlgorithm.Sha256,
            identifier: "Foo/Bar");

        // Act
        extension.WriteRazorSourceChecksumAttribute(context, node);

        // Assert
        var csharpText = context.CodeWriter.GetText().ToString().TrimEnd();
        Assert.Equal($"""
            [{SourceChecksumAttributeName}(@"Sha256", @"74657374", @"Foo/Bar")]
            """,
            csharpText);
    }

    [Fact]
    public void WriteRazorCompiledItemAttributeMetadata_RendersCorrectly()
    {
        // Arrange
        var extension = new MetadataAttributeTargetExtension();
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new RazorCompiledItemMetadataAttributeIntermediateNode(key: "key", value: "value");

        // Act
        extension.WriteRazorCompiledItemMetadataAttribute(context, node);

        // Assert
        var csharpText = context.CodeWriter.GetText().ToString().TrimEnd();
        Assert.Equal($"""
            [{CompiledItemMetadataAttributeName}("key", "value")]
            """,
            csharpText);
    }

    [Fact]
    public void WriteRazorCompiledItemAttributeMetadata_EscapesKeysAndValuesCorrectly()
    {
        // Arrange
        var extension = new MetadataAttributeTargetExtension();
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new RazorCompiledItemMetadataAttributeIntermediateNode(key: @"""test"" key", value: @"""test"" value");

        // Act
        extension.WriteRazorCompiledItemMetadataAttribute(context, node);

        // Assert
        var csharpText = context.CodeWriter.GetText().ToString().TrimEnd();
        Assert.Equal($"""
            [{CompiledItemMetadataAttributeName}("\"test\" key", "\"test\" value")]
            """,
            csharpText);
    }
}
