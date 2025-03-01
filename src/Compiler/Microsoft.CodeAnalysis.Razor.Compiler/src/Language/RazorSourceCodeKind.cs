// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

public enum RazorSourceCodeKind
{
    /// <summary>
    ///  Not Razor source code.
    /// </summary>
    None = 0,

    /// <summary>
    ///  Legacy Razor source code, i.e. .cshtml files.
    /// </summary>
    Legacy,

    /// <summary>
    ///  Razor component source code, i.e. .razor files.
    /// </summary>
    Component,

    /// <summary>
    ///  Razor component import source code, i.e. _Import.razor files.
    /// </summary>
    ComponentImport
}
