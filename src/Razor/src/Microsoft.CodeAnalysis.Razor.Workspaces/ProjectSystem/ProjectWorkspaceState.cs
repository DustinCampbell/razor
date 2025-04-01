// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal sealed record class ProjectWorkspaceState : IEquatable<ProjectWorkspaceState>
{
    public static readonly ProjectWorkspaceState Default = new([]);

    public ImmutableArray<TagHelperDescriptor> TagHelpers { get; }

    private Checksum? _checksum;

    private ProjectWorkspaceState(ImmutableArray<TagHelperDescriptor> tagHelpers)
    {
        TagHelpers = tagHelpers;
    }

    public static ProjectWorkspaceState Create(ImmutableArray<TagHelperDescriptor> tagHelpers)
        => tagHelpers.IsEmpty
            ? Default
            : new(tagHelpers);

    private Checksum Checksum
    {
        get
        {
            return _checksum ?? InterlockedOperations.Initialize(ref _checksum, ComputeChecksum(TagHelpers));

            static Checksum ComputeChecksum(ImmutableArray<TagHelperDescriptor> tagHelpers)
            {
                var builder = new Checksum.Builder();

                foreach (var tagHelper in tagHelpers)
                {
                    builder.AppendData(tagHelper.Checksum);
                }

                return builder.FreeAndGetChecksum();
            }
        }
    }

    public bool Equals(ProjectWorkspaceState? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null &&
               Checksum.Equals(other.Checksum);
    }

    public override int GetHashCode()
        => Checksum.GetHashCode();
}
