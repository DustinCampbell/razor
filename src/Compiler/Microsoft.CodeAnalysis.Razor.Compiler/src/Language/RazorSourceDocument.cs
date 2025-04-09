// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// The Razor template source.
/// </summary>
public sealed class RazorSourceDocument
{
    private readonly RazorSourceDocumentProperties _properties;

    /// <summary>
    /// Gets the source text of the document.
    /// </summary>
    public SourceText Text { get; }

    private RazorSourceDocument(SourceText text, RazorSourceDocumentProperties properties)
    {
        ArgHelper.ThrowIfNull(text);
        ArgHelper.ThrowIfNull(properties);

        Text = text;
        _properties = properties;
    }

    /// <inheritdoc cref="RazorSourceDocumentProperties.FilePath"/>
    public string? FilePath => _properties.FilePath;

    /// <inheritdoc cref="RazorSourceDocumentProperties.RelativePath"/>
    public string? RelativePath => _properties.RelativePath;

    /// <summary>
    ///  Gets the file path in a format that should be used for display.
    /// </summary>
    /// <returns>
    ///  The <see cref="RelativePath"/> if set, or the <see cref="FilePath"/>.
    /// </returns>
    public string? GetFilePathForDisplay()
    {
        return RelativePath ?? FilePath;
    }

    /// <summary>
    ///  Creates a <see cref="RazorSourceDocument"/> from the specified <paramref name="text"/>.
    /// </summary>
    /// <param name="content">The source text.</param>
    /// <param name="properties">Properties to configure the <see cref="RazorSourceDocument"/>.</param>
    /// <returns>
    ///  The <see cref="RazorSourceDocument"/>.
    /// </returns>
    public static RazorSourceDocument Create(SourceText text, RazorSourceDocumentProperties properties)
    {
        return new(text, properties);
    }

    /// <summary>
    /// Reads the <see cref="RazorSourceDocument"/> from the specified <paramref name="projectItem"/>.
    /// </summary>
    /// <param name="projectItem">The <see cref="RazorProjectItem"/> to read from.</param>
    /// <returns>The <see cref="RazorSourceDocument"/>.</returns>
    public static RazorSourceDocument ReadFrom(RazorProjectItem projectItem)
    {
        ArgHelper.ThrowIfNull(projectItem);

        // ProjectItem.PhysicalPath is usually an absolute (rooted) path.
        var filePath = projectItem.PhysicalPath;
        if (string.IsNullOrEmpty(filePath))
        {
            // Fall back to the relative path only if necessary.
            filePath = projectItem.RelativePhysicalPath;
        }

        if (string.IsNullOrEmpty(filePath))
        {
            // Then fall back to the FilePath (yeah it's a bad name) which is like an MVC view engine path
            // It's much better to have something than nothing.
            filePath = projectItem.FilePath;
        }

        using var stream = projectItem.Read();

        // Autodetect the encoding.
        var relativePath = projectItem.RelativePhysicalPath ?? projectItem.FilePath;
        var sourceText = SourceText.From(stream, checksumAlgorithm: SourceHashAlgorithm.Sha256);
        return new RazorSourceDocument(sourceText, RazorSourceDocumentProperties.Create(filePath, relativePath));
    }
}
