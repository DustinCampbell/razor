// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

/// <summary>
/// A pool of <see cref="HashSet{T}"/> instances that compares strings.
/// </summary>
/// 
/// <remarks>
/// Instances originating from this pool are intended to be short-lived and are suitable
/// for temporary work. Do not return them as the results of methods or store them in fields.
/// </remarks>
internal class StringHashSetPool : HashSetPool<string>
{
    public static readonly StringHashSetPool Ordinal = Create(StringComparer.Ordinal);
    public static readonly StringHashSetPool OrdinalIgnoreCase = Create(StringComparer.OrdinalIgnoreCase);

    protected StringHashSetPool(Policy policy, int size)
        : base(policy, size)
    {
    }

    public new static StringHashSetPool Create(IEqualityComparer<string> comparer)
        => new(new Policy(comparer), size: DefaultPool.DefaultPoolSize);

    public static PooledObject<HashSet<string>> GetPooledObject(bool ignoreCase)
        => ignoreCase
            ? OrdinalIgnoreCase.GetPooledObject()
            : Ordinal.GetPooledObject();

    public static PooledObject<HashSet<string>> GetPooledObject(bool ignoreCase, out HashSet<string> set)
        => ignoreCase
            ? OrdinalIgnoreCase.GetPooledObject(out set)
            : Ordinal.GetPooledObject(out set);
}
