// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public class MetadataAttributeTargetExtensionTest
{
    [Fact]
    public void WriteRazorCompiledItemAttribute_RendersCorrectly()
    {
        // Arrange
        var extension = new MetadataAttributeTargetExtension();

        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new RazorCompiledItemAttributeIntermediateNode()
        {
            TypeName = "Foo.Bar",
            Kind = "test",
            Identifier = "Foo/Bar",
        };

        // Act
        extension.WriteRazorCompiledItemAttribute(context, node);

        // Assert
        var csharpText = context.CodeWriter.GetText().ToString();
        Assert.Equal($"""
            [assembly: {MetadataAttributeTargetExtension.CompiledItemAttributeName}(typeof(Foo.Bar), @"test", @"Foo/Bar")]
            """,
            csharpText.TrimEnd(),
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteRazorSourceChecksumAttribute_RendersCorrectly()
    {
        // Arrange
        var extension = new MetadataAttributeTargetExtension();

        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new RazorSourceChecksumAttributeIntermediateNode()
        {
            ChecksumAlgorithm = CodeAnalysis.Text.SourceHashAlgorithm.Sha256,
            Checksum = [(byte)'t', (byte)'e', (byte)'s', (byte)'t'],
            Identifier = "Foo/Bar",
        };

        // Act
        extension.WriteRazorSourceChecksumAttribute(context, node);

        // Assert
        var csharpText = context.CodeWriter.GetText().ToString();
        Assert.Equal($"""
            [{MetadataAttributeTargetExtension.SourceChecksumAttributeName}(@"Sha256", @"74657374", @"Foo/Bar")]
            """,
            csharpText.TrimEnd(),
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteRazorCompiledItemAttributeMetadata_RendersCorrectly()
    {
        // Arrange
        var extension = new MetadataAttributeTargetExtension();

        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new RazorCompiledItemMetadataAttributeIntermediateNode
        {
            Key = "key",
            Value = "value",
        };

        // Act
        extension.WriteRazorCompiledItemMetadataAttribute(context, node);

        // Assert
        var csharpText = context.CodeWriter.GetText().ToString().Trim();
        Assert.Equal($"""
            [{MetadataAttributeTargetExtension.CompiledItemMetadataAttributeName}("key", "value")]
            """,
            csharpText.TrimEnd(),
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteRazorCompiledItemAttributeMetadata_EscapesKeysAndValuesCorrectly()
    {
        // Arrange
        var extension = new MetadataAttributeTargetExtension();

        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new RazorCompiledItemMetadataAttributeIntermediateNode
        {
            Key = "\"test\" key",
            Value = @"""test"" value",
        };

        // Act
        extension.WriteRazorCompiledItemMetadataAttribute(context, node);

        // Assert
        var csharpText = context.CodeWriter.GetText().ToString().Trim();
        Assert.Equal($"""
            [{MetadataAttributeTargetExtension.CompiledItemMetadataAttributeName}("\"test\" key", "\"test\" value")]
            """,
            csharpText.TrimEnd(),
            ignoreLineEndingDifferences: true);
    }
}
