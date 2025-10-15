// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal partial class ArrayBuilderPool<T>
{
    protected class Policy : IPooledObjectPolicy<ImmutableArray<T>.Builder>
    {
        public static readonly Policy Instance = new();

        protected Policy()
        {
        }

        public virtual ImmutableArray<T>.Builder Create()
            => ImmutableArray.CreateBuilder<T>();

        public virtual bool Return(ImmutableArray<T>.Builder builder)
        {
            var count = builder.Count;

            builder.Clear();

            if (count > DefaultPool.MaximumObjectSize)
            {
                builder.Capacity = DefaultPool.MaximumObjectSize;
            }

            return true;
        }
    }
}
