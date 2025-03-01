// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class DefaultRazorProjectItemTest
{
    private static string TestFolder { get; } = Path.Combine(
        TestProject.GetProjectDirectory(typeof(DefaultRazorProjectFileSystemTest), layer: TestProject.Layer.Compiler),
        "TestFiles",
        "DefaultRazorProjectFileSystem");

    [Fact]
    public void DefaultRazorProjectItem_SetsProperties()
    {
        // Arrange
        var relativePhysicalPath = "Home.cshtml";
        var physicalPath = Path.Combine(TestFolder, relativePhysicalPath);

        // Act
        var projectItem = new DefaultRazorProjectItem(
            basePath: "/",
            filePath: "/Home.cshtml",
            physicalPath,
            relativePhysicalPath,
            RazorSourceCodeKind.ComponentImport,
            "MyCssScope");

        // Assert
        Assert.Equal("/Home.cshtml", projectItem.FilePath);
        Assert.Equal("/", projectItem.BasePath);
        Assert.True(projectItem.Exists);
        Assert.Equal("Home.cshtml", projectItem.FileName);
        Assert.Equal(RazorSourceCodeKind.ComponentImport, projectItem.SourceCodeKind);
        Assert.Equal(physicalPath, projectItem.PhysicalPath);
        Assert.Equal("Home.cshtml", projectItem.RelativePhysicalPath);
        Assert.Equal("MyCssScope", projectItem.CssScope);
    }

    [Fact]
    public void DefaultRazorProjectItem_InfersFileKind_Component()
    {
        // Arrange
        var relativePhysicalPath = "Home.cshtml";
        var physicalPath = Path.Combine(TestFolder, relativePhysicalPath);

        // Act
        var projectItem = new DefaultRazorProjectItem(
            basePath: "/",
            filePath: "/Home.razor",
            physicalPath,
            relativePhysicalPath,
            sourceCodeKind: RazorSourceCodeKind.None,
            cssScope: null);

        // Assert
        Assert.Equal(RazorSourceCodeKind.Component, projectItem.SourceCodeKind);
    }

    [Fact]
    public void DefaultRazorProjectItem_InfersFileKind_Legacy()
    {
        // Arrange
        var relativePhysicalPath = "Home.cshtml";
        var physicalPath = Path.Combine(TestFolder, relativePhysicalPath);

        // Act
        var projectItem = new DefaultRazorProjectItem(
            basePath: "/",
            filePath: "/Home.cshtml",
            physicalPath,
            relativePhysicalPath,
            sourceCodeKind: RazorSourceCodeKind.None,
            cssScope: null);

        // Assert
        Assert.Equal(RazorSourceCodeKind.Legacy, projectItem.SourceCodeKind);
    }

    [Fact]
    public void DefaultRazorProjectItem_InfersFileKind_Null()
    {
        // Arrange
        var relativePhysicalPath = "Home.cshtml";
        var physicalPath = Path.Combine(TestFolder, relativePhysicalPath);

        // Act
        var projectItem = new DefaultRazorProjectItem(
            basePath: "/",
            filePath: null!,
            physicalPath,
            relativePhysicalPath,
            sourceCodeKind: RazorSourceCodeKind.None,
            cssScope: null);

        // Assert
        Assert.Equal(RazorSourceCodeKind.None, projectItem.SourceCodeKind);
    }

    [Fact]
    public void Exists_ReturnsFalseWhenFileDoesNotExist()
    {
        // Arrange
        var relativePhysicalPath = Path.Combine("Views", "FileDoesNotExist.cshtml");
        var physicalPath = Path.Combine(TestFolder, relativePhysicalPath);

        // Act
        var projectItem = new DefaultRazorProjectItem(
            basePath: "/Views",
            filePath: "/FileDoesNotExist.cshtml",
            physicalPath,
            relativePhysicalPath,
            sourceCodeKind: RazorSourceCodeKind.None,
            cssScope: null);

        // Assert
        Assert.False(projectItem.Exists);
    }

    [Fact]
    public void Read_ReturnsReadStream()
    {
        // Arrange
        var relativePhysicalPath = "Home.cshtml";
        var physicalPath = Path.Combine(TestFolder, relativePhysicalPath);

        var projectItem = new DefaultRazorProjectItem(
            "/",
            "/Home.cshtml",
            physicalPath,
            relativePhysicalPath,
            sourceCodeKind: RazorSourceCodeKind.None,
            cssScope: null);

        // Act
        var stream = projectItem.Read();

        // Assert
        Assert.Equal("home-content", new StreamReader(stream).ReadToEnd());
    }
}
