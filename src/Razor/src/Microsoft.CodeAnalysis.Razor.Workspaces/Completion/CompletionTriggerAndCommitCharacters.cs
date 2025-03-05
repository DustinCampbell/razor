// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal class CompletionTriggerAndCommitCharacters
{
    /// <summary>
    /// Can trigger both Razor and Delegation completion (e.g. "@" triggers Razor directives and C#).
    /// </summary>
    private const string RazorDelegationTriggerCharacter = "@";

    private static readonly string[] s_vsHtmlTriggerCharacters = [":", "@", "#", ".", "!", "*", ",", "(", "[", "-", "<", "&", "\\", "/", "'", "\"", "=", ":", " ", "`"];
    private static readonly string[] s_vsCodeHtmlTriggerCharacters = ["@", "#", ".", "!", ",", "-", "<"];
    private static readonly string[] s_csharpTriggerCharacters = [" ", "(", "=", "#", ".", "<", "[", "{", "\"", "/", ":", "~"];
    private static readonly string[] s_razorTriggerCharacters = ["@", "<", ":", " "];

    private readonly HashSet<string> _csharpTriggerCharacters;
    private readonly HashSet<string> _htmlTriggerCharacters;
    private readonly HashSet<string> _razorTriggerCharacters;
    private readonly HashSet<string> _allDelegationTriggerCharacters;
    private readonly string[] _allTriggerCharacters;

    public CompletionTriggerAndCommitCharacters(LanguageServerFeatureOptions options)
    {
        _csharpTriggerCharacters = [.. s_csharpTriggerCharacters];

        var htmlTriggerCharacters = options.UseVsCodeCompletionTriggerCharacters
            ? s_vsCodeHtmlTriggerCharacters
            : s_vsHtmlTriggerCharacters;

        _htmlTriggerCharacters = [.. htmlTriggerCharacters];
        _razorTriggerCharacters = [.. s_razorTriggerCharacters];

        var allDelegationTriggerCharacters = new HashSet<string>
        {
            RazorDelegationTriggerCharacter
        };

        allDelegationTriggerCharacters.UnionWith(s_csharpTriggerCharacters);
        allDelegationTriggerCharacters.UnionWith(htmlTriggerCharacters);

        _allDelegationTriggerCharacters = allDelegationTriggerCharacters;

        var allTriggerCharacters = new HashSet<string>(allDelegationTriggerCharacters);
        allTriggerCharacters.UnionWith(s_razorTriggerCharacters);

        _allTriggerCharacters = [.. allTriggerCharacters];
    }

    /// <summary>
    /// This is the intersection of C# and HTML commit characters.
    /// </summary>
    // We need to specify it so that platform can correctly calculate ApplicableToSpan in
    // https://devdiv.visualstudio.com/DevDiv/_git/VSLanguageServerClient?path=/src/product/RemoteLanguage/Impl/Features/Completion/AsyncCompletionSource.cs&version=GBdevelop&line=855&lineEnd=855&lineStartColumn=9&lineEndColumn=49&lineStyle=plain&_a=contents
    // This is needed to fix https://github.com/dotnet/razor/issues/10787 in particular
    public string[] GetAllCommitCharacters()
        => [" ", ">", ";", "="];

    public string[] GetAllTriggerCharacters()
        => [.. _allTriggerCharacters];

    public bool IsCSharpTriggerCharacter(string character)
        => _csharpTriggerCharacters.Contains(character);

    public bool IsHtmlTriggerCharacter(string character)
        => _htmlTriggerCharacters.Contains(character);

    public bool IsRazorDelegationTriggerCharacter(string character)
        => character == RazorDelegationTriggerCharacter;

    public bool IsValidCSharpTrigger(CompletionContext context)
        => IsValidTrigger(context, _csharpTriggerCharacters);

    public bool IsValidDelegationTrigger(CompletionContext context)
        => IsValidTrigger(context, _allDelegationTriggerCharacters);

    public bool IsValidHtmlTrigger(CompletionContext context)
        => IsValidTrigger(context, _htmlTriggerCharacters);

    public bool IsValidRazorTrigger(CompletionContext context)
        => IsValidTrigger(context, _razorTriggerCharacters);

    private static bool IsValidTrigger(CompletionContext context, HashSet<string> triggerCharacters)
        => context.TriggerKind != CompletionTriggerKind.TriggerCharacter ||
           context.TriggerCharacter is null ||
           triggerCharacters.Contains(context.TriggerCharacter);
}
