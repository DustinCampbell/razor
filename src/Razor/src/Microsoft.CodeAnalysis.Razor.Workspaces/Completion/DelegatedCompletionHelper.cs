// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Protocol.Completion;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using LspRange = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.Completion;

using SyntaxNode = AspNetCore.Razor.Language.Syntax.SyntaxNode;

/// <summary>
/// Helper methods for C# and HTML completion ("delegated" completion) that are used both in LSP and cohosting
/// completion handler code.
/// </summary>
internal static class DelegatedCompletionHelper
{
    private static readonly FrozenSet<string> s_designTimeHelpers = new HashSet<string>(StringComparer.Ordinal)
    {
        "__builder",
        "__o",
        "__RazorDirectiveTokenHelpers__",
        "__tagHelperExecutionContext",
        "__tagHelperRunner",
        "__typeHelper",
        "_Imports",
        "BuildRenderTree"
    }.ToFrozenSet();

    /// <summary>
    /// Modifies completion context if needed so that it's acceptable to the delegated language.
    /// </summary>
    /// <param name="context">Original completion context passed to the completion handler</param>
    /// <param name="languageKind">Language of the completion position</param>
    /// <param name="completionTriggerAndCommitCharacters">Per-client set of trigger and commit characters</param>
    /// <param name="updatedContext">Possibly modified completion context</param>.
    /// <returns>
    ///  <see langword="true"/> if the given <see cref="VSInternalCompletionContext"/> is valid or was
    ///  written; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>For example, if we invoke C# completion in Razor via @ character, we will not
    /// want C# to see @ as the trigger character and instead will transform completion context
    /// into "invoked" and "explicit" rather than "typing", without a trigger character</remarks>
    public static bool ValidateOrGetUpdatedContext(
        this VSInternalCompletionContext context,
        RazorLanguageKind languageKind,
        CompletionTriggerAndCommitCharacters completionTriggerAndCommitCharacters,
        [NotNullWhen(true)] out VSInternalCompletionContext? updatedContext)
    {
        Debug.Assert(languageKind != RazorLanguageKind.Razor,
            $"{nameof(ValidateOrGetUpdatedContext)} should be called for delegated completion only");

        if (context.TriggerKind != CompletionTriggerKind.TriggerCharacter
            || context.TriggerCharacter is not { } triggerCharacter)
        {
            // Non-triggered based completion, the existing context is valid.
            updatedContext = context;
            return true;
        }

        if (languageKind == RazorLanguageKind.CSharp
            && CompletionTriggerAndCommitCharacters.CSharpTriggerCharacters.Contains(triggerCharacter))
        {
            // C# trigger character for C# content
            updatedContext = context;
            return true;
        }

        if (languageKind == RazorLanguageKind.Html)
        {
            // For HTML we don't want to delegate to HTML language server is completion is due to a trigger characters that is not
            // HTML trigger character. Doing so causes bad side effects in VSCode HTML client as we will end up with non-matching
            // completion entries
            updatedContext = completionTriggerAndCommitCharacters.HtmlTriggerCharacters.Contains(triggerCharacter) ? context : null;
            return updatedContext is not null;
        }

        // Trigger character not associated with the current language. Transform the context into an invoked context.
        updatedContext = new VSInternalCompletionContext()
        {
            InvokeKind = context.InvokeKind,
            TriggerKind = CompletionTriggerKind.Invoked,
        };

        if (languageKind == RazorLanguageKind.CSharp
            && CompletionTriggerAndCommitCharacters.RazorDelegationTriggerCharacters.Contains(triggerCharacter))
        {
            // The C# language server will not return any completions for the '@' character unless we
            // send the completion request explicitly.
            updatedContext.InvokeKind = VSInternalCompletionInvokeKind.Explicit;
        }

        return true;
    }

    /// <summary>
    /// Modifies a C# completion list to be usable by Razor.
    /// </summary>
    public static void UpdateCSharpCompletionList(
        VSInternalCompletionList completionList,
        RazorCodeDocument codeDocument,
        int hostDocumentIndex,
        Position projectedPosition)
    {
        // First, filter items from the completion list.
        FilterItems(completionList, codeDocument, hostDocumentIndex);

        // Then, update the completion item text edits.
        var hostDocumentPosition = codeDocument.Source.Text.GetPosition(hostDocumentIndex);
        UpdateTextEdits(completionList, hostDocumentPosition, projectedPosition);

        static void FilterItems(VSInternalCompletionList completionList, RazorCodeDocument codeDocument, int hostDocumentIndex)
        {
            var items = completionList.Items;

            // First, filter items from the completion list.
            using var _ = ArrayPool<bool>.Shared.GetPooledArraySpan(items.Length, out var toRemove);
            {
                toRemove.Clear();

                // If the current identifier doesn't start with "__", we remove common design-time helpers *and*
                // any item starting with "__" from the completion list. Otherwise, we only remove just the common
                // design-time helpers.
                var syntaxTree = codeDocument.GetSyntaxTree().AssumeNotNull();
                var sourceText = codeDocument.Source.Text;

                var startsWithDoubleUnderscore =
                    syntaxTree.Root.FindInnermostNode(hostDocumentIndex) is { Span: { Length: >= 2, Start: var start } } &&
                    sourceText[start] == '_' &&
                    sourceText[start + 1] == '_';

                var foundItemToRemove = false;

                // Now, test each completion item and note whether it should be removed or not.
                for (var i = 0; i < items.Length; i++)
                {
                    var item = items[i];

                    // Filter out the C# "using" snippet because we have our own.
                    if (item is { Kind: CompletionItemKind.Snippet, Label: "using" })
                    {
                        toRemove[i] = true;
                        foundItemToRemove = true;
                    }
                    // Filter out Razor design-time helpers and potentially all items starting with "__".
                    else if (s_designTimeHelpers.Contains(item.Label) || !startsWithDoubleUnderscore && item.Label.StartsWith("__"))
                    {
                        toRemove[i] = true;
                        foundItemToRemove = true;
                    }
                }

                if (foundItemToRemove)
                {
                    using var filteredItems = new PooledArrayBuilder<CompletionItem>(items.Length);

                    for (var i = 0; i < items.Length; i++)
                    {
                        if (!toRemove[i])
                        {
                            filteredItems.Add(items[i]);
                        }
                    }

                    completionList.Items = filteredItems.ToArray();
                }
            }
        }

        static void UpdateTextEdits(VSInternalCompletionList completionList, Position hostDocumentPosition, Position projectedPosition)
        {
            foreach (var item in completionList.Items)
            {
                if (item.TextEdit is { } edit)
                {
                    if (edit.TryGetFirst(out var textEdit))
                    {
                        textEdit.Range = TranslateRange(textEdit.Range, hostDocumentPosition, projectedPosition);
                    }
                    else if (edit.TryGetSecond(out var insertReplaceEdit))
                    {
                        insertReplaceEdit.Insert = TranslateRange(insertReplaceEdit.Insert, hostDocumentPosition, projectedPosition);
                        insertReplaceEdit.Replace = TranslateRange(insertReplaceEdit.Replace, hostDocumentPosition, projectedPosition);
                    }
                }
                else if (item.AdditionalTextEdits is not null)
                {
                    // Additional text edits should typically only be provided at resolve time. We don't support them in the normal completion flow.
                    item.AdditionalTextEdits = null;
                }
            }

            // Ensure that we update the item defaults as well.
            if (completionList.ItemDefaults?.EditRange is { } editRange)
            {
                if (editRange.TryGetFirst(out var range))
                {
                    completionList.ItemDefaults.EditRange = TranslateRange(range, hostDocumentPosition, projectedPosition);
                }
                else if (editRange.TryGetSecond(out var insertReplaceRange))
                {
                    insertReplaceRange.Insert = TranslateRange(insertReplaceRange.Insert, hostDocumentPosition, projectedPosition);
                    insertReplaceRange.Replace = TranslateRange(insertReplaceRange.Replace, hostDocumentPosition, projectedPosition);
                }
            }

            static LspRange TranslateRange(LspRange textEditRange, Position hostDocumentPosition, Position projectedPosition)
            {
                var offset = projectedPosition.Character - hostDocumentPosition.Character;

                var translatedStartPosition = TranslatePosition(offset, hostDocumentPosition, textEditRange.Start);
                var translatedEndPosition = TranslatePosition(offset, hostDocumentPosition, textEditRange.End);

                return VsLspFactory.CreateRange(translatedStartPosition, translatedEndPosition);

                static Position TranslatePosition(int offset, Position hostDocumentPosition, Position editPosition)
                {
                    var translatedCharacter = editPosition.Character - offset;

                    // Note: If this completion handler ever expands to deal with multi-line TextEdits, this logic will likely need to change since
                    // it assumes we're only dealing with single-line TextEdits.
                    return VsLspFactory.CreatePosition(hostDocumentPosition.Line, translatedCharacter);
                }
            }
        }
    }

    /// <summary>
    /// Modifies an HTML completion list to be usable by Razor.
    /// </summary>
    public static void UpdateHtmlCompletionList(VSInternalCompletionList completionList, RazorCompletionOptions completionOptions)
    {
        if (completionOptions.CommitElementsWithSpace)
        {
            return;
        }

        // Filter default commit characters.
        string[]? defaultCommitChars = null;

        if (completionList.CommitCharacters is { } commitCharacters)
        {
            if (commitCharacters.TryGetFirst(out var commitChars))
            {
                defaultCommitChars = FilterCommitChars(commitChars);
            }
            else if (commitCharacters.TryGetSecond(out var vsCommitChars))
            {
                defaultCommitChars = FilterVSCommitChars(vsCommitChars);
            }
        }

        using var itemCommitChars = new PooledArrayBuilder<string>();

        foreach (var item in completionList.Items)
        {
            if (item.Kind == CompletionItemKind.Element)
            {
                if (item.CommitCharacters is null)
                {
                    if (defaultCommitChars is not null)
                    {
                        // This item wants to use the default commit characters, so change it to our updated version of them, without the space
                        item.CommitCharacters = defaultCommitChars;
                    }
                }
                else
                {
                    // This item has its own commit characters, so just remove spaces
                    itemCommitChars.Clear();

                    foreach (var commitChar in item.CommitCharacters)
                    {
                        if (commitChar != " ")
                        {
                            itemCommitChars.Add(commitChar);
                        }
                    }

                    item.CommitCharacters = itemCommitChars.ToArray();
                }
            }
        }

        static string[]? FilterCommitChars(string[] commitChars)
        {
            using var builder = new PooledArrayBuilder<string>(commitChars.Length);

            foreach (var commitChar in commitChars)
            {
                if (commitChar != " ")
                {
                    builder.Add(commitChar);
                }
            }

            // If the default commit characters didn't include " " already, then we set our list to null to avoid over-specifying commit characters
            return builder.Count != commitChars.Length
                ? builder.ToArray()
                : null;
        }

        static string[]? FilterVSCommitChars(VSInternalCommitCharacter[] vsCommitChars)
        {
            using var builder = new PooledArrayBuilder<string>(vsCommitChars.Length);

            foreach (var vsCommitChar in vsCommitChars)
            {
                if (vsCommitChar.Character != " ")
                {
                    builder.Add(vsCommitChar.Character);
                }
            }

            // If the default commit characters didn't include " " already, then we set our list to null to avoid over-specifying commit characters
            return builder.Count != vsCommitChars.Length
                ? builder.ToArray()
                : null;
        }
    }

    /// <summary>
    /// Returns possibly update document position info and provisional edit (if any)
    /// </summary>
    /// <remarks>
    /// Provisional completion happens when typing something like @DateTime. in a document.
    /// In this case the '.' initially is parsed as belonging to HTML. However, we want to
    /// show C# member completion in this case, so we want to make a temporary change to the
    /// generated C# code so that '.' ends up in C#. This method will check for such case,
    /// and provisional completion case is detected, will update position language from HTML
    /// to C# and will return a temporary edit that should be made to the generated document
    /// in order to add the '.' to the generated C# contents.
    /// </remarks>
    public static bool TryGetProvisionalCompletionInfo(
        this VSInternalCompletionContext context,
        DocumentPositionInfo positionInfo,
        RazorCodeDocument codeDocument,
        IDocumentMappingService documentMappingService,
        out CompletionPositionInfo completionInfo)
    {
        if (positionInfo.Position.Character == 0 ||
            positionInfo.LanguageKind != RazorLanguageKind.Html ||
            context.TriggerKind != CompletionTriggerKind.TriggerCharacter ||
            context.TriggerCharacter != ".")
        {
            // Invalid position info or completion context
            completionInfo = default;
            return false;
        }

        var previousPositionInfo = documentMappingService.GetPositionInfo(codeDocument, positionInfo.HostDocumentIndex - 1);

        if (previousPositionInfo.LanguageKind != RazorLanguageKind.CSharp)
        {
            completionInfo = default;
            return false;
        }

        var previousPosition = previousPositionInfo.Position;

        // Edit the CSharp projected document to contain a '.'. This allows C# completion to provide valid
        // completion items for moments when a user has typed a '.' that's typically interpreted as Html.
        var addProvisionalDot = VsLspFactory.CreateTextEdit(previousPosition, ".");

        var provisionalPositionInfo = new DocumentPositionInfo(
            RazorLanguageKind.CSharp,
            VsLspFactory.CreatePosition(
                previousPosition.Line,
                previousPosition.Character + 1),
            previousPositionInfo.HostDocumentIndex + 1);

        completionInfo = new CompletionPositionInfo(addProvisionalDot, provisionalPositionInfo, ShouldIncludeDelegationSnippets: false);
        return true;
    }

    public static bool ShouldIncludeSnippets(RazorCodeDocument codeDocument, int absoluteIndex)
    {
        var tree = codeDocument.GetSyntaxTree().AssumeNotNull();

        var token = tree.Root.FindToken(absoluteIndex, includeWhitespace: false);
        if (token.Kind == SyntaxKind.EndOfFile &&
            token.GetPreviousToken()?.Parent is { } parent &&
            parent.FirstAncestorOrSelf<SyntaxNode>(RazorSyntaxFacts.IsAnyStartTag) is not null)
        {
            // If we're at the end of the file, we check if the previous token is part of a start tag, because the parser
            // treats whitespace at the end different. eg with "<$$[EOF]" or "<div $$", the EndOfFile won't be seen as being
            // in the tag, so without this special casing snippets would be shown.
            return false;
        }

        var node = token.Parent;
        var startOrEndTag = node?.FirstAncestorOrSelf<SyntaxNode>(n => RazorSyntaxFacts.IsAnyStartTag(n) || RazorSyntaxFacts.IsAnyEndTag(n));

        if (startOrEndTag is null)
        {
            return token.Kind is not (SyntaxKind.OpenAngle or SyntaxKind.CloseAngle);
        }

        if (startOrEndTag.Span.Start == absoluteIndex)
        {
            // We're at the start of the tag, we should include snippets. This is the case for things like $$<div></div> or <div>$$</div>, since the
            // index is right associative to the token when using FindToken.
            return true;
        }

        return !startOrEndTag.Span.Contains(absoluteIndex);
    }
}
