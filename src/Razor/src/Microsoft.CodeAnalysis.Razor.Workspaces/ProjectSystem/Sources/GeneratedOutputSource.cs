// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.Razor.Threading;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem.Sources;

internal sealed class GeneratedOutputSource
{
    private readonly SemaphoreSlim _gate = new(initialCount: 1);

    // Hold the output in a WeakReference to avoid memory leaks in the case of a long-lived
    // document snapshots. In particular, the DynamicFileInfo system results in the Roslyn
    // workspace holding onto document snapshots.
    private WeakReference<RazorCodeDocument>? _weakOutput;

    private CachedData? _cachedData;

    public bool TryGetValue([NotNullWhen(true)] out RazorCodeDocument? result)
    {
        var weakOutput = _weakOutput;
        if (weakOutput is null)
        {
            result = null;
            return false;
        }

        return weakOutput.TryGetTarget(out result);
    }

    public async ValueTask<RazorCodeDocument> GetValueAsync(DocumentSnapshot document, CancellationToken cancellationToken)
    {
        if (TryGetValue(out var result))
        {
            return result;
        }

        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            if (TryGetValue(out result))
            {
                return result;
            }

            var project = document.Project;
            var projectEngine = project.ProjectEngine;
            var compilerOptions = project.CompilerOptions;

            result = await CompilationHelpers
                .GenerateCodeDocumentAsync(document, projectEngine, compilerOptions, cancellationToken)
                .ConfigureAwait(false);

            if (_weakOutput is null)
            {
                _weakOutput = new(result);
            }
            else
            {
                _weakOutput.SetTarget(result);
            }

            //_output = result;
            _cachedData = CachedData.From(result);

            return result;
        }
    }

    public bool IsAffectedBy(ProjectWorkspaceState projectWorkspaceState)
    {
        var cachedData = _cachedData;
        if (cachedData is null)
        {
            // No cached data, assume we are affected.
            return true;
        }

        if (cachedData.HasDiagnostics)
        {
            // If the last compilation had any diagnostics, assume we are affected.
            return true;
        }

        // We're only affected if the new tag helpers don't contain all of the checksums
        // from the previous run.
        return !projectWorkspaceState.ContainsAll(cachedData.ReferencedTagHelperChecksums);
    }

    /// <summary>
    ///  Holds cached data about the RazorCodeDocument to avoid unnecessary re-computation.
    /// </summary>
    private sealed class CachedData
    {
        public bool HasDiagnostics { get; }
        public ImmutableArray<Checksum> ReferencedTagHelperChecksums { get; }

        private CachedData(bool hasDiagnostics, ImmutableArray<Checksum> referencedTagHelperChecksums)
        {
            HasDiagnostics = hasDiagnostics;
            ReferencedTagHelperChecksums = referencedTagHelperChecksums;
        }

        public static CachedData From(RazorCodeDocument codeDocument)
        {
            var hasDiagnostics = codeDocument.GetSyntaxTree() is { } syntaxTree &&
                                 syntaxTree.Diagnostics.Length > 0;

            var referencedTagHelperChecksums = codeDocument.GetReferencedTagHelpers() is { } referencedTagHelpers
                ? referencedTagHelpers.SelectAsArray(static x => x.Checksum)
                : [];

            return new(hasDiagnostics, referencedTagHelperChecksums);
        }
    }
}
