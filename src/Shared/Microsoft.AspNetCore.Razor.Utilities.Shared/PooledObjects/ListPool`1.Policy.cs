// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal partial class ListPool<T>
{
    protected class Policy : IPooledObjectPolicy<List<T>>
    {
        public static readonly Policy Instance = new();

        protected Policy()
        {
        }

        public virtual List<T> Create() => [];

        public virtual bool Return(List<T> list)
        {
            var count = list.Count;

            list.Clear();

            if (count > DefaultPool.MaximumObjectSize)
            {
                list.TrimExcess();
            }

            return true;
        }
    }
}
