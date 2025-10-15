// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal partial class StopwatchPool
{
    protected class Policy : IPooledObjectPolicy<Stopwatch>
    {
        public static readonly Policy Instance = new();

        protected Policy()
        {
        }

        public virtual Stopwatch Create() => new();

        public virtual bool Return(Stopwatch watch)
        {
            watch.Reset();
            return true;
        }
    }
}
