// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

public readonly partial struct Content
{
    /// <summary>
    ///  Creates a new <see cref="Content"/> that represents a substring of the current content.
    /// </summary>
    /// <param name="start">The zero-based starting character position.</param>
    /// <returns>
    ///  A <see cref="Content"/> that is equivalent to the substring that begins at <paramref name="start"/>
    ///  in this instance.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="start"/> is less than zero or greater than the length of this instance.
    /// </exception>
    /// <remarks>
    ///  This method efficiently creates a slice without copying the underlying character data.
    ///  For single-value content, it slices the underlying memory. For multi-part content,
    ///  it creates a new content structure that references only the necessary parts.
    /// </remarks>
    public Content Slice(int start)
    {
        ArgHelper.ThrowIfNegative(start);
        ArgHelper.ThrowIfGreaterThan(start, Length);

        // Fast path: slice is entire content
        if (start == 0)
        {
            return this;
        }

        // Fast path: single value content - just slice the memory
        if (IsSingleValue)
        {
            return _value[start..];
        }

        // Multi-part content: need to find the affected parts
        return SliceMultiPart(start, Length - start);
    }

    /// <summary>
    ///  Creates a new <see cref="Content"/> that represents a substring of the current content.
    /// </summary>
    /// <param name="start">The zero-based starting character position.</param>
    /// <param name="length">The number of characters in the substring.</param>
    /// <returns>
    ///  A <see cref="Content"/> that is equivalent to the substring of <paramref name="length"/>
    ///  that begins at <paramref name="start"/> in this instance.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="start"/> or <paramref name="length"/> is less than zero, or
    ///  <paramref name="start"/> + <paramref name="length"/> is greater than the length of this instance.
    /// </exception>
    /// <remarks>
    ///  This method efficiently creates a slice without copying the underlying character data.
    ///  For single-value content, it slices the underlying memory. For multi-part content,
    ///  it creates a new content structure that references only the necessary parts.
    /// </remarks>
    public Content Slice(int start, int length)
    {
        ArgHelper.ThrowIfNegative(start);
        ArgHelper.ThrowIfNegative(Length);
        ArgHelper.ThrowIfGreaterThan(start + length, Length);

        // Fast path: empty slice
        if (length == 0)
        {
            return Empty;
        }

        // Fast path: slice is entire content
        if (start == 0 && length == Length)
        {
            return this;
        }

        // Fast path: single value content - just slice the memory
        if (IsSingleValue)
        {
            return _value.Slice(start, length);
        }

        // Multi-part content: need to find the affected parts
        return SliceMultiPart(start, length);
    }

    private Content SliceMultiPart(int start, int length)
    {
        Debug.Assert(IsMultiPart);
        Debug.Assert(start >= 0);
        Debug.Assert(length > 0);
        Debug.Assert(start + length <= Length);

        var parts = Parts;

        using var builder = new PooledArrayBuilder<ReadOnlyMemory<char>>(capacity: parts.Count);
        var remaining = length;
        var currentPos = 0;

        foreach (var part in parts.NonEmpty)
        {
            var partLength = part.Length;

            // Skip parts entirely before the start position
            if (currentPos + partLength <= start)
            {
                currentPos += partLength;
                continue;
            }

            // Calculate slice boundaries for this part
            var partStart = Math.Max(0, start - currentPos);
            var partEnd = Math.Min(partLength, start + length - currentPos);
            var partSliceLength = partEnd - partStart;

            if (partSliceLength > 0)
            {
                builder.Add(part.Slice(partStart, partSliceLength));
                remaining -= partSliceLength;

                // Early exit if we've collected all required characters
                if (remaining == 0)
                {
                    break;
                }
            }

            currentPos += partLength;
        }

        Debug.Assert(remaining == 0, "Should have collected exactly the requested number of characters");

        return builder.ToContent();
    }
}
