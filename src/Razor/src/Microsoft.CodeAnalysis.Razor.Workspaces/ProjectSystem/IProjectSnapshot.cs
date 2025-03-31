// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal interface IProjectSnapshot
{
    ProjectKey Key { get; }

    IEnumerable<string> DocumentFilePaths { get; }

    /// <summary>
    /// Gets the full path to the .csproj file for this project
    /// </summary>
    string FilePath { get; }

    string? RootNamespace { get; }
    string DisplayName { get; }

    ValueTask<ImmutableArray<TagHelperDescriptor>> GetTagHelpersAsync(CancellationToken cancellationToken);

    bool ContainsDocument(string filePath);
    bool TryGetDocument(string filePath, [NotNullWhen(true)] out IDocumentSnapshot? document);
}
