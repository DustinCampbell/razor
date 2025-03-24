// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Telemetry;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.Razor.DynamicFiles;

internal sealed class RazorMappingService(
    RazorCodeDocument codeDocument,
    ITelemetryReporter telemetryReporter,
    ILoggerFactory loggerFactory) : IRazorMappingService
{
    private readonly RazorCodeDocument _codeDocument = codeDocument;

    private readonly ITelemetryReporter _telemetryReporter = telemetryReporter;
    private readonly IDocumentMappingService _documentMappingService = new DocumentMappingService(loggerFactory);
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<RazorMappingService>();

    public Task<ImmutableArray<RazorMappedSpanResult>> MapSpansAsync(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken)
    {
        var source = _codeDocument.Source.Text;

        var csharpDocument = _codeDocument.GetRequiredCSharpDocument();
        var filePath = _codeDocument.Source.FilePath.AssumeNotNull();

        using var results = new PooledArrayBuilder<RazorMappedSpanResult>();

        foreach (var span in spans)
        {
            if (TryGetMappedSpans(span, source, csharpDocument, out var linePositionSpan, out var mappedSpan))
            {
                results.Add(new(filePath, linePositionSpan, mappedSpan));
            }
            else
            {
                results.Add(default);
            }
        }

        if (results.Count == 0)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorMappedSpanResult>();
        }

        return Task.FromResult(results.DrainToImmutable());
    }

    public async Task<ImmutableArray<RazorMappedEditoResult>> MapTextChangesAsync(Document oldDocument, Document newDocument, CancellationToken cancellationToken)
    {
        try
        {
            var changes = await newDocument.GetTextChangesAsync(oldDocument, cancellationToken).ConfigureAwait(false);
            var results = await RazorEditHelper
                .MapCSharpEditsAsync(
                    changes.SelectAsArray(static c => c.ToRazorTextChange()),
                    _codeDocument,
                    _documentMappingService,
                    _telemetryReporter,
                    cancellationToken)
                .ConfigureAwait(false);

            // Grab the inner array from the resulting ImmutableArray to perform a non-mutating conversion
            // without creating an extra array copy.
            var array = ImmutableCollectionsMarshal.AsArray(results);
            var textChanges = Array.ConvertAll(array, static x => x.ToTextChange());

            _logger.LogTrace($"""
                Before:
                {DisplayEdits(changes)}

                After:
                {DisplayEdits(textChanges)}
                """);

            var filePath = _codeDocument.Source.FilePath.AssumeNotNull();

            return [new RazorMappedEditoResult() { FilePath = filePath, TextChanges = textChanges }];
        }
        catch (Exception ex)
        {
            _telemetryReporter.ReportFault(ex, "Failed to map edits");
            return [];
        }

        static string DisplayEdits(IEnumerable<TextChange> changes)
        {
            return string.Join(
                Environment.NewLine,
                changes.Select(e => $"{e.Span} => '{e.NewText}'"));
        }
    }

    // Internal for testing.
    internal static bool TryGetMappedSpans(TextSpan span, SourceText source, RazorCSharpDocument output, out LinePositionSpan linePositionSpan, out TextSpan mappedSpan)
    {
        foreach (var mapping in output.SourceMappings)
        {
            var original = mapping.OriginalSpan.AsTextSpan();
            var generated = mapping.GeneratedSpan.AsTextSpan();

            if (!generated.Contains(span))
            {
                // If the search span isn't contained within the generated span, it is not a match.
                // A C# identifier won't cover multiple generated spans.
                continue;
            }

            var leftOffset = span.Start - generated.Start;
            var rightOffset = span.End - generated.End;
            if (leftOffset >= 0 && rightOffset <= 0)
            {
                // This span mapping contains the span.
                mappedSpan = new TextSpan(original.Start + leftOffset, (original.End + rightOffset) - (original.Start + leftOffset));
                linePositionSpan = source.GetLinePositionSpan(mappedSpan);
                return true;
            }
        }

        mappedSpan = default;
        linePositionSpan = default;
        return false;
    }

    private sealed class DocumentMappingService(ILoggerFactory loggerFactory) : AbstractDocumentMappingService(loggerFactory.GetOrCreateLogger<DocumentMappingService>())
    {
    }
}
