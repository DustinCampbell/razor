// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

internal static class CodeRenderingContextExtensions
{
    public static IDisposable BuildLinePragma(
        this CodeRenderingContext context,
        SourceSpan? span,
        bool suppressLineDefaultAndHidden = false)
    {
        if (string.IsNullOrEmpty(span?.FilePath))
        {
            // Can't build a valid line pragma without a file path.
            return NullDisposable.Default;
        }

        return new LinePragmaWriter(context.CodeWriter, span.Value, context, 0, useEnhancedLinePragma: false, suppressLineDefaultAndHidden);
    }

    public static IDisposable BuildEnhancedLinePragma(
        this CodeRenderingContext context,
        SourceSpan? span,
        int characterOffset = 0,
        bool suppressLineDefaultAndHidden = false)
    {
        if (string.IsNullOrEmpty(span?.FilePath))
        {
            // Can't build a valid line pragma without a file path.
            return NullDisposable.Default;
        }

        return new LinePragmaWriter(context.CodeWriter, span.Value, context, characterOffset, useEnhancedLinePragma: true, suppressLineDefaultAndHidden);
    }

    private class LinePragmaWriter : IDisposable
    {
        private readonly CodeWriter _writer;
        private readonly CodeRenderingContext _context;
        private readonly int _startIndent;
        private readonly int _startLineIndex;
        private readonly SourceSpan _span;
        private readonly bool _suppressLineDefaultAndHidden;

        public LinePragmaWriter(
            CodeWriter writer,
            SourceSpan span,
            CodeRenderingContext context,
            int characterOffset,
            bool useEnhancedLinePragma = false,
            bool suppressLineDefaultAndHidden = false)
        {
            Debug.Assert(context.Options.DesignTime || useEnhancedLinePragma, "Runtime generation should only use enhanced line pragmas");

            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            _writer = writer;
            _context = context;
            _suppressLineDefaultAndHidden = suppressLineDefaultAndHidden;
            _startIndent = _writer.CurrentIndent;
            _writer.CurrentIndent = 0;
            _span = span;

            var endsWithNewline = _writer.LastChar is '\n';
            if (!endsWithNewline)
            {
                _writer.WriteLine();
            }

            if (!_context.Options.SuppressNullabilityEnforcement)
            {
                _writer.WriteLine("#nullable restore");
            }

            var ensurePathBackslashes = context.Options.RemapLinePragmaPathsOnWindows && PlatformInformation.IsWindows;
            if (useEnhancedLinePragma && _context.Options.UseEnhancedLinePragma)
            {
                writer.WriteEnhancedLineNumberDirective(span, characterOffset, ensurePathBackslashes);
            }
            else
            {
                writer.WriteLineNumberDirective(span, ensurePathBackslashes);
            }

            // Capture the line index after writing the #line directive.
            _startLineIndex = writer.Location.LineIndex;

            if (useEnhancedLinePragma)
            {
                // If the caller requested an enhanced line directive, but we fell back to regular ones, write out the extra padding that is required
                if (!_context.Options.UseEnhancedLinePragma)
                {
                    context.CodeWriter.WritePadding(0, span, context);
                    characterOffset = 0;
                }

                context.AddSourceMappingFor(span, characterOffset);
            }
        }

        public void Dispose()
        {
            // Need to add an additional line at the end IF there wasn't one already written.
            // This is needed to work with the C# editor's handling of #line ...
            var endsWithNewline = _writer.LastChar is '\n';

            // Always write at least 1 empty line to potentially separate code from pragmas.
            _writer.WriteLine();

            // Check if the previous empty line wasn't enough to separate code from pragmas.
            if (!endsWithNewline)
            {
                _writer.WriteLine();
            }

            var lineCount = _writer.Location.LineIndex - _startLineIndex;
            var linePragma = new LinePragma(_span.LineIndex, lineCount, _span.FilePath, _span.EndCharacterIndex, _span.EndCharacterIndex, _span.CharacterIndex);
            _context.AddLinePragma(linePragma);

            if (!_suppressLineDefaultAndHidden)
            {
                _writer
                    .WriteLine("#line default")
                    .WriteLine("#line hidden");
            }

            if (!_context.Options.SuppressNullabilityEnforcement)
            {
                _writer.WriteLine("#nullable disable");
            }

            _writer.CurrentIndent = _startIndent;

        }
    }

    private class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Default = new NullDisposable();

        private NullDisposable()
        {
        }

        public void Dispose()
        {
        }
    }

}
