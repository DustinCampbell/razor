// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal partial class HashSetPool<T>
{
    protected class Policy(IEqualityComparer<T>? comparer = null) : IPooledObjectPolicy<HashSet<T>>
    {
        public static readonly Policy Default = new();

        public IEqualityComparer<T> Comparer { get; } = comparer ?? EqualityComparer<T>.Default;

        public virtual HashSet<T> Create()
            => new(Comparer);

        public virtual bool Return(HashSet<T> set)
        {
            var count = set.Count;

            set.Clear();

            if (count > DefaultPool.MaximumObjectSize)
            {
                set.TrimExcess();
            }

            return true;
        }
    }
}
