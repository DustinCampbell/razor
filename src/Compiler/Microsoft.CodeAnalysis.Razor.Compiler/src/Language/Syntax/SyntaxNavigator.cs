// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class SyntaxNavigator
{
    private static Func<SyntaxToken, bool> GetPredicateFunction(bool includeZeroWidth)
    {
        return includeZeroWidth ? SyntaxToken.Any : SyntaxToken.NonZeroWidth;
    }

    private static Func<OldSyntaxToken, bool> GetOldPredicateFunction(bool includeZeroWidth)
    {
        return includeZeroWidth ? OldSyntaxToken.Any : OldSyntaxToken.NonZeroWidth;
    }

    private static bool Matches(Func<SyntaxToken, bool>? predicate, SyntaxToken token)
    {
        return predicate == null || ReferenceEquals(predicate, SyntaxToken.Any) || predicate(token);
    }

    private static bool Matches(Func<OldSyntaxToken, bool>? predicate, OldSyntaxToken token)
    {
        return predicate == null || ReferenceEquals(predicate, OldSyntaxToken.Any) || predicate(token);
    }

    internal static SyntaxToken GetFirstToken(SyntaxNode current, bool includeZeroWidth)
    {
        return GetFirstToken(current, GetPredicateFunction(includeZeroWidth));
    }

    internal static SyntaxToken GetLastToken(SyntaxNode current, bool includeZeroWidth)
    {
        return GetLastToken(current, GetPredicateFunction(includeZeroWidth));
    }

    internal static SyntaxToken GetPreviousToken(SyntaxToken current, bool includeZeroWidth)
    {
        return GetPreviousToken(current, GetPredicateFunction(includeZeroWidth));
    }

    internal static SyntaxToken GetNextToken(SyntaxToken current, bool includeZeroWidth)
    {
        return GetNextToken(current, GetPredicateFunction(includeZeroWidth));
    }

    internal static OldSyntaxToken? GetFirstOldToken(SyntaxNode current, bool includeZeroWidth)
    {
        return GetFirstOldToken(current, GetOldPredicateFunction(includeZeroWidth));
    }

    internal static OldSyntaxToken? GetLastOldToken(SyntaxNode current, bool includeZeroWidth)
    {
        return GetLastOldToken(current, GetOldPredicateFunction(includeZeroWidth));
    }

    internal static OldSyntaxToken? GetPreviousOldToken(OldSyntaxToken current, bool includeZeroWidth)
    {
        return GetPreviousOldToken(current, GetOldPredicateFunction(includeZeroWidth));
    }

    internal static OldSyntaxToken? GetNextOldToken(OldSyntaxToken current, bool includeZeroWidth)
    {
        return GetNextOldToken(current, GetOldPredicateFunction(includeZeroWidth));
    }

    internal static SyntaxToken GetFirstToken(SyntaxNode current, Func<SyntaxToken, bool>? predicate)
    {
        using var stack = new PooledArrayBuilder<ChildSyntaxList.Enumerator>();
        stack.Push(current.ChildNodesAndOldTokens().GetEnumerator());

        while (stack.Count > 0)
        {
            var en = stack.Pop();
            if (en.MoveNext())
            {
                var child = en.Current;

                if (child.IsToken)
                {
                    var token = GetFirstToken(child.AsToken(), predicate);
                    if (token.Kind != SyntaxKind.None)
                    {
                        return token;
                    }
                }

                // push this enumerator back, not done yet
                stack.Push(en);

                if (!child.IsToken)
                {
                    stack.Push(child.ChildNodesAndOldTokens().GetEnumerator());
                }
            }
        }

        return default;
    }

    private static SyntaxToken GetFirstToken(SyntaxToken token, Func<SyntaxToken, bool>? predicate)
    {
        if (Matches(predicate, token))
        {
            return token;
        }

        return default;
    }

    internal static SyntaxToken GetLastToken(SyntaxNode current, Func<SyntaxToken, bool> predicate)
    {
        using var stack = new PooledArrayBuilder<ChildSyntaxList.Reversed.Enumerator>();
        stack.Push(current.ChildNodesAndOldTokens().Reverse().GetEnumerator());

        while (stack.Count > 0)
        {
            var en = stack.Pop();

            if (en.MoveNext())
            {
                var child = en.Current;

                if (child.IsToken)
                {
                    var token = GetLastToken(child.AsToken(), predicate);
                    if (token.Kind != SyntaxKind.None)
                    {
                        return token;
                    }
                }

                // push this enumerator back, not done yet
                stack.Push(en);

                if (!child.IsToken)
                {
                    stack.Push(child.ChildNodesAndOldTokens().Reverse().GetEnumerator());
                }
            }
        }

        return default;
    }

    private static SyntaxToken GetLastToken(SyntaxToken token, Func<SyntaxToken, bool> predicate)
    {
        if (Matches(predicate, token))
        {
            return token;
        }

        return default;
    }

    internal static SyntaxToken GetNextToken(SyntaxToken current, Func<SyntaxToken, bool>? predicate)
    {
        if (current.Parent != null)
        {
            // walk forward in parent's child list until we find ourself
            // and then return the next token
            var returnNext = false;
            foreach (var child in current.Parent.ChildNodesAndOldTokens())
            {
                if (returnNext)
                {
                    if (child.IsToken)
                    {
                        var token = GetFirstToken(child.AsToken(), predicate);
                        if (token.Kind != SyntaxKind.None)
                        {
                            return token;
                        }
                    }
                    else
                    {
                        var token = GetFirstToken(child, predicate);
                        if (token.Kind != SyntaxKind.None)
                        {
                            return token;
                        }
                    }
                }
                else if (child.IsToken && child.AsToken() == current)
                {
                    returnNext = true;
                }
            }

            // otherwise get next token from the parent's parent, and so on
            return GetNextToken(current.Parent, predicate);
        }

        return default;
    }

    internal static SyntaxToken GetNextToken(SyntaxNode node, Func<SyntaxToken, bool>? predicate)
    {
        while (node.Parent != null)
        {
            // walk forward in parent's child list until we find ourselves and then return the
            // next token
            var returnNext = false;
            foreach (var child in node.Parent.ChildNodesAndOldTokens())
            {
                if (returnNext)
                {
                    if (child.IsToken)
                    {
                        var token = GetFirstToken(child.AsToken(), predicate);
                        if (token.Kind != SyntaxKind.None)
                        {
                            return token;
                        }
                    }
                    else
                    {
                        var token = GetFirstToken(child, predicate);
                        if (token.Kind != SyntaxKind.None)
                        {
                            return token;
                        }
                    }
                }
                else if (child == node)
                {
                    returnNext = true;
                }
            }

            // didn't find the next token in my parent's children, look up the tree
            node = node.Parent;
        }

        return default;
    }

    internal static SyntaxToken GetPreviousToken(SyntaxToken current, Func<SyntaxToken, bool> predicate)
    {
        if (current.Parent != null)
        {
            // walk backward in parent's child list until we find ourself
            // and then return the next token
            var returnPrevious = false;
            foreach (var child in current.Parent.ChildNodesAndOldTokens().Reverse())
            {
                if (returnPrevious)
                {
                    if (child.IsToken)
                    {
                        var token = GetLastToken(child.AsToken(), predicate);
                        if (token.Kind != SyntaxKind.None)
                        {
                            return token;
                        }
                    }
                    else
                    {
                        var token = GetLastToken(child, predicate);
                        if (token.Kind != SyntaxKind.None)
                        {
                            return token;
                        }
                    }
                }
                else if (child.IsToken && child.AsToken() == current)
                {
                    returnPrevious = true;
                }
            }

            // otherwise get next token from the parent's parent, and so on
            return GetPreviousToken(current.Parent, predicate);
        }

        return default;
    }

    internal static SyntaxToken GetPreviousToken(SyntaxNode node, Func<SyntaxToken, bool> predicate)
    {
        while (node.Parent != null)
        {
            // walk backward in parent's child list until we find ourselves and then return the
            // previous token
            var returnPrevious = false;
            foreach (var child in node.Parent.ChildNodesAndOldTokens().Reverse())
            {
                if (returnPrevious)
                {
                    if (child.IsToken)
                    {
                        var token = GetLastToken(child.AsToken(), predicate);
                        if (token.Kind != SyntaxKind.None)
                        {
                            return token;
                        }
                    }
                    else
                    {
                        var token = GetLastToken(child, predicate);
                        if (token.Kind != SyntaxKind.None)
                        {
                            return token;
                        }
                    }
                }
                else if (child == node)
                {
                    returnPrevious = true;
                }
            }

            // didn't find the previous token in my parent's children, look up the tree
            node = node.Parent;
        }

        return default;
    }

    internal static OldSyntaxToken? GetFirstOldToken(SyntaxNode current, Func<OldSyntaxToken, bool>? predicate)
    {
        using var stack = new PooledArrayBuilder<ChildSyntaxList.Enumerator>();
        stack.Push(current.ChildNodesAndOldTokens().GetEnumerator());

        while (stack.Count > 0)
        {
            var en = stack.Pop();
            if (en.MoveNext())
            {
                var child = en.Current;

                if (child.IsToken)
                {
                    var token = GetFirstOldToken((OldSyntaxToken)child, predicate);
                    if (token != null)
                    {
                        return token;
                    }
                }

                // push this enumerator back, not done yet
                stack.Push(en);

                if (!child.IsToken)
                {
                    stack.Push(child.ChildNodesAndOldTokens().GetEnumerator());
                }
            }
        }

        return null;
    }

    internal static OldSyntaxToken? GetLastOldToken(SyntaxNode current, Func<OldSyntaxToken, bool> predicate)
    {
        using var stack = new PooledArrayBuilder<ChildSyntaxList.Reversed.Enumerator>();
        stack.Push(current.ChildNodesAndOldTokens().Reverse().GetEnumerator());

        while (stack.Count > 0)
        {
            var en = stack.Pop();

            if (en.MoveNext())
            {
                var child = en.Current;

                if (child.IsToken)
                {
                    var token = GetLastOldToken((OldSyntaxToken)child, predicate);
                    if (token != null)
                    {
                        return token;
                    }
                }

                // push this enumerator back, not done yet
                stack.Push(en);

                if (!child.IsToken)
                {
                    stack.Push(child.ChildNodesAndOldTokens().Reverse().GetEnumerator());
                }
            }
        }

        return null;
    }

    private static OldSyntaxToken? GetFirstOldToken(OldSyntaxToken token, Func<OldSyntaxToken, bool>? predicate)
    {
        if (Matches(predicate, token))
        {
            return token;
        }

        return null;
    }

    private static OldSyntaxToken? GetLastOldToken(OldSyntaxToken token, Func<OldSyntaxToken, bool> predicate)
    {
        if (Matches(predicate, token))
        {
            return token;
        }

        return null;
    }

    internal static OldSyntaxToken? GetNextOldToken(SyntaxNode node, Func<OldSyntaxToken, bool>? predicate)
    {
        while (node.Parent != null)
        {
            // walk forward in parent's child list until we find ourselves and then return the
            // next token
            var returnNext = false;
            foreach (var child in node.Parent.ChildNodesAndOldTokens())
            {
                if (returnNext)
                {
                    if (child.IsToken)
                    {
                        var token = GetFirstOldToken((OldSyntaxToken)child, predicate);
                        if (token != null)
                        {
                            return token;
                        }
                    }
                    else
                    {
                        var token = GetFirstOldToken(child, predicate);
                        if (token != null)
                        {
                            return token;
                        }
                    }
                }
                else if (child == node)
                {
                    returnNext = true;
                }
            }

            // didn't find the next token in my parent's children, look up the tree
            node = node.Parent;
        }

        return null;
    }

    internal static OldSyntaxToken? GetPreviousOldToken(
        SyntaxNode node,
        Func<OldSyntaxToken, bool> predicate)
    {
        while (node.Parent != null)
        {
            // walk forward in parent's child list until we find ourselves and then return the
            // previous token
            var returnPrevious = false;
            foreach (var child in node.Parent.ChildNodesAndOldTokens().Reverse())
            {
                if (returnPrevious)
                {
                    if (child.IsToken)
                    {
                        var token = GetLastOldToken((OldSyntaxToken)child, predicate);
                        if (token != null)
                        {
                            return token;
                        }
                    }
                    else
                    {
                        var token = GetLastOldToken(child, predicate);
                        if (token != null)
                        {
                            return token;
                        }
                    }
                }
                else if (child == node)
                {
                    returnPrevious = true;
                }
            }

            // didn't find the previous token in my parent's children, look up the tree
            node = node.Parent;
        }

        return null;
    }

    internal static OldSyntaxToken? GetNextOldToken(OldSyntaxToken current, Func<OldSyntaxToken, bool>? predicate)
    {
        if (current.Parent != null)
        {
            // walk forward in parent's child list until we find ourself
            // and then return the next token
            var returnNext = false;
            foreach (var child in current.Parent.ChildNodesAndOldTokens())
            {
                if (returnNext)
                {
                    if (child.IsToken)
                    {
                        var token = GetFirstOldToken((OldSyntaxToken)child, predicate);
                        if (token != null)
                        {
                            return token;
                        }
                    }
                    else
                    {
                        var token = GetFirstOldToken(child, predicate);
                        if (token != null)
                        {
                            return token;
                        }
                    }
                }
                else if (child == current)
                {
                    returnNext = true;
                }
            }

            // otherwise get next token from the parent's parent, and so on
            return GetNextOldToken(current.Parent, predicate);
        }

        return null;
    }

    internal static OldSyntaxToken? GetPreviousOldToken(OldSyntaxToken current, Func<OldSyntaxToken, bool> predicate)
    {
        if (current.Parent != null)
        {
            // walk forward in parent's child list until we find ourself
            // and then return the next token
            var returnPrevious = false;
            foreach (var child in current.Parent.ChildNodesAndOldTokens().Reverse())
            {
                if (returnPrevious)
                {
                    if (child.IsToken)
                    {
                        var token = GetLastOldToken((OldSyntaxToken)child, predicate);
                        if (token != null)
                        {
                            return token;
                        }
                    }
                    else
                    {
                        var token = GetLastOldToken(child, predicate);
                        if (token != null)
                        {
                            return token;
                        }
                    }
                }
                else if (child == current)
                {
                    returnPrevious = true;
                }
            }

            // otherwise get next token from the parent's parent, and so on
            return GetPreviousOldToken(current.Parent, predicate);
        }

        return null;
    }
}
