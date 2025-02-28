// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.AspNetCore.Razor.Language.Components;

#if !NET
using Microsoft.AspNetCore.Razor.Utilities;
#endif

namespace Microsoft.AspNetCore.Razor.Language;

internal static class RazorFileKinds
{
    private static ReadOnlySpan<char> RazorExtension => ".razor".AsSpan();
    private static ReadOnlySpan<char> CSHtmlExtension => ".cshtml".AsSpan();

    internal const RazorFileKind MaxValue = RazorFileKind.ComponentImport;

    /// <summary>
    ///  Returns <see langword="true"/> if the file kind is a component or component import.
    /// </summary>
    public static bool IsComponent(this RazorFileKind fileKind)
        => fileKind is RazorFileKind.Component or RazorFileKind.ComponentImport;

    /// <summary>
    ///  Returns <see langword="true"/> if the file kind is a component import.
    /// </summary>
    public static bool IsComponentImport(this RazorFileKind fileKind)
        => fileKind is RazorFileKind.ComponentImport;

    /// <summary>
    ///  Returns <see langword="true"/> if the file kind is legacy.
    /// </summary>
    public static bool IsLegacy(this RazorFileKind fileKind)
        => fileKind is RazorFileKind.Legacy;

    public static RazorFileKind GetComponentFileKindFromFilePath(string filePath)
    {
        ArgHelper.ThrowIfNull(filePath);

        return GetComponentFileKindFromFilePathCore(filePath);
    }

    public static RazorFileKind GetFileKindFromFilePath(string filePath)
    {
        ArgHelper.ThrowIfNull(filePath);

        return GetExtension(filePath).Equals(RazorExtension, StringComparison.OrdinalIgnoreCase)
            ? GetComponentFileKindFromFilePathCore(filePath)
            : RazorFileKind.Legacy;
    }

    public static bool TryGetFileKind(string? filePath, out RazorFileKind fileKind)
    {
        var extension = GetExtension(filePath);

        if (extension.IsEmpty)
        {
            fileKind = RazorFileKind.None;
            return false;
        }

        if (extension.Equals(RazorExtension, StringComparison.OrdinalIgnoreCase))
        {
            fileKind = GetComponentFileKindFromFilePathCore(filePath);
            return true;
        }
        else if (extension.Equals(CSHtmlExtension, StringComparison.OrdinalIgnoreCase))
        {
            fileKind = RazorFileKind.Legacy;
            return true;
        }

        fileKind = RazorFileKind.None;
        return false;
    }

    private static RazorFileKind GetComponentFileKindFromFilePathCore(string? filePath)
    {
        // We need to check if the file is a component import or a component. To do this,
        // we check to see if the file name is "_Imports.razor". Note that Razor has always
        // performed this as a case-sensitive comparison.
        return StringComparer.Ordinal.Equals(Path.GetFileName(filePath), ComponentMetadata.ImportsFileName)
            ? RazorFileKind.ComponentImport
            : RazorFileKind.Component;
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
