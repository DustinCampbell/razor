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

internal class GeneratedDocumentTextLoader(IDocumentSnapshot document, string filePath) : TextLoader
{
    private readonly AsyncLazy<TextAndVersion> _lazyLoadTextAndVersion = AsyncLazy.Create(
        async static (arg, cancellationToken) =>
        {
            var (document, filePath) = arg;

            var codeDocument = await document.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

            var csharpSourceText = codeDocument.GetCSharpDocument().Text;

            // If the encoding isn't UTF8, edit-continue won't work.
            Debug.Assert(csharpSourceText.Encoding == Encoding.UTF8);

            return TextAndVersion.Create(csharpSourceText, version: VersionStamp.Create(), filePath);
        },
        arg: (document, filePath));

    public override Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
        => _lazyLoadTextAndVersion.GetValueAsync(cancellationToken);
}
