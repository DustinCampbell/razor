// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Internal;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal sealed record class HostProject
{
    public ProjectKey Key { get; }

    /// <summary>
    /// Gets the full path to the .csproj file for this project
    /// </summary>
    public string FilePath { get; }

    public RazorConfiguration Configuration { get; init; }

    public string? RootNamespace { get; init; }

    /// <summary>
    /// An extra user-friendly string to show in the VS navigation bar to help the user, of the form "{ProjectFileName} ({Flavor})"
    /// </summary>
    public string DisplayName { get; }

    public HostProject(
        ProjectKey key,
        string filePath,
        RazorConfiguration configuration,
        string? rootNamespace,
        string? displayName = null)
    {
        Key = key;
        FilePath = filePath;
        Configuration = configuration;
        RootNamespace = rootNamespace;
        DisplayName = displayName ?? Path.GetFileNameWithoutExtension(filePath);
    }

    public bool Equals(HostProject? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null &&
               Key == other.Key &&
               FilePathComparer.Instance.Equals(FilePath, other.FilePath) &&
               Configuration == other.Configuration &&
               RootNamespace == other.RootNamespace &&
               DisplayName == other.DisplayName;
    }

    public override int GetHashCode()
    {
        var hash = HashCodeCombiner.Start();

        hash.Add(Key);
        hash.Add(FilePath, FilePathComparer.Instance);
        hash.Add(Configuration);
        hash.Add(RootNamespace);
        hash.Add(DisplayName);

        return hash.CombinedHash;
    }
}
