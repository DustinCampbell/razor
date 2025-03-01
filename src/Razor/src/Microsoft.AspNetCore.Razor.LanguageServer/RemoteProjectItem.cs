// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.LanguageServer;

internal sealed class RemoteProjectItem(string filePath, string physicalPath, RazorSourceCodeKind? sourceCodeKind) : RazorProjectItem
{
    public override string BasePath => "/";
    public override string FilePath { get; } = filePath;
    public override string PhysicalPath { get; } = physicalPath;
    public override RazorSourceCodeKind SourceCodeKind { get; } = ComputeFileKind(sourceCodeKind, filePath);
    public override string RelativePhysicalPath { get; } = filePath.StartsWith('/') ? filePath[1..] : filePath;

    public override bool Exists
    {
        get
        {
            var platformPath = PhysicalPath[1..];

            return Path.IsPathRooted(platformPath)
                ? File.Exists(platformPath)
                : File.Exists(PhysicalPath);
        }
    }

    public override Stream Read()
        => throw new NotSupportedException();
}
