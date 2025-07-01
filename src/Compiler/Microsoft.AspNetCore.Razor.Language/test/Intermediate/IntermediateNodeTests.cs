// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.CodeAnalysis.Text;
using Xunit;

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
            [assembly: {RazorCompiledItemAttributeIntermediateNode.AttributeName}(typeof(Foo.Bar), @"test", @"Foo/Bar")]
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
            [{RazorSourceChecksumAttributeIntermediateNode.AttributeName}(@"Sha256", @"74657374", @"Foo/Bar")]
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
            [{RazorCompiledItemMetadataAttributeIntermediateNode.AttributeName}("key", "value")]
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
            [{RazorCompiledItemMetadataAttributeIntermediateNode.AttributeName}("\"test\" key", "\"test\" value")]
            """,
            csharpText);
    }

}
