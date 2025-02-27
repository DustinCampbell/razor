// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Language;

internal static class RazorFileKinds
{
    internal const RazorFileKind MaxValue = RazorFileKind.ComponentImport;

    public static bool IsComponent(RazorFileKind fileKind)
        => fileKind is RazorFileKind.Component or RazorFileKind.ComponentImport;

    public static bool IsComponentImport(RazorFileKind fileKind)
        => fileKind is RazorFileKind.ComponentImport;

    public static bool IsLegacy(RazorFileKind fileKind)
        => fileKind is RazorFileKind.Legacy;

    public static RazorFileKind GetComponentFileKindFromFilePath(string filePath)
    {
        ArgHelper.ThrowIfNull(filePath);

        return string.Equals(ComponentMetadata.ImportsFileName, Path.GetFileName(filePath), StringComparison.Ordinal)
            ? RazorFileKind.ComponentImport
            : RazorFileKind.Component;
    }

    public static RazorFileKind GetFileKindFromFilePath(string? filePath)
    {
        ArgHelper.ThrowIfNull(filePath);

        if (string.Equals(ComponentMetadata.ImportsFileName, Path.GetFileName(filePath), StringComparison.Ordinal))
        {
            return RazorFileKind.ComponentImport;
        }
        else if (string.Equals(".razor", Path.GetExtension(filePath), StringComparison.OrdinalIgnoreCase))
        {
            return RazorFileKind.Component;
        }
        else
        {
            return RazorFileKind.Legacy;
        }
    }
}
