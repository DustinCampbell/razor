// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language;

public readonly partial struct Content
{
    private delegate void AppendValue<T>(ref MemoryBuilder<ReadOnlyMemory<char>> builder, T value);

    private readonly ref struct Replacer<T>
    {
        private readonly ReadOnlySpan<char> _oldValue;
        private readonly StringComparison _comparisonType;
        private readonly T _newValue;
        private readonly AppendValue<T>? _appendValue;

        public Replacer(ReadOnlySpan<char> oldValue, StringComparison comparisonType, T newValue, AppendValue<T>? appendValue)
        {
            _oldValue = oldValue;
            _comparisonType = comparisonType;
            _newValue = newValue;
            _appendValue = appendValue;
        }

        public bool TryReplace(ReadOnlySpan<ReadOnlyMemory<char>> parts, out Content newContent)
        {
            var builder = new MemoryBuilder<ReadOnlyMemory<char>>(initialCapacity: parts.Length * 2);

            try
            {
                Debug.Assert(!parts.IsEmpty);
                var partIndex = 0;
                var hasReplacements = false;
                ReadOnlyMemory<char> part = default; // Start with empty part

                while (partIndex < parts.Length || !part.IsEmpty)
                {
                    // Get the next part to process if we don't have one
                    if (part.IsEmpty)
                    {
                        part = parts[partIndex];
                    }

                    // Try matching entirely within the current part first.
                    // Note that we loop in case there are multiple matches in the same part.
                    while (!part.IsEmpty && part.Length >= _oldValue.Length)
                    {
                        var index = part.Span.IndexOf(_oldValue, _comparisonType);

                        if (index < 0)
                        {
                            break;
                        }

                        // We have a match.
                        hasReplacements = true;

                        if (index > 0)
                        {
                            // There's a portion before the match. Add that first.
                            builder.Append(part[..index]);
                        }

                        // Add the replacement value.
                        _appendValue?.Invoke(ref builder, _newValue);

                        // Move past the matched portion.
                        part = part[(index + _oldValue.Length)..];
                    }

                    // Now try matching across parts if we haven't exhausted the current part
                    var foundCrossPartMatch = false;
                    if (!part.IsEmpty)
                    {
                        // See if we can match across parts.
                        var maxPrefixLength = Math.Min(part.Length, _oldValue.Length - 1);

                        for (var prefixLength = maxPrefixLength; prefixLength >= 1; prefixLength--)
                        {
                            var prefix = _oldValue[..prefixLength];

                            if (part.Span.EndsWith(prefix, _comparisonType))
                            {
                                // It matches the end of this part. See if we can match the rest
                                var remainingValue = _oldValue[prefixLength..];
                                var remainingParts = parts[(partIndex + 1)..];

                                if (TryMatchAcrossParts(remainingValue, remainingParts, _comparisonType))
                                {
                                    // We have a match across parts.
                                    hasReplacements = true;
                                    foundCrossPartMatch = true;

                                    // Add the portion before the match
                                    var beforeMatch = part[..^prefixLength];
                                    if (!beforeMatch.IsEmpty)
                                    {
                                        builder.Append(beforeMatch);
                                    }

                                    // Add the replacement value
                                    _appendValue?.Invoke(ref builder, _newValue);

                                    // Skip the parts that were matched
                                    var charsToSkip = remainingValue.Length;
                                    partIndex++; // Move to next part

                                    while (charsToSkip > 0 && partIndex < parts.Length)
                                    {
                                        var nextPart = parts[partIndex];

                                        if (nextPart.Length <= charsToSkip)
                                        {
                                            // Skip the entire part
                                            charsToSkip -= nextPart.Length;
                                            partIndex++;
                                        }
                                        else
                                        {
                                            // Partial skip - continue processing remaining portion of this part
                                            part = nextPart[charsToSkip..];
                                            // Continue the loop with the remaining content
                                            break;
                                        }
                                    }

                                    // If we've consumed all characters to skip, we don't have a current part
                                    if (charsToSkip == 0)
                                    {
                                        part = default;
                                    }

                                    break; // Found a match, break out of prefix length loop
                                }
                            }
                        }
                    }

                    if (!foundCrossPartMatch)
                    {
                        // No cross-part match found - add remaining part and advance
                        if (!part.IsEmpty)
                        {
                            builder.Append(part);
                        }

                        part = default; // Clear current part
                        partIndex++;
                    }
                }

                newContent = hasReplacements ? builder.ToContent() : default;
                return hasReplacements;
            }
            finally
            {
                builder.Dispose();
            }
        }
    }

    private static class Replacer
    {
        public static Replacer<Content> ForContent(ReadOnlySpan<char> oldValue, Content newValue, StringComparison comparisonType)
        {
            return new(oldValue, comparisonType, newValue, static (ref builder, newValue) =>
            {
                foreach (var part in newValue.Parts.NonEmpty)
                {
                    builder.Append(part);
                }
            });
        }

        public static Replacer<ReadOnlyMemory<char>> ForValue(ReadOnlySpan<char> oldValue, ReadOnlyMemory<char> newValue, StringComparison comparisonType)
            => new(oldValue, comparisonType, newValue, static (ref builder, newValue) => builder.Append(newValue));

        public static Replacer<ReadOnlyMemory<char>> ForEmpty(ReadOnlySpan<char> oldValue, StringComparison comparisonType)
            => new(oldValue, comparisonType, ReadOnlyMemory<char>.Empty, appendValue: null);
    }
}
