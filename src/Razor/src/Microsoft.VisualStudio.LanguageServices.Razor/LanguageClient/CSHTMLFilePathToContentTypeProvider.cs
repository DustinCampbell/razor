﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Razor.LanguageClient;

[FileExtension(RazorLSPConstants.CSHTMLFileExtension)]
[Name(nameof(CSHTMLFilePathToContentTypeProvider))]
[Export(typeof(IFilePathToContentTypeProvider))]
[method: ImportingConstructor]
internal class CSHTMLFilePathToContentTypeProvider(
    IContentTypeRegistryService contentTypeRegistryService,
    ILspEditorFeatureDetector lspEditorFeatureDetector,
    LanguageServerFeatureOptions options)
    : RazorFilePathToContentTypeProviderBase(contentTypeRegistryService, lspEditorFeatureDetector, options)
{
}
