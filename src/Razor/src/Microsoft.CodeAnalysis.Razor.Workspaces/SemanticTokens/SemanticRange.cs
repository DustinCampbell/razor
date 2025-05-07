// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.SemanticTokens;

internal readonly struct SemanticRange(
    int kind,
    int startLine,
    int startCharacter,
    int endLine,
    int endCharacter,
    int modifier,
    bool fromRazor) : IComparable<SemanticRange>
{
    public int Kind { get; } = kind;

    public int StartLine { get; } = startLine;
    public int EndLine { get; } = endLine;
    public int StartCharacter { get; } = startCharacter;
    public int EndCharacter { get; } = endCharacter;

    public int Modifier { get; } = modifier;

    /// <summary>
    /// If we produce a token, and a delegated server produces a token, we want to prefer ours, so we use this flag to help our
    /// sort algorithm, that way we can avoid the perf hit of actually finding duplicates, and just take the first instance that
    /// covers a range.
    /// </summary>
    public bool FromRazor { get; } = fromRazor;

    public SemanticRange(int kind, LinePositionSpan span, int modifier, bool fromRazor)
        : this(kind, span.Start.Line, span.Start.Character, span.End.Line, span.End.Character, modifier, fromRazor)
    {
    }

    public LinePositionSpan AsLinePositionSpan()
        => new(new(StartLine, StartCharacter), new(EndLine, EndCharacter));

    public int CompareTo(SemanticRange other)
    {
        var result = StartLine.CompareTo(other.StartLine);
        if (result != 0)
        {
            return result;
        }

        result = StartCharacter.CompareTo(other.StartCharacter);
        if (result != 0)
        {
            return result;
        }

        result = EndLine.CompareTo(other.EndLine);
        if (result != 0)
        {
            return result;
        }

        result = EndCharacter.CompareTo(other.EndCharacter);
        if (result != 0)
        {
            return result;
        }

        // If we have ranges that are the same, we want a Razor produced token to win over a non-Razor produced token
        if (FromRazor && !other.FromRazor)
        {
            return -1;
        }
        else if (other.FromRazor && !FromRazor)
        {
            return 1;
        }

        return 0;
    }

    public override string ToString()
        => $"[Kind: {Kind}, StartLine: {StartLine}, StartCharacter: {StartCharacter}, EndLine: {EndLine}, EndCharacter: {EndCharacter}]";
}
