// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class RazorProjectItemTest
{
    [Fact]
    public void CombinedPath_ReturnsPathIfBasePathIsEmpty()
    {
        // Arrange
        var emptyBasePath = "/";
        var path = "/foo/bar.cshtml";
        var projectItem = new TestRazorProjectItem(path, basePath: emptyBasePath);

        // Act
        var combinedPath = projectItem.CombinedPath;

        // Assert
        Assert.Equal(path, combinedPath);
    }

    [Theory]
    [InlineData("/root", "/root/foo/bar.cshtml")]
    [InlineData("root/subdir", "root/subdir/foo/bar.cshtml")]
    public void CombinedPath_ConcatsPaths(string basePath, string expected)
    {
        // Arrange
        var path = "/foo/bar.cshtml";
        var projectItem = new TestRazorProjectItem(path, basePath: basePath);

        // Act
        var combinedPath = projectItem.CombinedPath;

        // Assert
        Assert.Equal(expected, combinedPath);
    }
}
