﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Serialization.Json;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.Razor.LiveShare.Serialization;

internal class ProjectSnapshotHandleProxyJsonConverter : ObjectJsonConverter<ProjectSnapshotHandleProxy>
{
    public static readonly ProjectSnapshotHandleProxyJsonConverter Instance = new();

    protected override ProjectSnapshotHandleProxy ReadFromProperties(JsonDataReader reader)
    {
        var filePath = reader.ReadNonNullUri(nameof(ProjectSnapshotHandleProxy.FilePath));
        var intermediateOutputPath = reader.ReadNonNullUri(nameof(ProjectSnapshotHandleProxy.IntermediateOutputPath));
        var configuration = reader.ReadNonNullObject(nameof(ProjectSnapshotHandleProxy.Configuration), ObjectReaders.ReadConfigurationFromProperties);
        var rootNamespace = reader.ReadStringOrNull(nameof(ProjectSnapshotHandleProxy.RootNamespace));
        var projectWorkspaceState = reader.ReadObjectOrNull(nameof(ProjectSnapshotHandleProxy.ProjectWorkspaceState), ObjectReaders.ReadProjectWorkspaceStateFromProperties) ?? ProjectWorkspaceState.Default;

        return new(filePath, intermediateOutputPath, configuration, rootNamespace, projectWorkspaceState);
    }

    protected override void WriteProperties(JsonDataWriter writer, ProjectSnapshotHandleProxy value)
    {
        writer.Write(nameof(value.FilePath), value.FilePath);
        writer.Write(nameof(value.IntermediateOutputPath), value.IntermediateOutputPath);
        writer.WriteObject(nameof(value.Configuration), value.Configuration, ObjectWriters.WriteProperties);
        writer.WriteIfNotNull(nameof(value.RootNamespace), value.RootNamespace);
        writer.WriteObjectIfNotNull(nameof(value.ProjectWorkspaceState), value.ProjectWorkspaceState, ObjectWriters.WriteProperties);
    }
}
