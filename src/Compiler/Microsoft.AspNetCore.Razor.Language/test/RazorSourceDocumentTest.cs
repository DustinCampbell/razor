// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class RazorSourceDocumentTest
{
    [Fact]
    public void ReadFrom_ProjectItem()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem("filePath.cshtml", "c:\\myapp\\filePath.cshtml", "filePath.cshtml", "c:\\myapp\\");

        // Act
        var document = RazorSourceDocument.ReadFrom(projectItem);

        // Assert
        Assert.Equal("c:\\myapp\\filePath.cshtml", document.FilePath);
        Assert.Equal("filePath.cshtml", document.RelativePath);
        Assert.Equal(projectItem.Content, ReadContent(document));
    }

    [Fact]
    public void ReadFrom_ProjectItem_NoRelativePath()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem("filePath.cshtml", "c:\\myapp\\filePath.cshtml", basePath: "c:\\myapp\\");

        // Act
        var document = RazorSourceDocument.ReadFrom(projectItem);

        // Assert
        Assert.Equal("c:\\myapp\\filePath.cshtml", document.FilePath);
        Assert.Equal("filePath.cshtml", document.RelativePath);
        Assert.Equal(projectItem.Content, ReadContent(document));
    }

    [Fact]
    public void ReadFrom_ProjectItem_FallbackToRelativePath()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem("filePath.cshtml", relativePhysicalPath: "filePath.cshtml", basePath: "c:\\myapp\\");

        // Act
        var document = RazorSourceDocument.ReadFrom(projectItem);

        // Assert
        Assert.Equal("filePath.cshtml", document.FilePath);
        Assert.Equal("filePath.cshtml", document.RelativePath);
        Assert.Equal(projectItem.Content, ReadContent(document));
    }

    [Fact]
    public void ReadFrom_ProjectItem_FallbackToFileName()
    {
        // Arrange
        var projectItem = new TestRazorProjectItem("filePath.cshtml", basePath: "c:\\myapp\\");

        // Act
        var document = RazorSourceDocument.ReadFrom(projectItem);

        // Assert
        Assert.Equal("filePath.cshtml", document.FilePath);
        Assert.Equal("filePath.cshtml", document.RelativePath);
        Assert.Equal(projectItem.Content, ReadContent(document));
    }

    [Fact]
    public void ReadFrom_WithProjectItem_FallbackToFilePath_WhenRelativePhysicalPathIsNull()
    {
        // Arrange
        var filePath = "filePath.cshtml";
        var projectItem = new TestRazorProjectItem(filePath, relativePhysicalPath: null);

        // Act
        var document = RazorSourceDocument.ReadFrom(projectItem);

        // Assert
        Assert.Equal(filePath, document.FilePath);
        Assert.Equal(filePath, document.RelativePath);
    }

    [Fact]
    public void ReadFrom_WithProjectItem_UsesRelativePhysicalPath()
    {
        // Arrange
        var filePath = "filePath.cshtml";
        var relativePhysicalPath = "relative-path.cshtml";
        var projectItem = new TestRazorProjectItem(filePath, relativePhysicalPath: relativePhysicalPath);

        // Act
        var document = RazorSourceDocument.ReadFrom(projectItem);

        // Assert
        Assert.Equal(relativePhysicalPath, document.FilePath);
        Assert.Equal(relativePhysicalPath, document.RelativePath);
    }

    private static string ReadContent(RazorSourceDocument razorSourceDocument) => razorSourceDocument.Text.ToString();
}
