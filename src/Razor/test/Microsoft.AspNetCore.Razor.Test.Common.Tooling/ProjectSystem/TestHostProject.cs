// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.Test.Common.ProjectSystem;

internal static class TestHostProject
{
    public static HostProject Create(string filePath)
        => Create(new ProjectKey(Path.Combine(Path.GetDirectoryName(filePath) ?? @"\\path", "obj")), filePath);

    public static HostProject Create(ProjectKey projectKey, string filePath)
        => new(projectKey, filePath, RazorConfiguration.Default, rootNamespace: "TestRootNamespace");
}
