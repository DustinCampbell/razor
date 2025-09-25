// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal partial class QueuePool<T> : DefaultObjectPool<Queue<T>>
{
    public static readonly QueuePool<T> Default = new(Policy.Instance, size: DefaultPool.DefaultPoolSize);

    protected QueuePool(IPooledObjectPolicy<Queue<T>> policy, int size)
        : base(policy, size)
    {
    }

    public static PooledObject<Queue<T>> GetPooledObject()
        => Default.GetPooledObject();

    public static PooledObject<Queue<T>> GetPooledObject(out Queue<T> queue)
        => Default.GetPooledObject(out queue);
}
