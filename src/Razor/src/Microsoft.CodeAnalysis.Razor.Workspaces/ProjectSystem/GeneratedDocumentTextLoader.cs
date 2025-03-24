// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal class GeneratedDocumentTextLoader(RazorCodeDocument codeDocument, string filePath) : TextLoader
{
    private readonly RazorCodeDocument _codeDocument = codeDocument;
    private readonly string _filePath = filePath;
    private readonly VersionStamp _version = VersionStamp.Create();

    public override Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
    {
        var csharpSourceText = _codeDocument.GetCSharpSourceText();

        // If the encoding isn't UTF8, edit-continue won't work.
        Debug.Assert(csharpSourceText.Encoding == Encoding.UTF8);

        return Task.FromResult(TextAndVersion.Create(csharpSourceText, _version, _filePath));
    }
}
