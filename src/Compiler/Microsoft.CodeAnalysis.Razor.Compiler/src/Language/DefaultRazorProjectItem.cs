// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
///  The default implementation of <see cref="RazorProjectItem"/>.
/// </summary>
/// 
/// <param name="basePath">The base path.</param>
/// <param name="filePath">The file path.</param>
/// <param name="physicalPath">The physical path of the file path.</param>
/// <param name="relativePhysicalPath">The physical path of the base path.</param>
/// <param name="sourceCodeKind">The file kind. If null, the document kind will be inferred from the file extension.</param>
/// <param name="cssScope">A scope identifier that will be used on elements in the generated class, or <see langword="null"/>.</param>
internal sealed class DefaultRazorProjectItem(
    string basePath, string filePath,
    string physicalPath, string relativePhysicalPath,
    RazorSourceCodeKind? sourceCodeKind, string? cssScope) : RazorProjectItem
{
    public override string BasePath { get; } = basePath;
    public override string FilePath { get; } = filePath;
    public override string PhysicalPath { get; } = physicalPath;
    public override string RelativePhysicalPath { get; } = relativePhysicalPath;
    public override RazorSourceCodeKind SourceCodeKind { get; } = ComputeFileKind(sourceCodeKind, filePath);
    public override string? CssScope { get; } = cssScope;

    public override bool Exists
        => File.Exists(PhysicalPath);

    public override Stream Read()
        => new FileStream(PhysicalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
}
