﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

public class DocumentSnapshotTest : WorkspaceTestBase
{
    private static readonly HostDocument s_componentHostDocument = TestProjectData.SomeProjectComponentFile1;
    private static readonly HostDocument s_componentCshtmlHostDocument = TestProjectData.SomeProjectCshtmlComponentFile5;
    private static readonly HostDocument s_legacyHostDocument = TestProjectData.SomeProjectFile1;
    private static readonly HostDocument s_nestedComponentHostDocument = TestProjectData.SomeProjectNestedComponentFile3;

    private readonly SourceText _sourceText;
    private readonly VersionStamp _version;
    private readonly DocumentSnapshot _componentDocument;
    private readonly DocumentSnapshot _componentCshtmlDocument;
    private readonly DocumentSnapshot _legacyDocument;
    private readonly DocumentSnapshot _nestedComponentDocument;

    public DocumentSnapshotTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
        _sourceText = SourceText.From("<p>Hello World</p>");
        _version = VersionStamp.Create();

        var textLoader = TestMocks.CreateTextLoader(_sourceText, _version);

        var solutionState = SolutionState
            .Create(ProjectEngineFactoryProvider, LanguageServerFeatureOptions)
            .AddProject(TestProjectData.SomeProject)
            .AddDocument(TestProjectData.SomeProject.Key, s_legacyHostDocument, textLoader)
            .AddDocument(TestProjectData.SomeProject.Key, s_componentHostDocument, textLoader)
            .AddDocument(TestProjectData.SomeProject.Key, s_componentCshtmlHostDocument, textLoader)
            .AddDocument(TestProjectData.SomeProject.Key, s_nestedComponentHostDocument, textLoader);

        var solution = new SolutionSnapshot(solutionState);
        var project = solution.GetRequiredProject(TestProjectData.SomeProject.Key);

        _legacyDocument = project.GetDocument(s_legacyHostDocument.FilePath).AssumeNotNull();
        _componentDocument = project.GetDocument(s_componentHostDocument.FilePath).AssumeNotNull();
        _componentCshtmlDocument = project.GetDocument(s_componentCshtmlHostDocument.FilePath).AssumeNotNull();
        _nestedComponentDocument = project.GetDocument(s_nestedComponentHostDocument.FilePath).AssumeNotNull();
    }

    [Fact]
    public async Task GCCollect_OutputIsNoLongerCached()
    {
        // Arrange
        await Task.Run(async () => { await _legacyDocument.GetGeneratedOutputAsync(DisposalToken); });

        // Act

        // Forces collection of the cached document output
        GC.Collect();

        // Assert
        Assert.False(_legacyDocument.TryGetGeneratedOutput(out _));
    }

    [Fact]
    public async Task RegeneratingWithReference_CachesOutput()
    {
        // Arrange
        var output = await _legacyDocument.GetGeneratedOutputAsync(DisposalToken);

        // Mostly doing this to ensure "var output" doesn't get optimized out
        Assert.NotNull(output);

        // Act & Assert
        Assert.True(_legacyDocument.TryGetGeneratedOutput(out _));
    }

    // This is a sanity test that we invoke component codegen for components.It's a little fragile but
    // necessary.

    [Fact]
    public async Task GetGeneratedOutputAsync_CshtmlComponent_ContainsComponentImports()
    {
        // Act
        var codeDocument = await _componentCshtmlDocument.GetGeneratedOutputAsync(DisposalToken);

        // Assert
        Assert.Contains("using global::Microsoft.AspNetCore.Components", codeDocument.GetCSharpSourceText().ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetGeneratedOutputAsync_Component()
    {
        // Act
        var codeDocument = await _componentDocument.GetGeneratedOutputAsync(DisposalToken);

        // Assert
        Assert.Contains("ComponentBase", codeDocument.GetCSharpSourceText().ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetGeneratedOutputAsync_NestedComponentDocument_SetsCorrectNamespaceAndClassName()
    {
        // Act
        var codeDocument = await _nestedComponentDocument.GetGeneratedOutputAsync(DisposalToken);

        // Assert
        Assert.Contains("ComponentBase", codeDocument.GetCSharpSourceText().ToString(), StringComparison.Ordinal);
        Assert.Contains("namespace SomeProject.Nested", codeDocument.GetCSharpSourceText().ToString(), StringComparison.Ordinal);
        Assert.Contains("class File3", codeDocument.GetCSharpSourceText().ToString(), StringComparison.Ordinal);
    }

    // This is a sanity test that we invoke legacy codegen for .cshtml files. It's a little fragile but
    // necessary.
    [Fact]
    public async Task GetGeneratedOutputAsync_Legacy()
    {
        // Act
        var codeDocument = await _legacyDocument.GetGeneratedOutputAsync(DisposalToken);

        // Assert
        Assert.Contains("Template", codeDocument.GetCSharpSourceText().ToString(), StringComparison.Ordinal);
    }
}