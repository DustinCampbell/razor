﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;

internal static class IDocumentContextFactoryExtensions
{
    public static bool TryCreate(
        this IDocumentContextFactory service,
        TextDocumentIdentifier documentIdentifier,
        [NotNullWhen(true)] out DocumentContext? context)
            => service.TryCreate(documentIdentifier.DocumentUri.GetRequiredParsedUri(), documentIdentifier.GetProjectContext(), out context);

    public static bool TryCreate(
        this IDocumentContextFactory service,
        Uri documentUri,
        [NotNullWhen(true)] out DocumentContext? context)
            => service.TryCreate(documentUri, projectContext: null, out context);
}
