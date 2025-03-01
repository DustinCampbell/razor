// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal sealed record class HostDocument
{
    public RazorSourceCodeKind SourceCodeKind { get; init; }
    public string FilePath { get; init; }
    public string TargetPath { get; init; }

    public HostDocument(string filePath, string targetPath, RazorSourceCodeKind? sourceCodeKind = null)
    {
        FilePath = filePath;
        TargetPath = targetPath;

        SourceCodeKind = sourceCodeKind is RazorSourceCodeKind value || SourceCodeFileKinds.TryGetSourceCodeKind(filePath, out value)
            ? value
            : RazorSourceCodeKind.Legacy;

        Debug.Assert(SourceCodeKind != RazorSourceCodeKind.None);
    }
}
