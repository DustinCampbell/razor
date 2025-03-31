// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#if JSONSERIALIZATION_PROJECTSYSTEM
using System.IO;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Serialization;
using SR = Microsoft.AspNetCore.Razor.Serialization.Json.Internal.Strings;

namespace Microsoft.AspNetCore.Razor.Serialization.Json;

internal static partial class ObjectReaders
{
    public static HostDocument ReadHostDocumentFromProperties(JsonDataReader reader)
    {
        var filePath = reader.ReadNonNullString(nameof(HostDocument.FilePath));
        var targetPath = reader.ReadNonNullString(nameof(HostDocument.TargetPath));
        var fileKind = (RazorFileKind)reader.ReadInt32OrDefault(nameof(HostDocument.FileKind), defaultValue: (int)RazorFileKind.Component);

        return new HostDocument(filePath, targetPath, fileKind);
    }

    public static ProjectWorkspaceState ReadProjectWorkspaceStateFromProperties(JsonDataReader reader)
    {
        var tagHelpers = reader.ReadImmutableArrayOrEmpty(nameof(ProjectWorkspaceState.TagHelpers),
            static r => ReadTagHelper(r
#if JSONSERIALIZATION_ENABLETAGHELPERCACHE
                , useCache: true
#endif
            ));

        return ProjectWorkspaceState.Create(tagHelpers);
    }

    public static RazorProjectInfo ReadProjectInfoFromProperties(JsonDataReader reader)
    {
        if (!reader.TryReadInt32(WellKnownPropertyNames.Version, out var version) || version != SerializationFormat.Version)
        {
            throw new RazorProjectInfoSerializationException(SR.Unsupported_razor_project_info_version_encountered);
        }

        var projectKeyId = reader.ReadNonNullString(nameof(RazorProjectInfo.ProjectKey));
        var filePath = reader.ReadNonNullString(nameof(RazorProjectInfo.FilePath));
        var configuration = reader.ReadObject(nameof(RazorProjectInfo.Configuration), ReadConfigurationFromProperties) ?? RazorConfiguration.Default;
        var projectWorkspaceState = reader.ReadObject(nameof(RazorProjectInfo.ProjectWorkspaceState), ReadProjectWorkspaceStateFromProperties) ?? ProjectWorkspaceState.Default;
        var rootNamespace = reader.ReadString(nameof(RazorProjectInfo.RootNamespace));
        var documents = reader.ReadImmutableArray(nameof(RazorProjectInfo.Documents), static r => r.ReadNonNullObject(ReadHostDocumentFromProperties));

        var displayName = Path.GetFileNameWithoutExtension(filePath);

        return new RazorProjectInfo(new ProjectKey(projectKeyId), filePath, configuration, rootNamespace, displayName, projectWorkspaceState, documents);
    }
}
#endif
