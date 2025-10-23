// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

public readonly partial struct Content
{
    public readonly ref partial struct PartList
    {
        /// <summary>
        ///  Provides enumeration over only the non-empty parts of a <see cref="PartList"/>.
        /// </summary>
        /// <remarks>
        ///  <para>
        ///   This enumerable wraps a <see cref="PartList"/> and provides access to only those parts
        ///   that contain at least one character. Empty parts are automatically filtered out during
        ///   enumeration.
        ///  </para>
        ///  <para>
        ///   The enumerable uses the same efficient ref struct pattern as the underlying
        ///   <see cref="PartList.Enumerator"/> to avoid heap allocations.
        ///  </para>
        /// </remarks>
        /// <param name="parts">The <see cref="PartList"/> to enumerate non-empty parts from.</param>
        public readonly ref partial struct NonEmptyParts(PartList parts)
        {
            private readonly PartList _parts = parts;

            /// <summary>
            ///  Helper that gets only the non-empty parts of this content as a pooled array span.
            /// </summary>
            internal PooledArray<ReadOnlyMemory<char>> AsPooledSpan(out ReadOnlySpan<ReadOnlyMemory<char>> result)
            {
                var pooledArray = ArrayPool<ReadOnlyMemory<char>>.Shared.GetPooledArraySpan(
                    minimumLength: _parts.Count,
                    clearOnReturn: true,
                    out var span);

                var count = 0;

                foreach (var part in _parts.NonEmpty)
                {
                    span[count++] = part;
                }

                result = span[..count];

                return pooledArray;
            }

            /// <summary>
            ///  Returns an enumerator that iterates through only the non-empty parts.
            /// </summary>
            /// <returns>
            ///  An <see cref="Enumerator"/> that can be used to iterate through the non-empty parts.
            /// </returns>
            /// <remarks>
            ///  The enumerator is a ref struct that wraps the underlying <see cref="PartList.Enumerator"/>
            ///  and filters out parts with zero length during enumeration.
            /// </remarks>
            public Enumerator GetEnumerator() => new(_parts);
        }
    }
}
