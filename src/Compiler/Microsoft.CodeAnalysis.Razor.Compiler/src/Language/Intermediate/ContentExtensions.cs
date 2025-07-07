// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

internal static class ContentExtensions
{
    public static StringBuilder Append(this StringBuilder builder, Content content)
    {
        foreach (var part in content.AllParts)
        {
#if NET
            builder.Append(part);
#else
            unsafe
            {
                fixed (char* ptr = part.Span)
                {
                    builder.Append(ptr, part.Length);
                }
            }
#endif
        }

        return builder;
    }

    public static Content ToContent(this ref readonly PooledArrayBuilder<ReadOnlyMemory<char>> builder)
    {
        if (builder.Count == 0)
        {
            return Content.Empty;
        }

        if (builder.Count == 1)
        {
            return builder[0];
        }

        return new Content(builder.ToImmutable());
    }

    public static Content ToContent(this ref readonly PooledArrayBuilder<Content> builder)
    {
        if (builder.Count == 0)
        {
            return Content.Empty;
        }

        if (builder.Count == 1)
        {
            return builder[0];
        }

        return new Content(builder.ToImmutable());
    }
}
