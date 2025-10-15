// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal interface ILegacyInternalSyntax
{
    ISpanChunkGenerator? ChunkGenerator { get; }
    SpanEditHandler? EditHandler { get; }
}
