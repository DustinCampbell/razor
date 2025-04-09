// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Xunit;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators
{
    public class SourceGeneratorProjectItemTest
    {
        [Fact]
        public void PhysicalPath_ReturnsSourceTextPath()
        {
            // Arrange
            var emptyBasePath = "/";
            var path = "/foo/bar.cshtml";
            var projectItem = new SourceGeneratorProjectItem(
                filePath: path,
                basePath: emptyBasePath,
                relativePhysicalPath: "/foo",
                fileKind: RazorFileKind.Legacy,
                additionalText: new TestAdditionalText(string.Empty),
                cssScope: null);

            // Act
            var physicalPath = projectItem.PhysicalPath;

            // Assert
            Assert.Equal("dummy", physicalPath);
        }
    }
}
