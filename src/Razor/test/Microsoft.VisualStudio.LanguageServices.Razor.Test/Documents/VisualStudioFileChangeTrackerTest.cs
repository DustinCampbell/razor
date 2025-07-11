﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.Documents;

public class VisualStudioFileChangeTrackerTest(ITestOutputHelper testOutput) : VisualStudioTestBase(testOutput)
{
    [UIFact]
    public async Task StartListening_AdvisesForFileChange()
    {
        // Arrange
        var fileChangeService = new StrictMock<IVsAsyncFileChangeEx>();
        fileChangeService
            .Setup(f => f.AdviseFileChangeAsync(It.IsAny<string>(), It.IsAny<_VSFILECHANGEFLAGS>(), It.IsAny<IVsFreeThreadedFileChangeEvents2>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(123u)
            .Verifiable();
        var tracker = new VisualStudioFileChangeTracker(
            TestProjectData.SomeProjectImportFile.FilePath,
            LoggerFactory,
            fileChangeService.Object,
            JoinableTaskFactory.Context);

        // Act
        tracker.StartListening();

        await tracker._fileChangeAdviseTask!;

        // Assert
        fileChangeService.Verify();
    }

    [UIFact]
    public async Task StartListening_AlreadyListening_DoesNothing()
    {
        // Arrange
        var callCount = 0;
        var fileChangeService = new StrictMock<IVsAsyncFileChangeEx>();
        fileChangeService
            .Setup(f => f.AdviseFileChangeAsync(It.IsAny<string>(), It.IsAny<_VSFILECHANGEFLAGS>(), It.IsAny<IVsFreeThreadedFileChangeEvents2>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(123u)
            .Callback(() => callCount++);
        var tracker = new VisualStudioFileChangeTracker(
            TestProjectData.SomeProjectImportFile.FilePath,
            LoggerFactory,
            fileChangeService.Object,
            JoinableTaskFactory.Context);

        tracker.StartListening();

        // Act
        tracker.StartListening();

        await tracker._fileChangeAdviseTask!;

        // Assert
        Assert.Equal(1, callCount);
    }

    [UIFact]
    public async Task StopListening_UnadvisesForFileChange()
    {
        // Arrange
        var fileChangeService = new StrictMock<IVsAsyncFileChangeEx>();
        fileChangeService
            .Setup(f => f.AdviseFileChangeAsync(It.IsAny<string>(), It.IsAny<_VSFILECHANGEFLAGS>(), It.IsAny<IVsFreeThreadedFileChangeEvents2>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(123u)
            .Verifiable();
        fileChangeService
            .Setup(f => f.UnadviseFileChangeAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestProjectData.SomeProjectImportFile.FilePath)
            .Verifiable();
        var tracker = new VisualStudioFileChangeTracker(
            TestProjectData.SomeProjectImportFile.FilePath,
            LoggerFactory,
            fileChangeService.Object,
            JoinableTaskFactory.Context);

        tracker.StartListening();

        await tracker._fileChangeAdviseTask!;

        // Act
        tracker.StopListening();

        await tracker._fileChangeUnadviseTask!;

        // Assert
        fileChangeService.Verify();
    }

    [UIFact]
    public void StopListening_NotListening_DoesNothing()
    {
        // Arrange
        var fileChangeService = new StrictMock<IVsAsyncFileChangeEx>();
        fileChangeService
            .Setup(f => f.UnadviseFileChangeAsync(123, It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException());
        var tracker = new VisualStudioFileChangeTracker(
            TestProjectData.SomeProjectImportFile.FilePath,
            LoggerFactory,
            fileChangeService.Object,
            JoinableTaskFactory.Context);

        // Act
        tracker.StopListening();

        // Assert
        Assert.Null(tracker._fileChangeUnadviseTask);
    }

    [UITheory]
    [InlineData((uint)_VSFILECHANGEFLAGS.VSFILECHG_Size, (int)FileChangeKind.Changed)]
    [InlineData((uint)_VSFILECHANGEFLAGS.VSFILECHG_Time, (int)FileChangeKind.Changed)]
    [InlineData((uint)_VSFILECHANGEFLAGS.VSFILECHG_Add, (int)FileChangeKind.Added)]
    [InlineData((uint)_VSFILECHANGEFLAGS.VSFILECHG_Del, (int)FileChangeKind.Removed)]
    public async Task FilesChanged_WithSpecificFlags_InvokesChangedHandler_WithExpectedArguments(uint fileChangeFlag, int expectedKind)
    {
        // Arrange
        var filePath = TestProjectData.SomeProjectImportFile.FilePath;
        var fileChangeService = Mock.Of<IVsAsyncFileChangeEx>(MockBehavior.Strict);
        var tracker = new VisualStudioFileChangeTracker(filePath, LoggerFactory, fileChangeService, JoinableTaskFactory.Context);

        var called = false;
        tracker.Changed += (sender, args) =>
        {
            called = true;
            Assert.Same(sender, tracker);
            Assert.Equal(filePath, args.FilePath);
            Assert.Equal((FileChangeKind)expectedKind, args.Kind);
        };

        // Act
        tracker.FilesChanged(fileCount: 1, filePaths: [filePath], fileChangeFlags: [fileChangeFlag]);
        await tracker._fileChangedTask!;

        // Assert
        Assert.True(called);
    }
}
