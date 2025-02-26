// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Language;

internal static class RazorFileKinds
{
    private static readonly FrozenDictionary<string, RazorFileKind> s_fileKindMap = new Dictionary<string, RazorFileKind>(StringComparer.OrdinalIgnoreCase)
    {
        [FileKinds.Component] = RazorFileKind.Component,
        [FileKinds.ComponentImport] = RazorFileKind.ComponentImport,
        [FileKinds.Legacy] = RazorFileKind.Legacy
    }.ToFrozenDictionary();

    internal const RazorFileKind MaxValue = RazorFileKind.ComponentImport;

    public static bool IsComponent(string fileKind)
        => IsComponent(FromString(fileKind));

    public static bool IsComponent(RazorFileKind fileKind)
        => fileKind is RazorFileKind.Component or RazorFileKind.ComponentImport;

    public static bool IsComponentImport(string fileKind)
        => IsComponentImport(FromString(fileKind));

    public static bool IsComponentImport(RazorFileKind fileKind)
        => fileKind is RazorFileKind.ComponentImport;

    public static bool IsLegacy(string fileKind)
        => FromString(fileKind) is RazorFileKind.Legacy;

    public static bool IsLegacy(RazorFileKind fileKind)
        => fileKind is RazorFileKind.Legacy;

    public static RazorFileKind FromString(string? fileKind)
        => fileKind is not null && s_fileKindMap.TryGetValue(fileKind, out var result)
            ? result
            : RazorFileKind.None;

    public static RazorFileKind GetComponentFileKindFromFilePath(string filePath)
    {
        ArgHelper.ThrowIfNull(filePath);

        return string.Equals(ComponentMetadata.ImportsFileName, Path.GetFileName(filePath), StringComparison.Ordinal)
            ? RazorFileKind.ComponentImport
            : RazorFileKind.Component;
    }

    public static RazorFileKind GetFileKindFromFilePath(string filePath)
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
