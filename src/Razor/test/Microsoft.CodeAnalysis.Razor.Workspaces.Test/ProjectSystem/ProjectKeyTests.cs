// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common.Workspaces;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.Test.ProjectSystem;

public class ProjectKeyTests(ITestOutputHelper testOutput) : WorkspaceTestBase(testOutput)
{
    [Theory]
    [InlineData("/path/to/dir", @"\path\to\dir")]
    [InlineData("/path%2Fto/dir", @"\path\to\dir")]
    [InlineData(@"\path\to\dir\", @"\path\to\dir")]
    [InlineData(@"\path%5Cto\dir\", @"\path\to\dir")]
    public void EqualityTests(string id1, string id2)
    {
        var key1 = new ProjectKey(id1);
        var key2 = new ProjectKey(id2);

        // I'm covering all bases out of a complete lack of trust in compilers
        Assert.True(key1 == key2);
        Assert.True(key1.Equals(key2));
        Assert.True(key1.Equals((object)key2));
        Assert.Equal(key1, key2);

        // And just for good measure, has boolean logic changed?
        Assert.False(key1 != key2);
        Assert.False(!key1.Equals(key2));
        Assert.False(!key1.Equals((object)key2));
    }

    [ConditionalTheory(Is.Windows)]
    [InlineData(@"/c:/path/to/dir/", @"c:\path\to\dir")]
    [InlineData(@"/c:\path/to\dir/", @"c:\path\to\dir")]
    [InlineData(@"\path\to\dir\", @"path\to\dir")]
    [InlineData("/path/to/dir", @"path\to\dir")]
    [InlineData("path/to/dir", @"\path\to\dir")]
    [InlineData(@"C:\path\to\dir\", @"c:\path\to\dir")]
    [InlineData(@"\PATH\TO\DIR\", @"\path\to\dir")]
    public void EqualityTests_Windows(string id1, string id2)
    {
        var key1 = new ProjectKey(id1);
        var key2 = new ProjectKey(id2);

        // I'm covering all bases out of a complete lack of trust in compilers
        Assert.True(key1 == key2);
        Assert.True(key1.Equals(key2));
        Assert.True(key1.Equals((object)key2));
        Assert.Equal(key1, key2);

        // And just for good measure, has boolean logic changed?
        Assert.False(key1 != key2);
        Assert.False(!key1.Equals(key2));
        Assert.False(!key1.Equals((object)key2));
    }

    [Theory]
    [InlineData("/path/to/other/dir", @"\path\to\dir")]
    [InlineData("path/to/other/dir", @"\path\to\dir")]
    [InlineData("/path/to/other/dir", @"path\to\dir")]
    [InlineData(@"\path\to\other\dir\", @"\path\to\dir")]
    [InlineData(@"\path\to\other\dir\", @"path\to\dir")]
    [InlineData(@"\PATH\TO\OTHER\DIR\", @"\path\to\dir")]
    public void InequalityTests(string id1, string id2)
    {
        var key1 = new ProjectKey(id1);
        var key2 = new ProjectKey(id2);

        // I'm covering all bases out of a complete lack of trust in compilers
        Assert.False(key1 == key2);
        Assert.False(key1.Equals(key2));
        Assert.False(key1.Equals((object)key2));
        Assert.NotEqual(key1, key2);

        // And just for good measure, has boolean logic changed?
        Assert.True(key1 != key2);
        Assert.True(!key1.Equals(key2));
        Assert.True(!key1.Equals((object)key2));
    }

    [Fact]
    public void RoslynProjectToRazorProject()
    {
        var intermediateOutputPath = @"c:\project\obj";
        var assemblyPath = Path.Combine(intermediateOutputPath, "project.dll");

        var projectInfo = ProjectInfo
            .Create(ProjectId.CreateNewId(), VersionStamp.Default, "Project", "Assembly", "C#")
            .WithCompilationOutputInfo(new CompilationOutputInfo().WithAssemblyPath(assemblyPath));
        var project = Workspace.CurrentSolution
            .AddProject(projectInfo)
            .GetRequiredProject(projectInfo.Id);

        var razorKey = new ProjectKey(intermediateOutputPath);
        Assert.True(razorKey.Matches(project));

        var roslynKey = project.ToProjectKey();
        Assert.Equal(razorKey, roslynKey);
    }

    [Fact]
    public void ComparisonTest()
    {
        IEnumerable<ProjectKey> expected = [
            new("/c:/path/razor/project"),
            new("/c:/razor/project"),
            new("/d:/path/blazor/project"),
            new("/d:/path/razor/project"),
            ProjectKey.Unknown];

        IEnumerable<ProjectKey> keys = [
            new("/d:/path/blazor/project"),
            new("/c:/path/razor/project"),
            ProjectKey.Unknown,
            new("/d:/path/razor/project"),
            new("/c:/razor/project")];

        var actual = keys.OrderBy(x => x);

        Assert.Equal(expected, actual);
    }
}
