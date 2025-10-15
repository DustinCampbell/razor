// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

/// <summary>
/// Pooled <see cref="Dictionary{TKey, TValue}"/> instances when the key is of type <see cref="string"/>.
/// </summary>
/// 
/// <remarks>
/// Instances originating from this pool are intended to be short-lived and are suitable
/// for temporary work. Do not return them as the results of methods or store them in fields.
/// </remarks>
internal partial class StringDictionaryPool<TValue> : DictionaryPool<string, TValue>
{
    public static readonly StringDictionaryPool<TValue> Ordinal = Create(StringComparer.Ordinal);
    public static readonly StringDictionaryPool<TValue> OrdinalIgnoreCase = Create(StringComparer.OrdinalIgnoreCase);

    protected StringDictionaryPool(IPooledObjectPolicy<Dictionary<string, TValue>> policy, int size)
        : base(policy, size)
    {
    }

    public new static StringDictionaryPool<TValue> Create(IEqualityComparer<string> comparer)
        => new(new Policy(comparer), size: DefaultPool.DefaultPoolSize);

    public static PooledObject<Dictionary<string, TValue>> GetPooledObject(bool ignoreCase)
        => ignoreCase
            ? OrdinalIgnoreCase.GetPooledObject()
            : Ordinal.GetPooledObject();

    public static PooledObject<Dictionary<string, TValue>> GetPooledObject(bool ignoreCase, out Dictionary<string, TValue> map)
        => ignoreCase
            ? OrdinalIgnoreCase.GetPooledObject(out map)
            : Ordinal.GetPooledObject(out map);
}
