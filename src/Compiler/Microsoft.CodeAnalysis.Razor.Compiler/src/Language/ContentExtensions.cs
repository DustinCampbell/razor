// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

internal static class ContentExtensions
{
    public static Content ToContent(this ref readonly PooledArrayBuilder<Content> builder)
    {
        return builder.Count switch
        {
            0 => Content.Empty,
            1 => builder[0],
            _ => new(builder.ToImmutable())
        };
    }

    public static Content ToContentAndClear(this ref PooledArrayBuilder<Content> builder)
    {
        return builder.Count switch
        {
            0 => Content.Empty,
            1 => builder[0],
            _ => new(builder.ToImmutableAndClear())
        };
    }

    public static Content ToContent(this ref readonly PooledArrayBuilder<ReadOnlyMemory<char>> builder)
    {
        return builder.Count switch
        {
            0 => Content.Empty,
            1 => builder[0],
            _ => new(builder.ToImmutable())
        };
    }

    public static Content ToContentAndClear(this ref PooledArrayBuilder<ReadOnlyMemory<char>> builder)
    {
        return builder.Count switch
        {
            0 => Content.Empty,
            1 => builder[0],
            _ => new(builder.ToImmutableAndClear())
        };
    }

    public static Content ToContent(this ref readonly PooledArrayBuilder<string> builder)
    {
        return builder.Count switch
        {
            0 => Content.Empty,
            1 => builder[0],
            _ => new(builder.ToImmutable())
        };
    }

    public static Content ToContentAndClear(this ref PooledArrayBuilder<string> builder)
    {
        return builder.Count switch
        {
            0 => Content.Empty,
            1 => builder[0],
            _ => new(builder.ToImmutableAndClear())
        };
    }

    public static Content ToContent(this ref readonly MemoryBuilder<Content> builder)
    {
        return builder.Length switch
        {
            0 => Content.Empty,
            1 => builder[0],
            _ => new(ImmutableCollectionsMarshal.AsImmutableArray(builder.AsMemory().ToArray()))
        };
    }

    public static Content ToContent(this ref readonly MemoryBuilder<ReadOnlyMemory<char>> builder)
    {
        return builder.Length switch
        {
            0 => Content.Empty,
            1 => builder[0],
            _ => new(ImmutableCollectionsMarshal.AsImmutableArray(builder.AsMemory().ToArray()))
        };
    }

    public static Content ToContent(this ref readonly MemoryBuilder<string> builder)
    {
        return builder.Length switch
        {
            0 => Content.Empty,
            1 => builder[0],
            _ => new(ImmutableCollectionsMarshal.AsImmutableArray(builder.AsMemory().ToArray()))
        };
    }
}
