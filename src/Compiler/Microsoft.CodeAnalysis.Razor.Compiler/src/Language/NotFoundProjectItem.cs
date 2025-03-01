// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// A <see cref="RazorProjectItem"/> that does not exist.
/// </summary>
/// <param name="filePath">The path.</param>
/// <param name="sourceCodeKind">The file kind</param>
internal sealed class NotFoundProjectItem(string filePath, RazorSourceCodeKind? sourceCodeKind) : RazorProjectItem
{
    /// <inheritdoc />
    public override string BasePath => string.Empty;

    /// <inheritdoc />
    public override string FilePath => filePath;

    /// <inheritdoc />
    public override RazorSourceCodeKind SourceCodeKind { get; } = ComputeFileKind(sourceCodeKind, filePath);

    /// <inheritdoc />
    public override bool Exists => false;

    /// <inheritdoc />
    public override string PhysicalPath => throw new NotSupportedException();

    /// <inheritdoc />
    public override Stream Read() => throw new NotSupportedException();
}
