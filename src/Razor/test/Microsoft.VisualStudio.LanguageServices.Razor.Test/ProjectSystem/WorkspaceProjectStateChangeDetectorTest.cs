// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common.VisualStudio;
using Microsoft.AspNetCore.Razor.Test.Common.Workspaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.ProjectSystem;

public class WorkspaceProjectStateChangeDetectorTest : VisualStudioWorkspaceTestBase
{
    private readonly HostProject _hostProjectOne;
    private readonly HostProject _hostProjectTwo;
    private readonly HostProject _hostProjectThree;
    private readonly Solution _emptySolution;
    private readonly Solution _solutionWithOneProject;
    private readonly Solution _solutionWithTwoProjects;
    private readonly Solution _solutionWithDependentProject;
    private readonly Project _projectNumberOne;
    private readonly Project _projectNumberTwo;
    private readonly Project _projectNumberThree;

    private readonly DocumentId _cshtmlDocumentId;
    private readonly DocumentId _razorDocumentId;
    private readonly DocumentId _backgroundVirtualCSharpDocumentId;
    private readonly DocumentId _partialComponentClassDocumentId;

    public WorkspaceProjectStateChangeDetectorTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
        _emptySolution = Workspace.CurrentSolution;

        var projectId1 = ProjectId.CreateNewId("One");
        var projectId2 = ProjectId.CreateNewId("Two");
        var projectId3 = ProjectId.CreateNewId("Three");

        _cshtmlDocumentId = DocumentId.CreateNewId(projectId1);
        var cshtmlDocumentInfo = DocumentInfo.Create(_cshtmlDocumentId, "Test", filePath: "file.cshtml.g.cs");
        _razorDocumentId = DocumentId.CreateNewId(projectId1);
        var razorDocumentInfo = DocumentInfo.Create(_razorDocumentId, "Test", filePath: "file.razor.g.cs");
        _backgroundVirtualCSharpDocumentId = DocumentId.CreateNewId(projectId1);
        var backgroundDocumentInfo = DocumentInfo.Create(_backgroundVirtualCSharpDocumentId, "Test", filePath: "file.razor__bg__virtual.cs");
        _partialComponentClassDocumentId = DocumentId.CreateNewId(projectId1);
        var partialComponentClassDocumentInfo = DocumentInfo.Create(_partialComponentClassDocumentId, "Test", filePath: "file.razor.cs");

        _solutionWithTwoProjects = Workspace.CurrentSolution
            .AddProject(ProjectInfo.Create(
                projectId1,
                VersionStamp.Default,
                "One",
                "One",
                LanguageNames.CSharp,
                filePath: "One.csproj",
                documents: [cshtmlDocumentInfo, razorDocumentInfo, partialComponentClassDocumentInfo, backgroundDocumentInfo]).WithCompilationOutputInfo(new CompilationOutputInfo().WithAssemblyPath("obj1\\One.dll")))
            .AddProject(ProjectInfo.Create(
                projectId2,
                VersionStamp.Default,
                "Two",
                "Two",
                LanguageNames.CSharp,
                filePath: "Two.csproj").WithCompilationOutputInfo(new CompilationOutputInfo().WithAssemblyPath("obj2\\Two.dll")));

        _solutionWithOneProject = _emptySolution
            .AddProject(ProjectInfo.Create(
                projectId3,
                VersionStamp.Default,
                "Three",
                "Three",
                LanguageNames.CSharp,
                filePath: "Three.csproj").WithCompilationOutputInfo(new CompilationOutputInfo().WithAssemblyPath("obj3\\Three.dll")));

        var project2Reference = new ProjectReference(projectId2);
        var project3Reference = new ProjectReference(projectId3);
        _solutionWithDependentProject = Workspace.CurrentSolution
            .AddProject(ProjectInfo.Create(
                projectId1,
                VersionStamp.Default,
                "One",
                "One",
                LanguageNames.CSharp,
                filePath: "One.csproj",
                documents: [cshtmlDocumentInfo, razorDocumentInfo, partialComponentClassDocumentInfo, backgroundDocumentInfo],
                projectReferences: [project2Reference]).WithCompilationOutputInfo(new CompilationOutputInfo().WithAssemblyPath("obj1\\One.dll")))
            .AddProject(ProjectInfo.Create(
                projectId2,
                VersionStamp.Default,
                "Two",
                "Two",
                LanguageNames.CSharp,
                filePath: "Two.csproj",
                projectReferences: [project3Reference]).WithCompilationOutputInfo(new CompilationOutputInfo().WithAssemblyPath("obj2\\Two.dll")))
            .AddProject(ProjectInfo.Create(
                projectId3,
                VersionStamp.Default,
                "Three",
                "Three",
                LanguageNames.CSharp,
                filePath: "Three.csproj",
                documents: [razorDocumentInfo]).WithCompilationOutputInfo(new CompilationOutputInfo().WithAssemblyPath("obj3\\Three.dll")));

        _projectNumberOne = _solutionWithTwoProjects.GetRequiredProject(projectId1);
        _projectNumberTwo = _solutionWithTwoProjects.GetRequiredProject(projectId2);
        _projectNumberThree = _solutionWithOneProject.GetRequiredProject(projectId3);

        _hostProjectOne = new HostProject("One.csproj", "obj1", FallbackRazorConfiguration.MVC_1_1, "One");
        _hostProjectTwo = new HostProject("Two.csproj", "obj2", FallbackRazorConfiguration.MVC_1_1, "Two");
        _hostProjectThree = new HostProject("Three.csproj", "obj3", FallbackRazorConfiguration.MVC_1_1, "Three");
    }

    private static ProjectInfo CreateProjectInfo(HostProject hostProject)
    {
        var filePath = hostProject.FilePath;
        var name = Path.GetFileNameWithoutExtension(filePath);
        var outputFilePath = Path.Combine(Path.GetDirectoryName(filePath), "obj", name + ".dll");

        var result = ProjectInfo.Create(
            id: ProjectId.CreateNewId(),
            version: VersionStamp.Create(),
            name,
            assemblyName: name,
            language: LanguageNames.CSharp,
            filePath,
            outputFilePath);

        return result
            .WithCompilationOutputInfo(result.CompilationOutputInfo
                .WithAssemblyPath(outputFilePath));
    }

    private sealed class BlockingGenerator : IProjectWorkspaceStateGenerator, IDisposable
    {
        private readonly ManualResetEventSlim _blockEnqueueEvent = new(initialState: true);
        private ImmutableArray<(Project?, IProjectSnapshot)> _updates = [];

        private bool _shouldBlock;
        private bool _isWaitingToEnqueueUpdate;
        private bool _ignoreEnqueuedUpdate;

        public ImmutableArray<(Project? WorkspaceProject, IProjectSnapshot Project)> Updates => _updates;

        public void Dispose()
        {
            _blockEnqueueEvent.Dispose();
        }

        public void ClearUpdates()
        {
            ImmutableInterlocked.Update(ref _updates, array => []);
        }

        void IProjectWorkspaceStateGenerator.CancelUpdates()
            => throw new InvalidOperationException();

        void IProjectWorkspaceStateGenerator.EnqueueUpdate(Project? workspaceProject, IProjectSnapshot projectSnapshot)
        {
            if (_shouldBlock)
            {
                _isWaitingToEnqueueUpdate = true;
                _blockEnqueueEvent.Wait();
                _isWaitingToEnqueueUpdate = false;
            }

            if (!_ignoreEnqueuedUpdate)
            {
                ImmutableInterlocked.Update(ref _updates, array => array.Add((workspaceProject, projectSnapshot)));
            }

            _ignoreEnqueuedUpdate = false;
        }

        public Task WaitForEnqueueUpdateToBlockAsync()
        {
            if (_isWaitingToEnqueueUpdate)
            {
                return Task.CompletedTask;
            }

            return Task.Run(() => SpinWait.SpinUntil(() => _isWaitingToEnqueueUpdate));
        }

        public void StartBlocking()
        {
            if (_isWaitingToEnqueueUpdate)
            {
                return;
            }

            _shouldBlock = true;
            _blockEnqueueEvent.Reset();
        }

        public void StopBlocking(bool ignoreEnqueuedUpdate)
        {
            if (!_isWaitingToEnqueueUpdate)
            {
                return;
            }

            _shouldBlock = false;
            _ignoreEnqueuedUpdate = ignoreEnqueuedUpdate;
            _blockEnqueueEvent.Set();
        }
    }

    [UIFact]
    public async Task SolutionClosing_StopsActiveWork()
    {
        using var generator = new BlockingGenerator();
        var projectManager = CreateProjectSnapshotManager();

        var detector = CreateDetector(generator, projectManager);
        var detectorAccessor = detector.GetTestAccessor();

        generator.StartBlocking();

        // Add projects to workspace
        var solution = Workspace.CurrentSolution;
        Assert.Empty(solution.ProjectIds);

        var hostProject1 = TestHostProject.Create(@"C:\Projects\One\One.csproj");
        var hostProject2 = TestHostProject.Create(@"C:\Projects\Two\Two.csproj");

        solution = solution
            .AddProject(CreateProjectInfo(hostProject1))
            .AddProject(CreateProjectInfo(hostProject2));

        // Update the workspace and wait for the workspace change events to fire.
        var listenerTask = detectorAccessor.ListenForWorkspaceChangesAsync(
            WorkspaceChangeKind.ProjectAdded,
            WorkspaceChangeKind.ProjectAdded);
        Assert.True(Workspace.TryApplyChanges(solution));
        await listenerTask;

        // Add projects to ProjectSnapshotManager
        await projectManager.UpdateAsync(updater =>
        {
            updater.AddProject(hostProject1);
            updater.AddProject(hostProject2);
        });

        // Wait until the detector processes a batch and tries to
        // enqueue an update on the generator.
        await generator.WaitForEnqueueUpdateToBlockAsync();

        // Now that batch processing is paused, we can update the
        // ProjectSnapshotManager to trigger all of the work to be cancelled.
        await projectManager.UpdateAsync(updater =>
        {
            updater.SolutionClosed();

            // Trigger a project removed event while solution is closing.
            updater.RemoveProject(hostProject1.Key);
        });

        // Finally, we can stop blocking and ignore the update that was being enqueued.
        // For good measure, we wait until the work queue finishes its current batch.
        generator.StopBlocking(ignoreEnqueuedUpdate: true);
        await detectorAccessor.WaitUntilCurrentBatchCompletesAsync();

        // No updates should have been made! We blocked on the first update and
        // caused the remaining work to be cancelled.
        Assert.Empty(generator.Updates);
    }

    [UIFact]
    public async Task WorkspaceChanged_DocumentAdded_EnqueuesUpdatesForDependentProjects()
    {
        using var generator = new BlockingGenerator();
        var projectManager = CreateProjectSnapshotManager();

        var detector = CreateDetector(generator, projectManager);
        var detectorAccessor = detector.GetTestAccessor();

        generator.StartBlocking();

        // Add projects to workspace
        var solution = Workspace.CurrentSolution;
        Assert.Empty(solution.ProjectIds);

        var hostProject1 = TestHostProject.Create(@"C:\Projects\One\One.csproj");
        var hostProject2 = TestHostProject.Create(@"C:\Projects\Two\Two.csproj");
        var hostProject3 = TestHostProject.Create(@"C:\Projects\Three\Three.csproj");

        var projectInfo1 = CreateProjectInfo(hostProject1);
        var projectInfo2 = CreateProjectInfo(hostProject2);
        var projectInfo3 = CreateProjectInfo(hostProject3);

        projectInfo1 = projectInfo1.WithProjectReferences([(new(projectInfo2.Id))]);
        projectInfo2 = projectInfo2.WithProjectReferences([(new(projectInfo3.Id))]);

        solution = solution
            .AddProject(projectInfo1)
            .AddProject(projectInfo2)
            .AddProject(projectInfo3);

        // Update the workspace and wait for the workspace change events to fire.
        var listenerTask = detectorAccessor.ListenForWorkspaceChangesAsync(
            WorkspaceChangeKind.ProjectAdded,
            WorkspaceChangeKind.ProjectAdded);
        Assert.True(Workspace.TryApplyChanges(solution));
        await listenerTask;

        // Add projects to ProjectSnapshotManager
        await projectManager.UpdateAsync(updater =>
        {
            updater.AddProject(hostProject1);
            updater.AddProject(hostProject2);
            updater.AddProject(hostProject3);
        });

        await generator.WaitForEnqueueUpdateToBlockAsync();

        generator.StopBlocking(ignoreEnqueuedUpdate: false);

        await detectorAccessor.DrainBatchesAsync();

        ImmutableArray<ProjectKey> expected = [hostProject1.Key, hostProject2.Key, hostProject3.Key];
        expected = expected.OrderAsArray();

        var actual = generator.Updates.SelectAsArray(x => x.Project.Key).OrderAsArray();

        Assert.Equal<ProjectKey>(expected, actual);

        generator.ClearUpdates();
        generator.StartBlocking();

        var razorDocumentId = DocumentId.CreateNewId(projectInfo3.Id);
        var razorDocumentInfo = DocumentInfo.Create(razorDocumentId, "Test", filePath: "file.razor.g.cs");

        solution = Workspace.CurrentSolution.AddDocument(razorDocumentInfo);

        var addDocumentListenerTask = detectorAccessor.ListenForWorkspaceChangesAsync(WorkspaceChangeKind.DocumentAdded);
        Assert.True(Workspace.TryApplyChanges(solution));
        await addDocumentListenerTask;

        await generator.WaitForEnqueueUpdateToBlockAsync();

        generator.StopBlocking(ignoreEnqueuedUpdate: false);

        await detectorAccessor.DrainBatchesAsync();

        actual = generator.Updates.SelectAsArray(x => x.Project.Key).OrderAsArray();
        Assert.Equal<ProjectKey>(expected, actual);
    }

    [UITheory]
    [InlineData(WorkspaceChangeKind.DocumentAdded)]
    [InlineData(WorkspaceChangeKind.DocumentChanged)]
    [InlineData(WorkspaceChangeKind.DocumentRemoved)]
    public async Task WorkspaceChanged_DocumentEvents_EnqueuesUpdatesForDependentProjects(WorkspaceChangeKind kind)
    {
        // Arrange
        var generator = new TestProjectWorkspaceStateGenerator();
        var projectManager = CreateProjectSnapshotManager();
        var detector = CreateDetector(generator, projectManager);
        var detectorAccessor = detector.GetTestAccessor();

        await projectManager.UpdateAsync(updater =>
        {
            updater.AddProject(_hostProjectOne);
            updater.AddProject(_hostProjectTwo);
            updater.AddProject(_hostProjectThree);
        });

        // Initialize with a project. This will get removed.
        var e = new WorkspaceChangeEventArgs(WorkspaceChangeKind.SolutionAdded, oldSolution: _emptySolution, newSolution: _solutionWithOneProject);
        detectorAccessor.WorkspaceChanged(e);

        e = new WorkspaceChangeEventArgs(kind, oldSolution: _solutionWithOneProject, newSolution: _solutionWithDependentProject);

        var solution = _solutionWithDependentProject.WithProjectAssemblyName(_projectNumberThree.Id, "Changed");

        e = new WorkspaceChangeEventArgs(kind, oldSolution: _solutionWithDependentProject, newSolution: solution, projectId: _projectNumberThree.Id, documentId: _razorDocumentId);

        // Act
        detectorAccessor.WorkspaceChanged(e);

        await detectorAccessor.WaitUntilCurrentBatchCompletesAsync();

        // Assert
        Assert.Equal(3, generator.Updates.Count);
        Assert.Contains(generator.Updates, u => u.ProjectSnapshot.Key == _projectNumberOne.ToProjectKey());
        Assert.Contains(generator.Updates, u => u.ProjectSnapshot.Key == _projectNumberTwo.ToProjectKey());
        Assert.Contains(generator.Updates, u => u.ProjectSnapshot.Key == _projectNumberThree.ToProjectKey());
    }

    [UITheory]
    [InlineData(WorkspaceChangeKind.ProjectChanged)]
    [InlineData(WorkspaceChangeKind.ProjectAdded)]
    [InlineData(WorkspaceChangeKind.ProjectRemoved)]
    public async Task WorkspaceChanged_ProjectEvents_EnqueuesUpdatesForDependentProjects(WorkspaceChangeKind kind)
    {
        // Arrange
        var generator = new TestProjectWorkspaceStateGenerator();
        var projectManager = CreateProjectSnapshotManager();
        var detector = CreateDetector(generator, projectManager);
        var detectorAccessor = detector.GetTestAccessor();

        await projectManager.UpdateAsync(updater =>
        {
            updater.AddProject(_hostProjectOne);
            updater.AddProject(_hostProjectTwo);
            updater.AddProject(_hostProjectThree);
        });

        // Initialize with a project. This will get removed.
        var e = new WorkspaceChangeEventArgs(WorkspaceChangeKind.SolutionAdded, oldSolution: _emptySolution, newSolution: _solutionWithOneProject);
        detectorAccessor.WorkspaceChanged(e);

        e = new WorkspaceChangeEventArgs(kind, oldSolution: _solutionWithOneProject, newSolution: _solutionWithDependentProject);

        var solution = _solutionWithDependentProject.WithProjectAssemblyName(_projectNumberThree.Id, "Changed");

        e = new WorkspaceChangeEventArgs(kind, oldSolution: _solutionWithDependentProject, newSolution: solution, projectId: _projectNumberThree.Id);

        // Act
        detectorAccessor.WorkspaceChanged(e);

        await detectorAccessor.WaitUntilCurrentBatchCompletesAsync();

        // Assert
        Assert.Equal(3, generator.Updates.Count);
        Assert.Contains(generator.Updates, u => u.ProjectSnapshot.Key == _projectNumberOne.ToProjectKey());
        Assert.Contains(generator.Updates, u => u.ProjectSnapshot.Key == _projectNumberTwo.ToProjectKey());
        Assert.Contains(generator.Updates, u => u.ProjectSnapshot.Key == _projectNumberThree.ToProjectKey());
    }

    [UITheory]
    [InlineData(WorkspaceChangeKind.SolutionAdded)]
    [InlineData(WorkspaceChangeKind.SolutionChanged)]
    [InlineData(WorkspaceChangeKind.SolutionCleared)]
    [InlineData(WorkspaceChangeKind.SolutionReloaded)]
    [InlineData(WorkspaceChangeKind.SolutionRemoved)]
    public async Task WorkspaceChanged_SolutionEvents_EnqueuesUpdatesForProjectsInSolution(WorkspaceChangeKind kind)
    {
        // Arrange
        var generator = new TestProjectWorkspaceStateGenerator();
        var projectManager = CreateProjectSnapshotManager();
        var detector = CreateDetector(generator, projectManager);
        var detectorAccessor = detector.GetTestAccessor();

        await projectManager.UpdateAsync(updater =>
        {
            updater.AddProject(_hostProjectOne);
            updater.AddProject(_hostProjectTwo);
        });

        var e = new WorkspaceChangeEventArgs(kind, oldSolution: _emptySolution, newSolution: _solutionWithTwoProjects);

        // Act
        Assert.True(Workspace.TryApplyChanges(_solutionWithTwoProjects));

        await detectorAccessor.WaitUntilCurrentBatchCompletesAsync();

        // Assert
        Assert.Collection(
            generator.Updates,
            p => Assert.Equal(_projectNumberOne.Id, p.WorkspaceProject?.Id),
            p => Assert.Equal(_projectNumberTwo.Id, p.WorkspaceProject?.Id));
    }

    [UITheory]
    [InlineData(WorkspaceChangeKind.SolutionAdded)]
    [InlineData(WorkspaceChangeKind.SolutionChanged)]
    [InlineData(WorkspaceChangeKind.SolutionCleared)]
    [InlineData(WorkspaceChangeKind.SolutionReloaded)]
    [InlineData(WorkspaceChangeKind.SolutionRemoved)]
    public async Task WorkspaceChanged_SolutionEvents_EnqueuesStateClear_EnqueuesSolutionProjectUpdates(WorkspaceChangeKind kind)
    {
        // Arrange
        var generator = new TestProjectWorkspaceStateGenerator();
        var projectManager = CreateProjectSnapshotManager();
        var detector = CreateDetector(generator, projectManager);
        var detectorAccessor = detector.GetTestAccessor();

        await projectManager.UpdateAsync(updater =>
        {
            updater.AddProject(_hostProjectOne);
            updater.AddProject(_hostProjectTwo);
            updater.AddProject(_hostProjectThree);
        });

        // Initialize with a project. This will get removed.
        var e = new WorkspaceChangeEventArgs(WorkspaceChangeKind.SolutionAdded, oldSolution: _emptySolution, newSolution: _solutionWithOneProject);
        detectorAccessor.WorkspaceChanged(e);

        await detectorAccessor.WaitUntilCurrentBatchCompletesAsync();

        e = new WorkspaceChangeEventArgs(kind, oldSolution: _solutionWithOneProject, newSolution: _solutionWithTwoProjects);

        // Act
        detectorAccessor.WorkspaceChanged(e);

        await detectorAccessor.WaitUntilCurrentBatchCompletesAsync();

        // Assert
        Assert.Collection(
            generator.Updates,
            p => Assert.Equal(_projectNumberThree.Id, p.WorkspaceProject?.Id),
            p => Assert.Null(p.WorkspaceProject),
            p => Assert.Equal(_projectNumberOne.Id, p.WorkspaceProject?.Id),
            p => Assert.Equal(_projectNumberTwo.Id, p.WorkspaceProject?.Id));
    }

    [UITheory]
    [InlineData(WorkspaceChangeKind.ProjectChanged)]
    [InlineData(WorkspaceChangeKind.ProjectReloaded)]
    public async Task WorkspaceChanged_ProjectChangeEvents_UpdatesProjectState_AfterDelay(WorkspaceChangeKind kind)
    {
        // Arrange
        var generator = new TestProjectWorkspaceStateGenerator();
        var projectManager = CreateProjectSnapshotManager();
        var detector = CreateDetector(generator, projectManager);
        var detectorAccessor = detector.GetTestAccessor();

        await projectManager.UpdateAsync(updater =>
        {
            updater.AddProject(_hostProjectOne);
        });

        // Stop any existing work and clear out any updates that we might have received.
        detectorAccessor.CancelExistingWork();
        generator.Clear();

        // Create a listener for the workspace change we're about to send.
        var listenerTask = detectorAccessor.ListenForWorkspaceChangesAsync(kind);

        var solution = _solutionWithTwoProjects.WithProjectAssemblyName(_projectNumberOne.Id, "Changed");
        var e = new WorkspaceChangeEventArgs(kind, oldSolution: _solutionWithTwoProjects, newSolution: solution, projectId: _projectNumberOne.Id);

        // Act
        detectorAccessor.WorkspaceChanged(e);

        await listenerTask;
        await detectorAccessor.WaitUntilCurrentBatchCompletesAsync();

        // Assert
        var update = Assert.Single(generator.Updates);
        Assert.Equal(_projectNumberOne.Id, update.WorkspaceProject?.Id);
        Assert.Equal(_hostProjectOne.FilePath, update.ProjectSnapshot.FilePath);
    }

    [UIFact]
    public async Task WorkspaceChanged_DocumentChanged_BackgroundVirtualCS_UpdatesProjectState_AfterDelay()
    {
        // Arrange
        var generator = new TestProjectWorkspaceStateGenerator();
        var projectManager = CreateProjectSnapshotManager();
        var detector = CreateDetector(generator, projectManager);
        var detectorAccessor = detector.GetTestAccessor();

        Workspace.TryApplyChanges(_solutionWithTwoProjects);

        await projectManager.UpdateAsync(updater =>
        {
            updater.AddProject(_hostProjectOne);
        });

        generator.Clear();

        var solution = _solutionWithTwoProjects.WithDocumentText(_backgroundVirtualCSharpDocumentId, SourceText.From("public class Foo{}"));
        var e = new WorkspaceChangeEventArgs(WorkspaceChangeKind.DocumentChanged, oldSolution: _solutionWithTwoProjects, newSolution: solution, projectId: _projectNumberOne.Id, _backgroundVirtualCSharpDocumentId);

        // Act
        detectorAccessor.WorkspaceChanged(e);

        await detectorAccessor.WaitUntilCurrentBatchCompletesAsync();

        // Assert
        var update = Assert.Single(generator.Updates);
        Assert.Equal(_projectNumberOne.Id, update.WorkspaceProject?.Id);
        Assert.Equal(_hostProjectOne.FilePath, update.ProjectSnapshot.FilePath);
    }

    [UIFact]
    public async Task WorkspaceChanged_DocumentChanged_CSHTML_UpdatesProjectState_AfterDelay()
    {
        // Arrange
        var generator = new TestProjectWorkspaceStateGenerator();
        var projectManager = CreateProjectSnapshotManager();
        var detector = CreateDetector(generator, projectManager);
        var detectorAccessor = detector.GetTestAccessor();

        Workspace.TryApplyChanges(_solutionWithTwoProjects);

        await projectManager.UpdateAsync(updater =>
        {
            updater.AddProject(_hostProjectOne);
        });

        generator.Clear();

        var solution = _solutionWithTwoProjects.WithDocumentText(_cshtmlDocumentId, SourceText.From("Hello World"));
        var e = new WorkspaceChangeEventArgs(WorkspaceChangeKind.DocumentChanged, oldSolution: _solutionWithTwoProjects, newSolution: solution, projectId: _projectNumberOne.Id, _cshtmlDocumentId);

        // Act
        detectorAccessor.WorkspaceChanged(e);

        await detectorAccessor.WaitUntilCurrentBatchCompletesAsync();

        // Assert
        var update = Assert.Single(generator.Updates);
        Assert.Equal(_projectNumberOne.Id, update.WorkspaceProject?.Id);
        Assert.Equal(_hostProjectOne.FilePath, update.ProjectSnapshot.FilePath);
    }

    [UIFact]
    public async Task WorkspaceChanged_DocumentChanged_Razor_UpdatesProjectState_AfterDelay()
    {
        // Arrange
        var generator = new TestProjectWorkspaceStateGenerator();
        var projectManager = CreateProjectSnapshotManager();
        var detector = CreateDetector(generator, projectManager);
        var detectorAccessor = detector.GetTestAccessor();

        Workspace.TryApplyChanges(_solutionWithTwoProjects);

        await projectManager.UpdateAsync(updater =>
        {
            updater.AddProject(_hostProjectOne);
        });

        generator.Clear();

        var solution = _solutionWithTwoProjects.WithDocumentText(_razorDocumentId, SourceText.From("Hello World"));
        var e = new WorkspaceChangeEventArgs(WorkspaceChangeKind.DocumentChanged, oldSolution: _solutionWithTwoProjects, newSolution: solution, projectId: _projectNumberOne.Id, _razorDocumentId);

        // Act
        detectorAccessor.WorkspaceChanged(e);

        await detectorAccessor.WaitUntilCurrentBatchCompletesAsync();

        // Assert
        var update = Assert.Single(generator.Updates);
        Assert.Equal(_projectNumberOne.Id, update.WorkspaceProject?.Id);
        Assert.Equal(_hostProjectOne.FilePath, update.ProjectSnapshot.FilePath);
    }

    [UIFact]
    public async Task WorkspaceChanged_DocumentChanged_PartialComponent_UpdatesProjectState_AfterDelay()
    {
        // Arrange
        var generator = new TestProjectWorkspaceStateGenerator();
        var projectManager = CreateProjectSnapshotManager();
        var detector = CreateDetector(generator, projectManager);
        var detectorAccessor = detector.GetTestAccessor();

        Workspace.TryApplyChanges(_solutionWithTwoProjects);

        await projectManager.UpdateAsync(updater =>
        {
            updater.AddProject(_hostProjectOne);
        });

        await detectorAccessor.WaitUntilCurrentBatchCompletesAsync();
        generator.Clear();

        var sourceText = SourceText.From($$"""
            public partial class TestComponent : {{ComponentsApi.IComponent.MetadataName}} {}
            namespace Microsoft.AspNetCore.Components
            {
                public interface IComponent {}
            }
            """);
        var syntaxTreeRoot = await CSharpSyntaxTree.ParseText(sourceText).GetRootAsync();
        var solution = _solutionWithTwoProjects
            .WithDocumentText(_partialComponentClassDocumentId, sourceText)
            .WithDocumentSyntaxRoot(_partialComponentClassDocumentId, syntaxTreeRoot, PreservationMode.PreserveIdentity);
        var document = solution.GetRequiredDocument(_partialComponentClassDocumentId);

        // The change detector only operates when a semantic model / syntax tree is available.
        await document.GetSyntaxRootAsync();
        await document.GetSemanticModelAsync();

        var e = new WorkspaceChangeEventArgs(WorkspaceChangeKind.DocumentChanged, oldSolution: solution, newSolution: solution, projectId: _projectNumberOne.Id, _partialComponentClassDocumentId);

        // Act
        detectorAccessor.WorkspaceChanged(e);

        await detectorAccessor.WaitUntilCurrentBatchCompletesAsync();

        // Assert
        var update = Assert.Single(generator.Updates);
        Assert.Equal(_projectNumberOne.Id, update.WorkspaceProject?.Id);
        Assert.Equal(_hostProjectOne.FilePath, update.ProjectSnapshot.FilePath);
    }

    [UIFact]
    public async Task WorkspaceChanged_ProjectRemovedEvent_QueuesProjectStateRemoval()
    {
        // Arrange
        var generator = new TestProjectWorkspaceStateGenerator();
        var projectManager = CreateProjectSnapshotManager();
        var detector = CreateDetector(generator, projectManager);
        var detectorAccessor = detector.GetTestAccessor();

        await projectManager.UpdateAsync(updater =>
        {
            updater.AddProject(_hostProjectOne);
            updater.AddProject(_hostProjectTwo);
        });

        var solution = _solutionWithTwoProjects.RemoveProject(_projectNumberOne.Id);
        var e = new WorkspaceChangeEventArgs(WorkspaceChangeKind.ProjectRemoved, oldSolution: _solutionWithTwoProjects, newSolution: solution, projectId: _projectNumberOne.Id);

        // Act
        detectorAccessor.WorkspaceChanged(e);
        await detectorAccessor.WaitUntilCurrentBatchCompletesAsync();

        // Assert
        Assert.Single(
            generator.Updates,
            p => p.WorkspaceProject is null);
    }

    [UIFact]
    public async Task WorkspaceChanged_ProjectAddedEvent_AddsProject()
    {
        // Arrange
        var generator = new TestProjectWorkspaceStateGenerator();
        var projectManager = CreateProjectSnapshotManager();
        var detector = CreateDetector(generator, projectManager);
        var detectorAccessor = detector.GetTestAccessor();

        await projectManager.UpdateAsync(updater =>
        {
            updater.AddProject(_hostProjectThree);
        });

        var solution = _solutionWithOneProject;

        // Act
        var listenerTask = detectorAccessor.ListenForWorkspaceChangesAsync(WorkspaceChangeKind.ProjectAdded);
        Assert.True(Workspace.TryApplyChanges(solution));
        await listenerTask;
        await detectorAccessor.WaitUntilCurrentBatchCompletesAsync();

        // Assert
        Assert.Single(
            generator.Updates,
            p => p.WorkspaceProject?.Id == _projectNumberThree.Id);
    }

    [Fact]
    public async Task IsPartialComponentClass_NoIComponent_ReturnsFalse()
    {
        // Arrange
        var sourceText = SourceText.From("""
            public partial class TestComponent{}
            """);
        var syntaxTreeRoot = await CSharpSyntaxTree.ParseText(sourceText).GetRootAsync();
        var solution = _solutionWithTwoProjects
            .WithDocumentText(_partialComponentClassDocumentId, sourceText)
            .WithDocumentSyntaxRoot(_partialComponentClassDocumentId, syntaxTreeRoot, PreservationMode.PreserveIdentity);
        var document = solution.GetRequiredDocument(_partialComponentClassDocumentId);

        // Initialize document
        await document.GetSyntaxRootAsync();
        await document.GetSemanticModelAsync();

        // Act
        var result = WorkspaceProjectStateChangeDetector.IsPartialComponentClass(document);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsPartialComponentClass_InitializedDocument_ReturnsTrue()
    {
        // Arrange
        var sourceText = SourceText.From($$"""
            public partial class TestComponent : {{ComponentsApi.IComponent.MetadataName}} {}
            namespace Microsoft.AspNetCore.Components
            {
                public interface IComponent {}
            }
            """);
        var syntaxTreeRoot = await CSharpSyntaxTree.ParseText(sourceText).GetRootAsync();
        var solution = _solutionWithTwoProjects
            .WithDocumentText(_partialComponentClassDocumentId, sourceText)
            .WithDocumentSyntaxRoot(_partialComponentClassDocumentId, syntaxTreeRoot, PreservationMode.PreserveIdentity);
        var document = solution.GetRequiredDocument(_partialComponentClassDocumentId);

        // Initialize document
        await document.GetSyntaxRootAsync();
        await document.GetSemanticModelAsync();

        // Act
        var result = WorkspaceProjectStateChangeDetector.IsPartialComponentClass(document);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsPartialComponentClass_Uninitialized_ReturnsFalse()
    {
        // Arrange
        var sourceText = SourceText.From($$"""
            public partial class TestComponent : {{ComponentsApi.IComponent.MetadataName}} {}
            namespace Microsoft.AspNetCore.Components
            {
                public interface IComponent {}
            }
            """);
        var syntaxTreeRoot = CSharpSyntaxTree.ParseText(sourceText).GetRoot();
        var solution = _solutionWithTwoProjects
            .WithDocumentText(_partialComponentClassDocumentId, sourceText)
            .WithDocumentSyntaxRoot(_partialComponentClassDocumentId, syntaxTreeRoot, PreservationMode.PreserveIdentity);
        var document = solution.GetRequiredDocument(_partialComponentClassDocumentId);

        // Act
        var result = WorkspaceProjectStateChangeDetector.IsPartialComponentClass(document);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsPartialComponentClass_UninitializedSemanticModel_ReturnsFalse()
    {
        // Arrange
        var sourceText = SourceText.From($$"""
            public partial class TestComponent : {{ComponentsApi.IComponent.MetadataName}} {}
            namespace Microsoft.AspNetCore.Components
            {
                public interface IComponent {}
            }
            """);
        var syntaxTreeRoot = await CSharpSyntaxTree.ParseText(sourceText).GetRootAsync();
        var solution = _solutionWithTwoProjects
            .WithDocumentText(_partialComponentClassDocumentId, sourceText)
            .WithDocumentSyntaxRoot(_partialComponentClassDocumentId, syntaxTreeRoot, PreservationMode.PreserveIdentity);
        var document = solution.GetRequiredDocument(_partialComponentClassDocumentId);

        await document.GetSyntaxRootAsync();

        // Act
        var result = WorkspaceProjectStateChangeDetector.IsPartialComponentClass(document);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsPartialComponentClass_NonClass_ReturnsFalse()
    {
        // Arrange
        var sourceText = SourceText.From(string.Empty);
        var syntaxTreeRoot = await CSharpSyntaxTree.ParseText(sourceText).GetRootAsync();
        var solution = _solutionWithTwoProjects
            .WithDocumentText(_partialComponentClassDocumentId, sourceText)
            .WithDocumentSyntaxRoot(_partialComponentClassDocumentId, syntaxTreeRoot, PreservationMode.PreserveIdentity);
        var document = solution.GetRequiredDocument(_partialComponentClassDocumentId);

        // Initialize document
        await document.GetSyntaxRootAsync();
        await document.GetSemanticModelAsync();

        // Act
        var result = WorkspaceProjectStateChangeDetector.IsPartialComponentClass(document);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsPartialComponentClass_MultipleClassesOneComponentPartial_ReturnsTrue()
    {
        // Arrange
        var sourceText = SourceText.From($$"""
            public partial class NonComponent1 {}
            public class NonComponent2 {}
            public partial class TestComponent : {{ComponentsApi.IComponent.MetadataName}} {}
            public partial class NonComponent3 {}
            public class NonComponent4 {}
            namespace Microsoft.AspNetCore.Components
            {
                public interface IComponent {}
            }
            """);
        var syntaxTreeRoot = await CSharpSyntaxTree.ParseText(sourceText).GetRootAsync();
        var solution = _solutionWithTwoProjects
            .WithDocumentText(_partialComponentClassDocumentId, sourceText)
            .WithDocumentSyntaxRoot(_partialComponentClassDocumentId, syntaxTreeRoot, PreservationMode.PreserveIdentity);
        var document = solution.GetRequiredDocument(_partialComponentClassDocumentId);

        // Initialize document
        await document.GetSyntaxRootAsync();
        await document.GetSemanticModelAsync();

        // Act
        var result = WorkspaceProjectStateChangeDetector.IsPartialComponentClass(document);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsPartialComponentClass_NonComponents_ReturnsFalse()
    {
        // Arrange
        var sourceText = SourceText.From("""
            public partial class NonComponent1 {}
            public class NonComponent2 {}
            public partial class NonComponent3 {}
            public class NonComponent4 {}
            namespace Microsoft.AspNetCore.Components
            {
                public interface IComponent {}
            }
            """);
        var syntaxTreeRoot = await CSharpSyntaxTree.ParseText(sourceText).GetRootAsync();
        var solution = _solutionWithTwoProjects
            .WithDocumentText(_partialComponentClassDocumentId, sourceText)
            .WithDocumentSyntaxRoot(_partialComponentClassDocumentId, syntaxTreeRoot, PreservationMode.PreserveIdentity);
        var document = solution.GetRequiredDocument(_partialComponentClassDocumentId);

        // Initialize document
        await document.GetSyntaxRootAsync();
        await document.GetSemanticModelAsync();

        // Act
        var result = WorkspaceProjectStateChangeDetector.IsPartialComponentClass(document);

        // Assert
        Assert.False(result);
    }

    private WorkspaceProjectStateChangeDetector CreateDetector(
        IProjectWorkspaceStateGenerator generator,
        IProjectSnapshotManager projectManager)
    {
        var detector = new WorkspaceProjectStateChangeDetector(
            generator,
            projectManager,
            TestLanguageServerFeatureOptions.Instance,
            WorkspaceProvider,
            TimeSpan.FromMilliseconds(10));

        AddDisposable(detector);

        return detector;
    }
}
