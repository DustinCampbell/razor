// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language;

internal static class RazorSourceDocumentExtensions
{
    // The default scheme for identifiers matches MVC's view engine paths:
    // 1. Normalize backslash to forward-slash
    // 2. Always include leading slash
    // 3. Always include file name and extensions
    public static string? GetIdentifier(this RazorSourceDocument sourceDocument)
    {
        var identifier = sourceDocument.RelativePath;

        if (identifier.IsNullOrEmpty())
        {
            return null;
        }

        if (identifier.StartsWith('/') && !identifier.Contains('\\'))
        {
            // If the identifier starts with a slash and does not contain any backslashes,
            // we assume it is already in the correct format and return it as is.
            return identifier;
        }

        var addLeadingSlash = false;
        var length = identifier.Length;

        // If the identifier does not start with a slash or backslash, we need to add a leading slash,
        // so increase the length by 1.
        if (identifier is [not ('/' or '\\'), ..])
        {
            addLeadingSlash = true;
            length++;
        }

        return string.Create(length, (addLeadingSlash, identifier), static (span, state) =>
        {
            var (addLeadingSlash, identifier) = state;

            Debug.Assert(
                span.Length == identifier.Length + (addLeadingSlash ? 1 : 0),
                "The span length should match the expected length.");

            if (addLeadingSlash)
            {
                span[0] = '/';
                span = span[1..];
            }

            for (var i = 0; i < identifier.Length; i++)
            {
                var c = identifier[i];
                span[i] = c == '\\' ? '/' : c;
            }
        });
    }
}
