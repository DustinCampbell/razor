// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

internal static class Extensions
{
    public static string GetContent(this HtmlContentIntermediateNode node)
    {
        using var tokens = new PooledArrayBuilder<HtmlIntermediateToken>();

        node.CollectDescendantNodes(ref tokens.AsRef());

        if (tokens.Count == 0)
        {
            return string.Empty;
        }

        using var _ = StringBuilderPool.GetPooledObject(out var builder);

        foreach (var token in tokens)
        {
            builder.Append(token.Content);
        }

        return builder.ToString();
    }
}
