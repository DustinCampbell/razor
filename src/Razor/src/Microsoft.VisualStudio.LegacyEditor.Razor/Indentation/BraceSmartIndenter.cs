﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.VisualStudio.Razor.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Threading;
using ITextBuffer = Microsoft.VisualStudio.Text.ITextBuffer;

namespace Microsoft.VisualStudio.LegacyEditor.Razor.Indentation;

/// <summary>
/// This class is responsible for handling situations where Roslyn and the HTML editor cannot auto-indent Razor code.
/// </summary>
/// <example>
/// Attempting to insert a newline (pipe indicates the cursor):
/// @{ |}
/// Should result in the text buffer looking like the following:
/// @{
///     |
/// }
/// This is also true for directive block scenarios.
/// </example>
internal class BraceSmartIndenter : IDisposable
{
    internal record BraceIndentationContext(ITextView FocusedTextView, int ChangePosition);

    private readonly ITextBuffer _textBuffer;
    private readonly JoinableTaskContext _joinableTaskContext;
    private readonly IVisualStudioDocumentTracker _documentTracker;
    private readonly IEditorOperationsFactoryService _editorOperationsFactory;
    private readonly StringBuilder _indentBuilder = new();
    private BraceIndentationContext? _context;

    // Internal for testing
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    internal BraceSmartIndenter()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
    }

    public BraceSmartIndenter(
        IVisualStudioDocumentTracker documentTracker,
        IEditorOperationsFactoryService editorOperationsFactory,
        JoinableTaskContext joinableTaskContext)
    {
        Debug.Assert(documentTracker.TextBuffer.IsLegacyCoreRazorBuffer());

        _joinableTaskContext = joinableTaskContext;
        _documentTracker = documentTracker;
        _editorOperationsFactory = editorOperationsFactory;
        _textBuffer = _documentTracker.TextBuffer;
        _textBuffer.Changed += TextBuffer_OnChanged;
        _textBuffer.PostChanged += TextBuffer_OnPostChanged;
    }

    public void Dispose()
    {
        _joinableTaskContext.AssertUIThread();

        _textBuffer.Changed -= TextBuffer_OnChanged;
        _textBuffer.PostChanged -= TextBuffer_OnPostChanged;
    }

    // Internal for testing
    internal void TriggerSmartIndent(ITextView textView)
    {
        // This forces the smart indent. For example attempting to enter a newline between the functions directive:
        // @functions {} will not auto-indent in between the braces unless we forcefully move to end of line.
        var editorOperations = _editorOperationsFactory.GetEditorOperations(textView);
        editorOperations.MoveToEndOfLine(false);
    }

    // Internal for testing
    internal void TextBuffer_OnChanged(object? sender, TextContentChangedEventArgs args)
    {
        _joinableTaskContext.AssertUIThread();

        if (!args.TextChangeOccurred(out var changeInformation))
        {
            return;
        }

        var newText = changeInformation.newText;
        if (!_documentTracker.TextBuffer.TryGetCodeDocument(out var codeDocument))
        {
            // Parse not available.
            return;
        }

        var syntaxTree = codeDocument.GetRequiredSyntaxTree();
        if (TryCreateIndentationContext(changeInformation.firstChange.NewPosition, newText.Length, newText, syntaxTree, _documentTracker, out var context))
        {
            _context = context;
        }
    }

    private void TextBuffer_OnPostChanged(object sender, EventArgs e)
    {
        _joinableTaskContext.AssertUIThread();

        var context = _context;
        _context = null;

        if (context is not null)
        {
            // Save the current caret position
            var textView = context.FocusedTextView;
            var caret = textView.Caret.Position.BufferPosition;
            var textViewBuffer = textView.TextBuffer;
            var indent = CalculateIndent(textViewBuffer, context.ChangePosition);

            // Current state, pipe is cursor:
            // @{
            // |}

            // Insert the completion text, i.e. "\r\n      "
            InsertIndent(caret.Position, indent, textViewBuffer);

            // @{
            //
            // |}

            // Place the caret inbetween the braces (before our indent).
            RestoreCaretTo(caret.Position, textView);

            // @{
            // |
            // }

            // For Razor metacode cases the editor's smart indent wont kick in automatically.
            TriggerSmartIndent(textView);

            // @{
            //     |
            // }
        }
    }

    private string CalculateIndent(ITextBuffer buffer, int from)
    {
        // Get the line text of the block start
        var currentSnapshotPoint = new SnapshotPoint(buffer.CurrentSnapshot, from);
        var line = buffer.CurrentSnapshot.GetLineFromPosition(currentSnapshotPoint);
        var lineText = line.GetText();

        // Gather up the indent from the start block
        _indentBuilder.Append(line.GetLineBreakText());
        foreach (var ch in lineText)
        {
            if (!char.IsWhiteSpace(ch))
            {
                break;
            }

            _indentBuilder.Append(ch);
        }

        var indent = _indentBuilder.ToString();
        _indentBuilder.Clear();

        return indent;
    }

    // Internal for testing
    internal static void InsertIndent(int insertLocation, string indent, ITextBuffer textBuffer)
    {
        var edit = textBuffer.CreateEdit();
        edit.Insert(insertLocation, indent);
        edit.Apply();
    }

    // Internal for testing
    internal static void RestoreCaretTo(int caretPosition, ITextView textView)
    {
        var currentSnapshotPoint = new SnapshotPoint(textView.TextBuffer.CurrentSnapshot, caretPosition);
        textView.Caret.MoveTo(currentSnapshotPoint);
    }

    // Internal for testing
    internal static bool TryCreateIndentationContext(
        int changePosition,
        int changeLength,
        string finalText,
        RazorSyntaxTree syntaxTree,
        IVisualStudioDocumentTracker documentTracker,
        [NotNullWhen(returnValue: true)] out BraceIndentationContext? context)
    {
        var focusedTextView = documentTracker.GetFocusedTextView();
        if (focusedTextView != null && ParserHelpers.IsNewLine(finalText))
        {
            if (!AtApplicableRazorBlock(changePosition, syntaxTree))
            {
                context = null;
                return false;
            }

            var currentSnapshot = documentTracker.TextBuffer.CurrentSnapshot;
            var preChangeLineSnapshot = currentSnapshot.GetLineFromPosition(changePosition);

            // Handle the case where the \n comes through separately from the \r and the position
            // on the line is beyond what the GetText call above gives back.
            var linePosition = Math.Min(preChangeLineSnapshot.Length, changePosition - preChangeLineSnapshot.Start) - 1;

            if (AfterOpeningBrace(linePosition, preChangeLineSnapshot))
            {
                var afterChangePosition = changePosition + changeLength;
                var afterChangeLineSnapshot = currentSnapshot.GetLineFromPosition(afterChangePosition);
                var afterChangeLinePosition = afterChangePosition - afterChangeLineSnapshot.Start;

                if (BeforeClosingBrace(afterChangeLinePosition, afterChangeLineSnapshot))
                {
                    context = new BraceIndentationContext(focusedTextView, changePosition);
                    return true;
                }
            }
        }

        context = null;
        return false;
    }

    // Internal for testing
    internal static bool AtApplicableRazorBlock(int changePosition, RazorSyntaxTree syntaxTree)
    {
        // Our goal here is to return true when we're acting on code blocks that have all
        // whitespace content and are surrounded by metacode.
        // Some examples:
        // @functions { |}
        // @section foo { |}
        // @{ |}

        var change = new SourceChange(changePosition, 0, string.Empty);
#pragma warning disable CS0618 // Type or member is obsolete, BraceSmartIndenter is only used in legacy scenarios
        var owner = syntaxTree.Root.LocateOwner(change);
#pragma warning restore CS0618 // Type or member is obsolete

        if (IsUnlinkedSpan(owner))
        {
            return false;
        }

        if (SurroundedByInvalidContent(owner))
        {
            return false;
        }

        if (ContainsInvalidContent(owner))
        {
            return false;
        }

        // Indentable content inside of a code block.
        return true;
    }

    // Internal for testing
    internal static bool ContainsInvalidContent(SyntaxNode owner)
    {
        // We only support whitespace based content. Any non-whitespace content is an unknown to us
        // in regards to indentation.
        foreach (var child in owner.ChildNodesAndTokens())
        {
            if (!child.AsToken(out var token) ||
                !string.IsNullOrWhiteSpace(token.Content))
            {
                return true;
            }
        }

        return false;
    }

    // Internal for testing
    internal static bool IsUnlinkedSpan([NotNullWhen(false)] SyntaxNode? owner)
    {
        return owner is null ||
               owner.NextSpan() is null ||
               owner.PreviousSpan() is null;
    }

    // Internal for testing
    internal static bool SurroundedByInvalidContent(SyntaxNode owner)
    {
        return !(owner.NextSpan()?.IsMetaCodeSpanKind() ?? false) ||
               !(owner.PreviousSpan()?.IsMetaCodeSpanKind() ?? false);
    }

    internal static bool BeforeClosingBrace(int linePosition, ITextSnapshotLine lineSnapshot)
    {
        var lineText = lineSnapshot.GetText();
        for (; linePosition < lineSnapshot.Length; linePosition++)
        {
            if (!char.IsWhiteSpace(lineText[linePosition]))
            {
                break;
            }
        }

        var beforeClosingBrace = linePosition < lineSnapshot.Length && lineText[linePosition] == '}';
        return beforeClosingBrace;
    }

    internal static bool AfterOpeningBrace(int linePosition, ITextSnapshotLine lineSnapshot)
    {
        var lineText = lineSnapshot.GetText();
        for (; linePosition >= 0; linePosition--)
        {
            if (!char.IsWhiteSpace(lineText[linePosition]))
            {
                break;
            }
        }

        var afterClosingBrace = linePosition >= 0 && lineText[linePosition] == '{';
        return afterClosingBrace;
    }
}
