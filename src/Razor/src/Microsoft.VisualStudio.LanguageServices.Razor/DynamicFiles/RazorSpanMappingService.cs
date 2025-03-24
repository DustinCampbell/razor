// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.Razor.DynamicFiles;

internal sealed class RazorSpanMappingService(RazorCodeDocument codeDocument) : IRazorSpanMappingService
{
    internal RazorCodeDocument CodeDocument { get; } = codeDocument;

    internal ImmutableArray<RazorMappedSpanResult> MapsSpans(IEnumerable<TextSpan> spans)
    {
        var sourceText = CodeDocument.Source.Text;
        var csharpDocument = CodeDocument.GetRequiredCSharpDocument();
        var filePath = CodeDocument.Source.FilePath.AssumeNotNull();

        using var results = new PooledArrayBuilder<RazorMappedSpanResult>();

        foreach (var span in spans)
        {
            if (TryGetMappedSpans(span, sourceText, csharpDocument, out var linePositionSpan, out var mappedSpan))
            {
                results.Add(new(filePath, linePositionSpan, mappedSpan));
            }
            else
            {
                results.Add(default);
            }
        }

        return results.DrainToImmutable();
    }

    public Task<ImmutableArray<RazorMappedSpanResult>> MapSpansAsync(
        Document document,
        IEnumerable<TextSpan> spans,
        CancellationToken cancellationToken)
    {
        ArgHelper.ThrowIfNull(document);
        ArgHelper.ThrowIfNull(spans);

        var results = MapsSpans(spans);

        return Task.FromResult(results);
    }

    // Internal for testing.
    internal static bool TryGetMappedSpans(TextSpan span, SourceText razorText, RazorCSharpDocument csharpDocument, out LinePositionSpan linePositionSpan, out TextSpan mappedSpan)
    {
        foreach (var mapping in csharpDocument.SourceMappings)
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
                linePositionSpan = razorText.GetLinePositionSpan(mappedSpan);
                return true;
            }
        }

        mappedSpan = default;
        linePositionSpan = default;
        return false;
    }
}
