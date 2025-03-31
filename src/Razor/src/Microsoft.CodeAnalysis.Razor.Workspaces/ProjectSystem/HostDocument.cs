// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Internal;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal sealed record class HostDocument : IComparable<HostDocument>
{
    public RazorFileKind FileKind { get; init; }
    public string FilePath { get; init; }
    public string TargetPath { get; init; }

    public HostDocument(string filePath, string targetPath, RazorFileKind? fileKind = null)
    {
        FilePath = filePath;
        TargetPath = targetPath;
        FileKind = fileKind ?? FileKinds.GetFileKindFromPath(filePath);
    }

    public bool Equals(HostDocument? other)
        => other is not null &&
           FilePathComparer.Instance.Equals(FilePath, other.FilePath) &&
           FilePathComparer.Instance.Equals(TargetPath, other.TargetPath) &&
           StringComparer.OrdinalIgnoreCase.Equals(FileKind, other.FileKind);

    public override int GetHashCode()
    {
        var combiner = HashCodeCombiner.Start();

        combiner.Add(FilePath, FilePathComparer.Instance);
        combiner.Add(TargetPath, FilePathComparer.Instance);
        combiner.Add(FileKind, StringComparer.OrdinalIgnoreCase);

        return combiner.CombinedHash;
    }

    public int CompareTo(HostDocument? other)
    {
        if (other is null)
        {
            return -1; // Sort null values to the end
        }

        // Compare FilePath first
        var result = FilePathComparer.Instance.Compare(FilePath, other.FilePath);
        if (result != 0)
        {
            return result;
        }

        // If FilePath is equal, compare TargetPath
        result = FilePathComparer.Instance.Compare(TargetPath, other.TargetPath);
        if (result != 0)
        {
            return result;
        }

        // If TargetPath is equal, compare FileKind
        return StringComparer.OrdinalIgnoreCase.Compare(FileKind, other.FileKind);
    }
}
