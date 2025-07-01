// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public class RazorSourceDocumentExtensionsTest
{
    [Fact]
    public void GetIdentifier_ReturnsNull_ForNullRelativePath()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("content", filePath: "Test.cshtml", relativePath: null);

        // Act
        var result = source.GetIdentifier();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetIdentifier_ReturnsNull_ForEmptyRelativePath()
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("content", filePath: "Test.cshtml", relativePath: string.Empty);

        // Act
        var result = source.GetIdentifier();

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("Test.cshtml", "/Test.cshtml")]
    [InlineData("/Test.cshtml", "/Test.cshtml")]
    [InlineData("\\Test.cshtml", "/Test.cshtml")]
    [InlineData("\\About\\Test.cshtml", "/About/Test.cshtml")]
    [InlineData("\\About\\Test\\cshtml", "/About/Test/cshtml")]
    public void GetIdentifier_SanitizesRelativePath(string relativePath, string expected)
    {
        // Arrange
        var source = TestRazorSourceDocument.Create("content", filePath: "Test.cshtml", relativePath: relativePath);

        // Act
        var result = source.GetIdentifier();

        // Assert
        Assert.Equal(expected, result);
    }
}
