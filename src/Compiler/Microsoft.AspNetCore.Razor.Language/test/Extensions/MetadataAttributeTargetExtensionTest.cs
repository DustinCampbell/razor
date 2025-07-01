// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
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

        var node = new RazorCompiledItemAttributeIntermediateNode()
        {
            TypeName = "Foo.Bar",
            Kind = "test",
            Identifier = "Foo/Bar",
        };

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

        var node = new RazorSourceChecksumAttributeIntermediateNode()
        {
            ChecksumAlgorithm = CodeAnalysis.Text.SourceHashAlgorithm.Sha256,
            Checksum = [(byte)'t', (byte)'e', (byte)'s', (byte)'t'],
            Identifier = "Foo/Bar",
        };

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

        var node = new RazorCompiledItemMetadataAttributeIntermediateNode
        {
            Key = "key",
            Value = "value",
        };

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

        var node = new RazorCompiledItemMetadataAttributeIntermediateNode
        {
            Key = "\"test\" key",
            Value = @"""test"" value",
        };

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
