// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

public readonly partial struct Content
{
    /// <summary>
    ///  Creates a new <see cref="Content"/> with the specified value inserted at the given position.
    /// </summary>
    /// <param name="startIndex">The zero-based index position where the insertion begins.</param>
    /// <param name="value">The value to insert.</param>
    /// <returns>
    ///  A new <see cref="Content"/> that contains the original content with <paramref name="value"/>
    ///  inserted at <paramref name="startIndex"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="startIndex"/> is less than zero or greater than the length of this instance.
    /// </exception>
    /// <remarks>
    ///  This method efficiently creates a new content structure by slicing the original content
    ///  without copying the underlying character data. The result is a multi-part content with
    ///  up to three parts: content before the insertion point, the inserted value, and content
    ///  after the insertion point.
    /// </remarks>
    public Content Insert(int startIndex, Content value)
    {
        ArgHelper.ThrowIfNegative(startIndex);
        ArgHelper.ThrowIfGreaterThan(startIndex, Length);

        // Fast path: single value content
        if (value.IsSingleValue)
        {
            return InsertValue(startIndex, value._value);
        }

        if (value.IsEmpty)
        {
            return this;
        }

        if (IsEmpty)
        {
            return value;
        }

        if (startIndex == 0)
        {
            // Insert at the beginning: prepend the new value.
            // If possible, try to keep the content flattened and remove empty parts.
            return IsSingleValue
                ? value.IsSingleValue
                    ? new Content([value._value, _value])
                    : new Content([.. value.Parts.NonEmpty, _value])
                : value.IsSingleValue
                    ? new Content([value, .. Parts])
                    : new Content([.. value.Parts.NonEmpty, .. Parts.NonEmpty]);
        }

        if (startIndex == Length)
        {
            // Insert at the end: append the new value.
            // If possible, try to keep the content flattened and remove empty parts.
            return IsSingleValue
                ? value.IsSingleValue
                    ? new Content([_value, value._value])
                    : new Content([_value, .. value.Parts.NonEmpty])
                : value.IsSingleValue
                    ? new Content([.. Parts.NonEmpty, value._value])
                    : new Content([.. Parts.NonEmpty, .. value.Parts.NonEmpty]);
        }

        // Fast path: single value content
        if (IsSingleValue)
        {
            return new([_value[..startIndex], value, _value[startIndex..]]);
        }

        // Multi-part: walk through parts once, finding the insertion point
        return value.IsSingleValue
            ? InsertValueIntoMultiPart(startIndex, value._value)
            : InsertValueIntoMultiPart(startIndex, value);
    }

    /// <summary>
    ///  Creates a new <see cref="Content"/> with the specified value inserted at the given position.
    /// </summary>
    /// <param name="startIndex">The zero-based index position where the insertion begins.</param>
    /// <param name="value">The value to insert.</param>
    /// <returns>
    ///  A new <see cref="Content"/> that contains the original content with <paramref name="value"/>
    ///  inserted at <paramref name="startIndex"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="startIndex"/> is less than zero or greater than the length of this instance.
    /// </exception>
    /// <remarks>
    ///  This method efficiently creates a new content structure by slicing the original content
    ///  without copying the underlying character data. The result is a multi-part content with
    ///  up to three parts: content before the insertion point, the inserted value, and content
    ///  after the insertion point.
    /// </remarks>
    public Content Insert(int startIndex, ReadOnlyMemory<char> value)
    {
        ArgHelper.ThrowIfNegative(startIndex);
        ArgHelper.ThrowIfGreaterThan(startIndex, Length);

        return InsertValue(startIndex, value);
    }

    private Content InsertValue(int startIndex, ReadOnlyMemory<char> value)
    {
        if (value.IsEmpty)
        {
            return this;
        }

        if (IsEmpty)
        {
            return new Content(value);
        }

        if (startIndex == 0)
        {
            // Insert at the beginning: prepend the new value
            // If possible, try to keep the content flattened and remove empty parts.
            return IsSingleValue
                ? new Content([value, _value])
                : new Content([value, .. Parts.NonEmpty]);
        }

        if (startIndex == Length)
        {
            // Insert at the end: append the new value
            // If possible, try to keep the content flattened and remove empty parts.
            return IsSingleValue
                ? new Content([_value, value])
                : new Content([.. Parts.NonEmpty, value]);
        }

        // Fast path: single value content
        if (IsSingleValue)
        {
            return new([_value[..startIndex], value, _value[startIndex..]]);
        }

        // Multi-part: walk through parts once, finding the insertion point
        return InsertValueIntoMultiPart(startIndex, value);
    }

    private Content InsertValueIntoMultiPart(int startIndex, Content value)
    {
        Debug.Assert(value.IsMultiPart);
        Debug.Assert(IsMultiPart);
        Debug.Assert(startIndex > 0 && startIndex < Length);

        var parts = Parts;

        using var builder = new PooledArrayBuilder<Content>(capacity: parts.Count + 2);
        var currentPos = 0;
        var inserted = false;

        foreach (var part in parts.NonEmpty)
        {
            var partLength = part.Length;

            if (!inserted && currentPos + partLength >= startIndex)
            {
                // This part contains the insertion point
                var offsetInPart = startIndex - currentPos;

                if (offsetInPart > 0)
                {
                    // Add the portion before the insertion point
                    builder.Add(part[..offsetInPart]);
                }

                // Add the inserted value
                builder.Add(value);

                if (offsetInPart < partLength)
                {
                    // Add the portion after the insertion point
                    builder.Add(part[offsetInPart..]);
                }

                inserted = true;
            }
            else
            {
                // Add the part as-is
                builder.Add(part);
            }

            currentPos += partLength;
        }

        Debug.Assert(inserted, "Should have found insertion point");

        return builder.ToContent();
    }

    private Content InsertValueIntoMultiPart(int startIndex, ReadOnlyMemory<char> value)
    {
        Debug.Assert(IsMultiPart);
        Debug.Assert(startIndex > 0 && startIndex < Length);

        var parts = Parts;

        using var builder = new PooledArrayBuilder<ReadOnlyMemory<char>>(capacity: parts.Count + 2);
        var currentPos = 0;
        var inserted = false;

        foreach (var part in parts.NonEmpty)
        {
            var partLength = part.Length;

            if (!inserted && currentPos + partLength >= startIndex)
            {
                // This part contains the insertion point
                var offsetInPart = startIndex - currentPos;

                if (offsetInPart > 0)
                {
                    // Add the portion before the insertion point
                    builder.Add(part[..offsetInPart]);
                }

                // Add the inserted value
                builder.Add(value);

                if (offsetInPart < partLength)
                {
                    // Add the portion after the insertion point
                    builder.Add(part[offsetInPart..]);
                }

                inserted = true;
            }
            else
            {
                // Add the part as-is
                builder.Add(part);
            }

            currentPos += partLength;
        }

        Debug.Assert(inserted, "Should have found insertion point");

        return builder.ToContent();
    }

    /// <summary>
    ///  Creates a new <see cref="Content"/> with the specified string inserted at the given position.
    /// </summary>
    /// <param name="startIndex">The zero-based index position where the insertion begins.</param>
    /// <param name="value">The string to insert.</param>
    /// <returns>
    ///  A new <see cref="Content"/> that contains the original content with <paramref name="value"/>
    ///  inserted at <paramref name="startIndex"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="startIndex"/> is less than zero or greater than the length of this instance.
    /// </exception>
    /// <remarks>
    ///  This method efficiently creates a new content structure by slicing the original content
    ///  without copying the underlying character data. The result is a multi-part content with
    ///  up to three parts: content before the insertion point, the inserted value, and content
    ///  after the insertion point.
    /// </remarks>
    public Content Insert(int startIndex, string? value)
        => Insert(startIndex, value.AsMemory());

    /// <summary>
    ///  Creates a new <see cref="Content"/> with a specified number of characters removed starting
    ///  at a specified position.
    /// </summary>
    /// <param name="startIndex">The zero-based position to begin deleting characters.</param>
    /// <param name="count">The number of characters to delete.</param>
    /// <returns>
    ///  A new <see cref="Content"/> that is equivalent to this instance except for the removed characters.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="startIndex"/> or <paramref name="count"/> is less than zero, or
    ///  <paramref name="startIndex"/> + <paramref name="count"/> is greater than the length of this instance.
    /// </exception>
    /// <remarks>
    ///  This method efficiently creates a new content structure by slicing the original content
    ///  without copying the underlying character data. The result is either a single slice or
    ///  a multi-part content with two parts: content before the removal point and content after.
    /// </remarks>
    public Content Remove(int startIndex, int count)
    {
        ArgHelper.ThrowIfNegative(startIndex);
        ArgHelper.ThrowIfNegative(count);
        ArgHelper.ThrowIfGreaterThan(startIndex + count, Length);

        if (count == 0)
        {
            return this;
        }

        if (count == Length)
        {
            return Empty;
        }

        // If we're removing from the front, we can just slice.
        if (startIndex == 0)
        {
            return this[count..];
        }

        var endIndex = startIndex + count;

        // Likewise, if we're remove from the back, we can just slice.
        if (endIndex == Length)
        {
            return this[..startIndex];
        }

        // Fast path: single value content
        if (IsSingleValue)
        {
            return new([_value[..startIndex], _value[endIndex..]]);
        }

        // Multi-part: walk through parts once, removing the specified range
        return RemoveMultiPart(startIndex, endIndex);
    }

    private Content RemoveMultiPart(int startIndex, int endIndex)
    {
        Debug.Assert(IsMultiPart);
        Debug.Assert(startIndex >= 0 && endIndex <= Length);
        Debug.Assert(startIndex < endIndex);

        var parts = Parts;

        using var builder = new PooledArrayBuilder<ReadOnlyMemory<char>>(capacity: parts.Count);

        var currentPos = 0;

        foreach (var part in parts.NonEmpty)
        {
            var partLength = part.Length;
            var partStart = currentPos;
            var partEnd = currentPos + partLength;

            // Determine what portion of this part to keep
            if (partEnd <= startIndex || partStart >= endIndex)
            {
                // Part is entirely before or after the removed range
                builder.Add(part);
            }
            else
            {
                // Part overlaps with the removed range
                var keepStart = Math.Max(0, startIndex - partStart);
                var keepEnd = Math.Min(partLength, endIndex - partStart);

                // Add portion before the removed range
                if (keepStart > 0)
                {
                    builder.Add(part[..keepStart]);
                }

                // Add portion after the removed range
                if (keepEnd < partLength)
                {
                    builder.Add(part[keepEnd..]);
                }
            }

            currentPos += partLength;
        }

        return builder.ToContent();
    }

    /// <summary>
    ///  Creates a new <see cref="Content"/> in which all occurrences of a specified value are replaced
    ///  with another specified value.
    /// </summary>
    /// <param name="oldValue">The value to be replaced.</param>
    /// <param name="newValue">The value to replace all occurrences of <paramref name="oldValue"/>.</param>
    /// <returns>
    ///  A new <see cref="Content"/> that is equivalent to this instance except that all instances of
    ///  <paramref name="oldValue"/> are replaced with <paramref name="newValue"/>.
    ///  If <paramref name="oldValue"/> is not found in the current instance, the method returns
    ///  the current instance unchanged.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///  <paramref name="oldValue"/> is empty.
    /// </exception>
    /// <remarks>
    ///  <para>
    ///   This method efficiently creates a new content structure by slicing the original content
    ///   without copying the underlying character data. The result is a multi-part content where
    ///   each part is either a slice of the original content or the replacement value.
    ///  </para>
    ///  <para>
    ///   This method uses ordinal (case-sensitive and culture-insensitive) comparison to find occurrences
    ///   of <paramref name="oldValue"/>.
    ///  </para>
    /// </remarks>
    public Content Replace(ReadOnlySpan<char> oldValue, Content newValue)
        => Replace(oldValue, newValue, StringComparison.Ordinal);

    /// <summary>
    ///  Creates a new <see cref="Content"/> in which all occurrences of a specified value are replaced
    ///  with another specified value using the specified comparison type.
    /// </summary>
    /// <param name="oldValue">The value to be replaced.</param>
    /// <param name="newValue">The value to replace all occurrences of <paramref name="oldValue"/>.</param>
    /// <param name="comparisonType">The string comparison type to use.</param>
    /// <returns>
    ///  A new <see cref="Content"/> that is equivalent to this instance except that all instances of
    ///  <paramref name="oldValue"/> are replaced with <paramref name="newValue"/>.
    ///  If <paramref name="oldValue"/> is not found in the current instance, the method returns
    ///  the current instance unchanged.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///  <paramref name="oldValue"/> is empty.
    /// </exception>
    /// <remarks>
    ///  This method efficiently creates a new content structure by slicing the original content
    ///  without copying the underlying character data. The result is a multi-part content where
    ///  each part is either a slice of the original content or the replacement value.
    /// </remarks>
    public Content Replace(ReadOnlySpan<char> oldValue, Content newValue, StringComparison comparisonType)
    {
        if (oldValue.IsEmpty)
        {
            return ThrowHelper.ThrowArgumentException<Content>(nameof(oldValue), "The value to be replaced cannot be empty.");
        }

        if (Length == 0 || oldValue.Length > Length)
        {
            return this;
        }

        if (newValue.IsEmpty)
        {
            return ReplaceCore(Replacer.ForEmpty(oldValue, comparisonType));
        }

        if (newValue.IsSingleValue)
        {
            return ReplaceCore(Replacer.ForValue(oldValue, newValue._value, comparisonType));
        }

        return ReplaceCore(Replacer.ForContent(oldValue, newValue, comparisonType));
    }

    /// <summary>
    ///  Creates a new <see cref="Content"/> in which all occurrences of a specified value are replaced
    ///  with another specified value.
    /// </summary>
    /// <param name="oldValue">The value to be replaced.</param>
    /// <param name="newValue">The value to replace all occurrences of <paramref name="oldValue"/>.</param>
    /// <returns>
    ///  A new <see cref="Content"/> that is equivalent to this instance except that all instances of
    ///  <paramref name="oldValue"/> are replaced with <paramref name="newValue"/>.
    ///  If <paramref name="oldValue"/> is not found in the current instance, the method returns
    ///  the current instance unchanged.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///  <paramref name="oldValue"/> is empty.
    /// </exception>
    /// <remarks>
    ///  <para>
    ///   This method efficiently creates a new content structure by slicing the original content
    ///   without copying the underlying character data. The result is a multi-part content where
    ///   each part is either a slice of the original content or the replacement value.
    ///  </para>
    ///  <para>
    ///   This method uses ordinal (case-sensitive and culture-insensitive) comparison to find occurrences
    ///   of <paramref name="oldValue"/>.
    ///  </para>
    /// </remarks>
    public Content Replace(ReadOnlySpan<char> oldValue, ReadOnlyMemory<char> newValue)
        => Replace(oldValue, newValue, StringComparison.Ordinal);

    /// <summary>
    ///  Creates a new <see cref="Content"/> in which all occurrences of a specified value are replaced
    ///  with another specified value using the specified comparison type.
    /// </summary>
    /// <param name="oldValue">The value to be replaced.</param>
    /// <param name="newValue">The value to replace all occurrences of <paramref name="oldValue"/>.</param>
    /// <param name="comparisonType">The string comparison type to use.</param>
    /// <returns>
    ///  A new <see cref="Content"/> that is equivalent to this instance except that all instances of
    ///  <paramref name="oldValue"/> are replaced with <paramref name="newValue"/>.
    ///  If <paramref name="oldValue"/> is not found in the current instance, the method returns
    ///  the current instance unchanged.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///  <paramref name="oldValue"/> is empty.
    /// </exception>
    /// <remarks>
    ///  This method efficiently creates a new content structure by slicing the original content
    ///  without copying the underlying character data. The result is a multi-part content where
    ///  each part is either a slice of the original content or the replacement value.
    /// </remarks>
    public Content Replace(ReadOnlySpan<char> oldValue, ReadOnlyMemory<char> newValue, StringComparison comparisonType)
    {
        if (oldValue.IsEmpty)
        {
            return ThrowHelper.ThrowArgumentException<Content>(nameof(oldValue), "The value to be replaced cannot be empty.");
        }

        if (Length == 0 || oldValue.Length > Length)
        {
            return this;
        }

        if (newValue.IsEmpty)
        {
            return ReplaceCore(Replacer.ForEmpty(oldValue, comparisonType));
        }

        return ReplaceCore(Replacer.ForValue(oldValue, newValue, comparisonType));
    }

    private Content ReplaceCore<T>(Replacer<T> replacer)
    {
        using var _ = Parts.NonEmpty.AsPooledSpan(out var parts);

        return replacer.TryReplace(parts, out var newContent)
            ? newContent
            : this;
    }

    /// <summary>
    ///  Creates a new <see cref="Content"/> in which all occurrences of a specified string are replaced
    ///  with another specified string.
    /// </summary>
    /// <param name="oldValue">The string to be replaced.</param>
    /// <param name="newValue">The string to replace all occurrences of <paramref name="oldValue"/>.</param>
    /// <returns>
    ///  A new <see cref="Content"/> that is equivalent to this instance except that all instances of
    ///  <paramref name="oldValue"/> are replaced with <paramref name="newValue"/>.
    ///  If <paramref name="oldValue"/> is not found in the current instance, the method returns
    ///  the current instance unchanged.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///  <paramref name="oldValue"/> is empty.
    /// </exception>
    /// <remarks>
    ///  <para>
    ///   This method efficiently creates a new content structure by slicing the original content
    ///   without copying the underlying character data. The result is a multi-part content where
    ///   each part is either a slice of the original content or the replacement value.
    ///  </para>
    ///  <para>
    ///   This method uses ordinal (case-sensitive and culture-insensitive) comparison to find occurrences
    ///   of <paramref name="oldValue"/>.
    ///  </para>
    /// </remarks>
    public Content Replace(string oldValue, string? newValue)
        => Replace(oldValue.AsSpan(), newValue.AsMemory(), StringComparison.Ordinal);

    /// <summary>
    ///  Creates a new <see cref="Content"/> in which all occurrences of a specified string are replaced
    ///  with another specified string using the specified comparison type.
    /// </summary>
    /// <param name="oldValue">The string to be replaced.</param>
    /// <param name="newValue">The string to replace all occurrences of <paramref name="oldValue"/>.</param>
    /// <param name="comparison">The string comparison type to use.</param>
    /// <returns>
    ///  A new <see cref="Content"/> that is equivalent to this instance except that all instances of
    ///  <paramref name="oldValue"/> are replaced with <paramref name="newValue"/>.
    ///  If <paramref name="oldValue"/> is not found in the current instance, the method returns
    ///  the current instance unchanged.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///  <paramref name="oldValue"/> is empty.
    /// </exception>
    /// <remarks>
    ///  This method efficiently creates a new content structure by slicing the original content
    ///  without copying the underlying character data. The result is a multi-part content where
    ///  each part is either a slice of the original content or the replacement value.
    /// </remarks>
    public Content Replace(string oldValue, string? newValue, StringComparison comparison)
        => Replace(oldValue.AsSpan(), newValue.AsMemory(), comparison);

    /// <summary>
    ///  Creates a new <see cref="Content"/> in which all occurrences of a specified character are replaced
    ///  with another specified character.
    /// </summary>
    /// <param name="oldValue">The character to be replaced.</param>
    /// <param name="newValue">The character to replace all occurrences of <paramref name="oldValue"/>.</param>
    /// <returns>
    ///  A new <see cref="Content"/> that is equivalent to this instance except that all instances of
    ///  <paramref name="oldValue"/> are replaced with <paramref name="newValue"/>.
    ///  If <paramref name="oldValue"/> is not found in the current instance, the method returns
    ///  the current instance unchanged.
    /// </returns>
    /// <remarks>
    ///  <para>
    ///   This method efficiently creates a new content structure by slicing the original content
    ///   without copying the underlying character data. The result is a multi-part content where
    ///   each part is either a slice of the original content or the replacement value.
    ///  </para>
    ///  <para>
    ///   This method uses ordinal (case-sensitive and culture-insensitive) comparison to find occurrences
    ///   of <paramref name="oldValue"/>.
    ///  </para>
    /// </remarks>
    public Content Replace(char oldValue, char newValue)
    {
        if (IsEmpty)
        {
            return this;
        }

        if (oldValue == newValue)
        {
            return this;
        }

        var replacer = new CharReplacer(oldValue, newValue);
        Content newContent;

        // Fast path: single value content
        if (IsSingleValue)
        {
            return replacer.TryReplace(_value, out newContent)
                ? newContent
                : this;
        }

        // Multi-part content: walk through parts, replacing characters
        return replacer.TryReplace(this, out newContent)
            ? newContent
            : this;
    }
}
