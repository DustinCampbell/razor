// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperCollection
{
    public sealed partial class Builder
    {
        private sealed class LargeSetPool : HashSetPool<TagHelperDescriptor>
        {
            public static new readonly LargeSetPool Default = new();

            private LargeSetPool()
                : base(Policy.Instance, DefaultPool.DefaultPoolSize)
            {
            }

            private new sealed class Policy : HashSetPool<TagHelperDescriptor>.Policy
            {
                public static readonly Policy Instance = new();

                private Policy()
                    : base(EqualityComparer<TagHelperDescriptor>.Default)
                {
                }

                public override HashSet<TagHelperDescriptor> Create()
                {
#if NET
                    return new(InitialCapacity, Comparer);
#else
                    return new(Comparer);
#endif
                }

                public override bool Return(HashSet<TagHelperDescriptor> set)
                {
                    var count = set.Count;

                    set.Clear();

                    if (count > MaximumObjectSize)
                    {
                        set.TrimExcess();
                    }

                    return true;
                }
            }
        }
    }
}
