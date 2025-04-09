// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class DefaultRazorProjectItemTest
{
    private const string BasePath = "/";

    private static string TestFolder { get; } = Path.Combine(
        TestProject.GetProjectDirectory(typeof(DefaultRazorProjectFileSystemTest), layer: TestProject.Layer.Compiler),
        "TestFiles",
        "DefaultRazorProjectFileSystem");

    [Fact]
    public void DefaultRazorProjectItem_SetsProperties()
    {
        // Arrange
        const string FileName = "Home.cshtml";
        const string CssScope = "MyCssScope";

        var filePath = BasePath + FileName; // /Home.cshtml
        var physicalFilePath = Path.Combine(TestFolder, FileName);

        // Act
        var projectItem = new DefaultRazorProjectItem(
            BasePath, filePath, physicalFilePath, relativePhysicalPath: FileName, RazorFileKind.Legacy, cssScope: CssScope);

        // Assert
        Assert.Equal(filePath, projectItem.FilePath);
        Assert.Equal(BasePath, projectItem.BasePath);
        Assert.True(projectItem.Exists);
        Assert.Equal(RazorFileKind.Legacy, projectItem.FileKind);
        Assert.Equal(physicalFilePath, projectItem.PhysicalPath);
        Assert.Equal(FileName, projectItem.RelativePhysicalPath);
        Assert.Equal(CssScope, projectItem.CssScope);
    }

    [Fact]
    public void DefaultRazorProjectItem_InfersFileKind_Component()
    {
        // Arrange
        const string FileName = "Home.razor";

        var filePath = BasePath + FileName; // /Home.razor
        var physicalFilePath = Path.Combine(TestFolder, FileName);

        // Act
        var projectItem = new DefaultRazorProjectItem(
            BasePath, filePath, physicalFilePath, relativePhysicalPath: FileName, fileKind: null, cssScope: null);

        // Assert
        Assert.Equal(RazorFileKind.Component, projectItem.FileKind);
    }

    [Fact]
    public void DefaultRazorProjectItem_InfersFileKind_Legacy()
    {
        // Arrange
        const string FileName = "Home.cshtml";

        var filePath = BasePath + FileName; // /Home.cshtml
        var physicalFilePath = Path.Combine(TestFolder, FileName);

        // Act
        var projectItem = new DefaultRazorProjectItem(
            BasePath, filePath, physicalFilePath, relativePhysicalPath: FileName, fileKind: null, cssScope: null);

        // Assert
        Assert.Equal(RazorFileKind.Legacy, projectItem.FileKind);
    }

    [Fact]
    public void DefaultRazorProjectItem_InfersFileKind_None()
    {
        // Arrange
        const string FileName = "Home.cshtml";

        var physicalFilePath = Path.Combine(TestFolder, FileName);

        // Act
        var projectItem = new DefaultRazorProjectItem(
            BasePath, filePath: null, physicalFilePath, relativePhysicalPath: FileName, fileKind: null, cssScope: null);

        // Assert
        Assert.Equal(RazorFileKind.None, projectItem.FileKind);
    }

    [Fact]
    public void Exists_ReturnsFalseWhenFileDoesNotExist()
    {
        // Arrange
        const string BasePath = "/Views";
        const string FileName = "FileDoesNotExist.cshtml";

        var filePath = BasePath + FileName; // /Home.cshtml
        var physicalFilePath = Path.Combine(TestFolder, "Views", FileName);
        var relativePhysicalPath = Path.Combine("Views", FileName);

        // Act
        var projectItem = new DefaultRazorProjectItem(
            BasePath, filePath, physicalFilePath, relativePhysicalPath, fileKind: null, cssScope: null);

        // Assert
        Assert.False(projectItem.Exists);
    }

    [Fact]
    public void Read_ReturnsReadStream()
    {
        // Arrange
        const string FileName = "Home.cshtml";

        var filePath = BasePath + FileName; // /Home.cshtml
        var physicalFilePath = Path.Combine(TestFolder, FileName);
        var projectItem = new DefaultRazorProjectItem(
            BasePath, filePath, physicalFilePath, relativePhysicalPath: FileName, fileKind: null, cssScope: null);

        // Act
        var stream = projectItem.Read();

        // Assert
        using var reader = new StreamReader(stream);
        Assert.Equal("home-content", reader.ReadToEnd());
    }
}
