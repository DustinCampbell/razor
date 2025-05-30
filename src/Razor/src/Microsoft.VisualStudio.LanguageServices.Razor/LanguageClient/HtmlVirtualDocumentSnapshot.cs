﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Razor.LanguageClient;

internal class HtmlVirtualDocumentSnapshot : VirtualDocumentSnapshot
{
    public HtmlVirtualDocumentSnapshot(
        Uri uri,
        ITextSnapshot snapshot,
        long? hostDocumentSyncVersion,
        object? state)
    {
        if (uri is null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        Uri = uri;
        Snapshot = snapshot;
        HostDocumentSyncVersion = hostDocumentSyncVersion;
        State = state;
    }

    public override Uri Uri { get; }

    public override ITextSnapshot Snapshot { get; }

    public override long? HostDocumentSyncVersion { get; }

    public object? State { get; }
}
