// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.Razor.LiveShare;

// This type must be public because it is exposed by a public interface that is implemented as
// an RPC proxy by live share. However, its properties and constructor are intentionally internal
// because they expose internal compiler APIs.
public sealed class ProjectSnapshotHandleProxy
{
    internal Uri FilePath { get; }
    internal Uri ProjectKeyId { get; }
    internal RazorConfiguration Configuration { get; }
    internal string? RootNamespace { get; }
    internal ProjectWorkspaceState ProjectWorkspaceState { get; }

    internal ProjectSnapshotHandleProxy(
        Uri filePath,
        Uri projectKeyId,
        RazorConfiguration configuration,
        string? rootNamespace,
        ProjectWorkspaceState projectWorkspaceState)
    {
        FilePath = filePath;
        ProjectKeyId = projectKeyId;
        Configuration = configuration;
        RootNamespace = rootNamespace;
        ProjectWorkspaceState = projectWorkspaceState;
    }
}
