// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal static class PooledHashSet
{
    public static PooledHashSet<T> Create<T>(ReadOnlySpan<T> source)
    {
        var result = new PooledHashSet<T>(source.Length);

        foreach (var item in source)
        {
            result.Add(item);
        }

        return result;
    }
}
