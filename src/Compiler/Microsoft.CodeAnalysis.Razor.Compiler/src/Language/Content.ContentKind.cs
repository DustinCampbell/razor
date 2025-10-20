// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

public readonly partial struct Content
{
    /// <summary>
    ///  Specifies the internal storage type for <see cref="Content"/>.
    /// </summary>
    /// <remarks>
    ///  <see cref="ContentData"/> packs this into 2 bits.
    /// </remarks>
    private enum ContentKind : byte
    {
        /// <summary>
        /// Content is stored as a single <see cref="ReadOnlyMemory{T}"/> value.
        /// </summary>
        Value = 0,

        /// <summary>
        /// Content is stored as an array of <see cref="Content"/> objects.
        /// </summary>
        ContentArray = 1,

        /// <summary>
        /// Content is stored as an array of <see cref="ReadOnlyMemory{T}"/> objects.
        /// </summary>
        MemoryArray = 2,

        /// <summary>
        /// Content is stored as an array of <see cref="string"/> objects.
        /// </summary>
        StringArray = 3
    }
}
