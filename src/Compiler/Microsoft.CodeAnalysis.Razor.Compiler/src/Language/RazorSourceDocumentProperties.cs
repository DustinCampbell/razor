// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
///  Use to configure optional properties for creating a <see cref="RazorSourceDocument"/>.
/// </summary>
public sealed class RazorSourceDocumentProperties
{
    /// <summary>
    ///  A <see cref="RazorSourceDocumentProperties"/> instance with <see cref="FilePath"/>
    ///  and <see cref="RelativePath"/> set to <see langword="null"/>.
    /// </summary>
    internal static readonly RazorSourceDocumentProperties Empty = new(filePath: null, relativePath: null);

    /// <summary>
    ///  Gets the path to the source file, which ay be an absolute or project-relative path, or <see langword="null"/>.
    /// </summary>
    /// <remarks>
    ///  An absolute path must be provided to generate debuggable assemblies.
    /// </remarks>
    public string? FilePath { get; }

    /// <summary>
    ///  Gets the project-relative path to the source file, or <see langword="null"/>.
    /// </summary>
    /// <remarks>
    ///  The relative path (if provided) is used for display (error messages). The project-relative path may also
    ///  be used to embed checksums of the original source documents to support runtime recompilation of Razor code.
    /// </remarks>
    public string? RelativePath { get; }

    private RazorSourceDocumentProperties(string? filePath, string? relativePath)
    {
        FilePath = filePath;
        RelativePath = relativePath;
    }

    internal static RazorSourceDocumentProperties Create(string? filePath, string? relativePath)
    {
        if (filePath is null && relativePath is null)
        {
            return Empty;
        }

        return new(filePath, relativePath);
    }
}
