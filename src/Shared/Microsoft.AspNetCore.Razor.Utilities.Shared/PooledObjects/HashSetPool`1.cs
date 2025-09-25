// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

/// <summary>
/// A pool of <see cref="HashSet{T}"/> instances that compares items using default equality.
/// </summary>
/// 
/// <remarks>
/// Instances originating from this pool are intended to be short-lived and are suitable
/// for temporary work. Do not return them as the results of methods or store them in fields.
/// </remarks>
internal partial class HashSetPool<T> : DefaultObjectPool<HashSet<T>>
{
    public static readonly HashSetPool<T> Default = new(Policy.Default, size: DefaultPool.DefaultPoolSize);

    private readonly Policy _policy;

    protected HashSetPool(Policy policy, int size)
        : base(policy, size)
    {
        _policy = policy;
    }

    public IEqualityComparer<T> Comparer => _policy.Comparer;

    public static HashSetPool<T> Create(IEqualityComparer<T> comparer)
        => new(new Policy(comparer), size: DefaultPool.DefaultPoolSize);

    public static PooledObject<HashSet<T>> GetPooledObject()
        => Default.GetPooledObject();

    public static PooledObject<HashSet<T>> GetPooledObject(out HashSet<T> set)
        => Default.GetPooledObject(out set);
}
