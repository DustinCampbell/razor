// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

internal static class ContentExtensions
{
    public static Content ToContent(this ref readonly PooledArrayBuilder<ReadOnlyMemory<char>> builder)
    {
        return builder.Count switch
        {
            0 => Content.Empty,
            1 => new(builder[0]),
            _ => new(builder.ToImmutable())
        };
    }

    public static Content ToContentAndClear(this ref PooledArrayBuilder<ReadOnlyMemory<char>> builder)
    {
        return builder.Count switch
        {
            0 => Content.Empty,
            1 => new(builder[0]),
            _ => new(builder.ToImmutableAndClear())
        };
    }
}
