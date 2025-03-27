// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.ProjectSystem;

internal sealed class ProjectWorkspaceState : IEquatable<ProjectWorkspaceState>
{
    public static readonly ProjectWorkspaceState Default = new([]);

    public ImmutableArray<TagHelperDescriptor> TagHelpers { get; }

    private Checksum? _checksum;

    private ProjectWorkspaceState(ImmutableArray<TagHelperDescriptor> tagHelpers)
    {
        TagHelpers = tagHelpers.NullToEmpty();
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

            static Checksum ComputeChecksum(ImmutableArray<TagHelperDescriptor> descriptors)
            {
                if (descriptors.IsEmpty)
                {
                    return Checksum.Null;
                }

                var builder = new Checksum.Builder();

                foreach (var descriptor in descriptors)
                {
                    builder.AppendData(descriptor.Checksum);
                }

                return builder.FreeAndGetChecksum();
            }
        }
    }

    public override bool Equals(object? obj)
        => obj is ProjectWorkspaceState other &&
           Equals(other);

    public bool Equals(ProjectWorkspaceState? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) ||
               Checksum.Equals(other.Checksum);
    }

    public override int GetHashCode()
        => Checksum.GetHashCode();
}
