// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

#if !NET
using Microsoft.Extensions.Internal;
#endif

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public readonly partial record struct Content
{
    private sealed class ValueComparer : IEqualityComparer<ReadOnlyMemory<char>>
    {
        public static readonly ValueComparer Instance = new();

        private ValueComparer()
        {
        }

        public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
            => x.Span.Equals(y.Span, StringComparison.Ordinal);

        public int GetHashCode(ReadOnlyMemory<char> obj)
        {
#if NET
            return string.GetHashCode(obj.Span);
#else
            var hash = HashCodeCombiner.Start();

            foreach (var ch in obj.Span)
            {
                hash.Add(ch);
            }

            return hash.CombinedHash;
#endif
        }
    }
}
