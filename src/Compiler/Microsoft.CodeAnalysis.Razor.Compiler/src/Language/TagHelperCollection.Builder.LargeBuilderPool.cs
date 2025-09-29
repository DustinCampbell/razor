// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperCollection
{
    public sealed partial class Builder
    {
        private sealed class LargeBuilderPool : ArrayBuilderPool<TagHelperDescriptor>
        {
            public static new readonly LargeBuilderPool Default = new();

            private LargeBuilderPool()
                : base(Policy.Instance, DefaultPool.DefaultPoolSize)
            {
            }

            private new sealed class Policy : ArrayBuilderPool<TagHelperDescriptor>.Policy
            {
                public static new readonly Policy Instance = new();

                private Policy()
                {
                }

                public override ImmutableArray<TagHelperDescriptor>.Builder Create()
                    => ImmutableArray.CreateBuilder<TagHelperDescriptor>(InitialCapacity);

                public override bool Return(ImmutableArray<TagHelperDescriptor>.Builder builder)
                {
                    var count = builder.Count;

                    builder.Clear();

                    if (count > MaximumObjectSize)
                    {
                        builder.Capacity = MaximumObjectSize;
                    }

                    return true;
                }
            }
        }
    }
}
