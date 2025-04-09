// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language;

public static class TestRazorSourceDocument
{
    public static RazorSourceDocument CreateResource(string resourcePath, Type type, Encoding encoding = null, bool normalizeNewLines = false)
    {
        return CreateResource(resourcePath, type.GetTypeInfo().Assembly, encoding, normalizeNewLines);
    }

    public static RazorSourceDocument CreateResource(string resourcePath, Assembly assembly, Encoding encoding = null, bool normalizeNewLines = false)
    {
        var file = TestFile.Create(resourcePath, assembly);

        using (var input = file.OpenRead())
        using (var reader = new StreamReader(input))
        {
            var content = reader.ReadToEnd();
            if (normalizeNewLines)
            {
                content = NormalizeNewLines(content);
            }

            var properties = RazorSourceDocumentProperties.Create(resourcePath, resourcePath);
            return Create(content, encoding ?? Encoding.UTF8, properties);
        }
    }

    public static RazorSourceDocument CreateResource(
        string path,
        Assembly assembly,
        Encoding encoding,
        RazorSourceDocumentProperties properties,
        bool normalizeNewLines = false)
    {
        var file = TestFile.Create(path, assembly);

        using (var input = file.OpenRead())
        using (var reader = new StreamReader(input))
        {
            var content = reader.ReadToEnd();
            if (normalizeNewLines)
            {
                content = NormalizeNewLines(content);
            }

            return Create(content, encoding ?? Encoding.UTF8, properties);
        }
    }

    public static MemoryStream CreateStreamContent(string content = "Hello, World!", Encoding encoding = null, bool normalizeNewLines = false)
    {
        var stream = new MemoryStream();
        encoding ??= Encoding.UTF8;
        using (var writer = new StreamWriter(stream, encoding, bufferSize: 1024, leaveOpen: true))
        {
            if (normalizeNewLines)
            {
                content = NormalizeNewLines(content);
            }

            writer.Write(content);
        }

        stream.Seek(0L, SeekOrigin.Begin);

        return stream;
    }

    /// <summary>
    ///  Creates a <see cref="RazorSourceDocument"/> from the specified <paramref name="content"/>.
    /// </summary>
    /// <param name="content">The source document content.</param>
    /// <param name="properties">Properties to configure the <see cref="RazorSourceDocument"/>.</param>
    /// <returns>
    ///  The <see cref="RazorSourceDocument"/>.
    /// </returns>
    /// <remarks>
    ///  Uses <see cref="Encoding.UTF8" />.
    /// </remarks>
    public static RazorSourceDocument Create(string content, RazorSourceDocumentProperties properties)
    {
        return Create(content, Encoding.UTF8, properties);
    }

    /// <summary>
    /// Creates a <see cref="RazorSourceDocument"/> from the specified <paramref name="content"/>.
    /// </summary>
    /// <param name="content">The source document content.</param>
    /// <param name="encoding">The encoding of the source document.</param>
    /// <param name="properties">Properties to configure the <see cref="RazorSourceDocument"/>.</param>
    /// <returns>
    ///  The <see cref="RazorSourceDocument"/>.
    /// </returns>
    public static RazorSourceDocument Create(string content, Encoding encoding, RazorSourceDocumentProperties properties)
    {
        ArgHelper.ThrowIfNull(content);
        ArgHelper.ThrowIfNull(encoding);

        var text = SourceText.From(content, encoding, checksumAlgorithm: SourceHashAlgorithm.Sha256);

        return RazorSourceDocument.Create(text, properties);
    }

    /// <summary>
    ///  Creates a <see cref="RazorSourceDocument"/> from the specified <paramref name="content"/>.
    /// </summary>
    /// <param name="content">The source document content.</param>
    /// <param name="fileName">The file name of the <see cref="RazorSourceDocument"/>.</param>
    /// <returns>The <see cref="RazorSourceDocument"/>.</returns>
    /// <remarks>Uses <see cref="Encoding.UTF8" /></remarks>
    public static RazorSourceDocument Create(string content, string fileName)
    {
        return Create(content, fileName, Encoding.UTF8);
    }

    /// <summary>
    ///  Creates a <see cref="RazorSourceDocument"/> from the specified <paramref name="content"/>.
    /// </summary>
    /// <param name="content">The source document content.</param>
    /// <param name="fileName">The file name of the <see cref="RazorSourceDocument"/>.</param>
    /// <param name="encoding">The <see cref="Encoding"/> of the file <paramref name="content"/> was read from.</param>
    /// <returns>The <see cref="RazorSourceDocument"/>.</returns>
    public static RazorSourceDocument Create(string content, string fileName, Encoding encoding)
    {
        ArgHelper.ThrowIfNull(content);
        ArgHelper.ThrowIfNull(encoding);

        var properties = RazorSourceDocumentProperties.Create(fileName, relativePath: null);
        var text = SourceText.From(content, encoding, checksumAlgorithm: SourceHashAlgorithm.Sha256);

        return RazorSourceDocument.Create(text, properties);
    }

    public static RazorSourceDocument Create(
        string content = "Hello, world!",
        Encoding encoding = null,
        bool normalizeNewLines = false,
        string filePath = "test.cshtml",
        string relativePath = "test.cshtml")
    {
        if (normalizeNewLines)
        {
            content = NormalizeNewLines(content);
        }

        var properties = RazorSourceDocumentProperties.Create(filePath, relativePath);
        return Create(content, encoding ?? Encoding.UTF8, properties);
    }

    public static RazorSourceDocument Create(
        string content,
        RazorSourceDocumentProperties properties,
        Encoding encoding = null,
        bool normalizeNewLines = false)
    {
        if (normalizeNewLines)
        {
            content = NormalizeNewLines(content);
        }

        return Create(content, encoding ?? Encoding.UTF8, properties);
    }

    private static string NormalizeNewLines(string content)
    {
        return Regex.Replace(content, "(?<!\r)\n", "\r\n", RegexOptions.None, TimeSpan.FromSeconds(10));
    }
}
