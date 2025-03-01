// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.AspNetCore.Razor.Language.Components;

#if !NET
using Microsoft.AspNetCore.Razor.Utilities;
#endif

namespace Microsoft.AspNetCore.Razor.Language;

internal static class SourceCodeFileKinds
{
    private static ReadOnlySpan<char> RazorExtension => ".razor".AsSpan();
    private static ReadOnlySpan<char> CSHtmlExtension => ".cshtml".AsSpan();

    internal const RazorSourceCodeKind MaxValue = RazorSourceCodeKind.ComponentImport;

    /// <summary>
    ///  Returns <see langword="true"/> if the file kind is a component or component import.
    /// </summary>
    public static bool IsComponent(this RazorSourceCodeKind sourceCodeKind)
        => sourceCodeKind is RazorSourceCodeKind.Component or RazorSourceCodeKind.ComponentImport;

    /// <summary>
    ///  Returns <see langword="true"/> if the file kind is a component import.
    /// </summary>
    public static bool IsComponentImport(this RazorSourceCodeKind sourceCodeKind)
        => sourceCodeKind is RazorSourceCodeKind.ComponentImport;

    /// <summary>
    ///  Returns <see langword="true"/> if the file kind is legacy.
    /// </summary>
    public static bool IsLegacy(this RazorSourceCodeKind sourceCodeKind)
        => sourceCodeKind is RazorSourceCodeKind.Legacy;

    public static RazorSourceCodeKind GetComponentFileKindFromFilePath(string filePath)
    {
        ArgHelper.ThrowIfNull(filePath);

        return GetComponentFileKindFromFilePathCore(filePath);
    }

    public static bool TryGetSourceCodeKind(string? filePath, out RazorSourceCodeKind sourceCodeKind)
    {
        var extension = GetExtension(filePath);

        if (extension.IsEmpty)
        {
            sourceCodeKind = RazorSourceCodeKind.None;
            return false;
        }

        if (extension.Equals(RazorExtension, StringComparison.OrdinalIgnoreCase))
        {
            sourceCodeKind = GetComponentFileKindFromFilePathCore(filePath);
            return true;
        }
        else if (extension.Equals(CSHtmlExtension, StringComparison.OrdinalIgnoreCase))
        {
            sourceCodeKind = RazorSourceCodeKind.Legacy;
            return true;
        }

        sourceCodeKind = RazorSourceCodeKind.None;
        return false;
    }

    private static RazorSourceCodeKind GetComponentFileKindFromFilePathCore(string? filePath)
    {
        // We need to check if the file is a component import or a component. To do this,
        // we check to see if the file name is "_Imports.razor". Note that Razor has always
        // performed this as a case-sensitive comparison.
        return StringComparer.Ordinal.Equals(Path.GetFileName(filePath), ComponentMetadata.ImportsFileName)
            ? RazorSourceCodeKind.ComponentImport
            : RazorSourceCodeKind.Component;
    }

    private static ReadOnlySpan<char> GetExtension(string? filePath)
    {
        var path = filePath.AsSpan();

#if NET
        return Path.GetExtension(path);
#else
        // The following code is derived from https://github.com/dotnet/runtime/blob/bb5f473337719c4c406d63ffc9e845196b69caf9/src/libraries/System.Private.CoreLib/src/System/IO/Path.cs#L189-L213

        var length = path.Length;

        for (var i = length - 1; i >= 0; i--)
        {
            var ch = path[i];

            if (ch == '.')
            {
                return i != length - 1
                    ? path[i..length]
                    : [];
            }

            if (IsDirectorySeparator(ch))
            {
                break;
            }
        }

        return [];

        static bool IsDirectorySeparator(char ch)
        {
            return ch == Path.DirectorySeparatorChar ||
                  (PlatformInformation.IsWindows && ch == Path.AltDirectorySeparatorChar);
        }
#endif
    }
}
