// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal partial class QueuePool<T>
{
    protected class Policy : IPooledObjectPolicy<Queue<T>>
    {
        public static readonly Policy Instance = new();

        protected Policy()
        {
        }

        public virtual Queue<T> Create() => [];

        public virtual bool Return(Queue<T> queue)
        {
            var count = queue.Count;

            queue.Clear();

            if (count > DefaultPool.MaximumObjectSize)
            {
                queue.TrimExcess();
            }

            return true;
        }
    }
}
