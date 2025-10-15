// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal partial class MarkupEndTagSyntax : ILegacySyntax
{
    SyntaxNode ILegacySyntax.Update(ISpanChunkGenerator? chunkGenerator, SpanEditHandler? editHandler)
        => Update(chunkGenerator, editHandler);

    SyntaxNode ILegacySyntax.WithEditHandler(SpanEditHandler? editHandler)
        => WithEditHandler(editHandler);

    public MarkupEndTagSyntax Update(ISpanChunkGenerator? chunkGenerator, SpanEditHandler? editHandler)
        => Update(OpenAngle, ForwardSlash, Bang, Name, MiscAttributeContent, CloseAngle, chunkGenerator, editHandler, IsMarkupTransition);
}
