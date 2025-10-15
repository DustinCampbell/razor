// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

/// <summary>
/// A pool of <see cref="ImmutableDictionary{TKey, TValue}.Builder"/> instances.
/// </summary>
/// 
/// <remarks>
/// Instances originating from this pool are intended to be short-lived and are suitable
/// for temporary work. Do not return them as the results of methods or store them in fields.
/// </remarks>
internal partial class DictionaryBuilderPool<TKey, TValue> : DefaultObjectPool<ImmutableDictionary<TKey, TValue>.Builder>
    where TKey : notnull
{
    public static readonly DictionaryBuilderPool<TKey, TValue> Default = new(Policy.Instance, size: DefaultPool.DefaultPoolSize);

    protected DictionaryBuilderPool(IPooledObjectPolicy<ImmutableDictionary<TKey, TValue>.Builder> policy, int size)
        : base(policy, size)
    {
    }

    public static DictionaryBuilderPool<TKey, TValue> Create(IEqualityComparer<TKey> comparer)
        => new(new Policy(comparer), size: DefaultPool.DefaultPoolSize);

    public static PooledObject<ImmutableDictionary<TKey, TValue>.Builder> GetPooledObject()
        => Default.GetPooledObject();

    public static PooledObject<ImmutableDictionary<TKey, TValue>.Builder> GetPooledObject(out ImmutableDictionary<TKey, TValue>.Builder builder)
        => Default.GetPooledObject(out builder);
}
