// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

public readonly partial struct Content
{
    /// <summary>
    ///  Represents a list of flattened character parts from a <see cref="Content"/> structure.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   <see cref="PartList"/> provides enumeration over the leaf character sequences in a
    ///   potentially nested <see cref="Content"/> structure. Nested content is automatically
    ///   flattened during enumeration, exposing only the actual character data.
    ///  </para>
    ///  <para>
    ///   This type supports efficient foreach enumeration using a ref struct enumerator
    ///   that avoids heap allocations for the common case.
    ///  </para>
    /// </remarks>
    public readonly ref partial struct PartList(Content content)
    {
        private readonly Content _content = content;

        /// <summary>
        ///  Gets the total number of flattened parts in the content.
        /// </summary>
        /// <returns>
        ///  The number of individual character sequences after flattening all nested structures.
        ///  Returns 0 for empty content and 1 for single-value content.
        /// </returns>
        public int Count => _content._data.PartCount;

        /// <summary>
        ///  Gets an enumerable that provides access to only the non-empty parts in the content.
        /// </summary>
        /// <returns>
        ///  A <see cref="NonEmptyParts"/> that can be used to iterate through only the non-empty parts.
        /// </returns>
        /// <remarks>
        ///  This property provides efficient enumeration over non-empty parts, automatically filtering
        ///  out any parts that have zero length during traversal.
        /// </remarks>
        public readonly NonEmptyParts NonEmpty => new(this);

        /// <summary>
        ///  Helper that gets only the non-empty parts of this content as a pooled array span.
        /// </summary>
        internal PooledArray<ReadOnlyMemory<char>> AsPooledSpan(out ReadOnlySpan<ReadOnlyMemory<char>> result)
        {
            var pooledArray = ArrayPool<ReadOnlyMemory<char>>.Shared.GetPooledArraySpan(
                minimumLength: Count,
                clearOnReturn: true,
                out var span);

            var count = 0;

            foreach (var part in this)
            {
                span[count++] = part;
            }

            result = span[..count];

            return pooledArray;
        }

        /// <summary>
        ///  Returns an enumerator that iterates through the flattened parts.
        /// </summary>
        /// <returns>
        ///  An <see cref="Enumerator"/> that can be used to iterate through the parts.
        /// </returns>
        /// <remarks>
        ///  The enumerator is a ref struct that performs depth-first traversal of nested
        ///  content structures, yielding only the leaf character sequences.
        /// </remarks>
        public readonly Enumerator GetEnumerator() => new(this);
    }
}
