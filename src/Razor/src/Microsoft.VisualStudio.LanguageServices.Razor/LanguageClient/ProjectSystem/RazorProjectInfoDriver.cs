﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.Razor.LanguageClient.ProjectSystem;

internal sealed partial class RazorProjectInfoDriver : AbstractRazorProjectInfoDriver
{
    private readonly ProjectSnapshotManager _projectManager;

    public RazorProjectInfoDriver(
        ProjectSnapshotManager projectManager,
        ILoggerFactory loggerFactory,
        TimeSpan? delay = null) : base(loggerFactory, delay)
    {
        _projectManager = projectManager;

        StartInitialization();
    }

    protected override Task InitializeAsync(CancellationToken cancellationToken)
    {
        // Even though we aren't mutating the project snapshot manager, we call UpdateAsync(...) here to ensure
        // that we run on its dispatcher. That ensures that no changes will code in while we are iterating the
        // current set of projects and connected to the Changed event.
        return _projectManager.UpdateAsync(updater =>
        {
            foreach (var project in updater.GetProjects())
            {
                EnqueueUpdate(project.ToRazorProjectInfo());
            }

            _projectManager.Changed += ProjectManager_Changed;
        },
        cancellationToken);
    }

    private void ProjectManager_Changed(object sender, ProjectChangeEventArgs e)
    {
        // Don't do any work if the solution is closing
        if (e.IsSolutionClosing)
        {
            return;
        }

        switch (e.Kind)
        {
            case ProjectChangeKind.ProjectAdded:
            case ProjectChangeKind.ProjectChanged:
            case ProjectChangeKind.DocumentRemoved:
            case ProjectChangeKind.DocumentAdded:
                var newer = e.Newer.AssumeNotNull();
                EnqueueUpdate(newer.ToRazorProjectInfo());
                break;

            case ProjectChangeKind.ProjectRemoved:
                var older = e.Older.AssumeNotNull();
                EnqueueRemove(older.Key);
                break;

            case ProjectChangeKind.DocumentChanged:
                break;

            default:
                throw new NotSupportedException($"Unsupported {nameof(ProjectChangeKind)}: {e.Kind}");
        }
    }
}
