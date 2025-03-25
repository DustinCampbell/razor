// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.Razor.DynamicFiles;

internal sealed class GeneratedDocumentTextLoader(DocumentSnapshot document, string filePath) : TextLoader
{
    private readonly AsyncLazy<TextAndVersion> _lazyTextAndVersion = AsyncLazy.Create(
        async (arg, cancellationToken) =>
        {
            var (document, filePath) = arg;

            var codeDocument = await document.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
            var csharpSourceText = codeDocument.GetCSharpDocument().Text;

            // If the encoding isn't UTF8, edit-and-continue won't work.
            Debug.Assert(csharpSourceText.Encoding == Encoding.UTF8);

            return TextAndVersion.Create(csharpSourceText, VersionStamp.Create(), filePath);
        },
        arg: (document, filePath));

    public override Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
    {
        return _lazyTextAndVersion.GetValueAsync(cancellationToken);
    }
}
