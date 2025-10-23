// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language;

public readonly partial struct Content
{
    private ref struct CharReplacer(char oldValue, char newValue)
    {
        private readonly char _oldValue = oldValue;
        private readonly char _newValue = newValue;

        private char[]? _cell;

        private void AppendNewValue(ref MemoryBuilder<ReadOnlyMemory<char>> builder)
        {
            _cell ??= [_newValue];
            builder.Append(_cell);
        }

        public bool TryReplace(ReadOnlyMemory<char> value, out Content newContent)
        {
            var builder = new MemoryBuilder<ReadOnlyMemory<char>>();
            try
            {
                if (TryReplaceInPart(value, ref builder))
                {
                    newContent = builder.ToContent();
                    return true;
                }

                newContent = default;
                return false;
            }
            finally
            {
                builder.Dispose();
            }
        }

        public bool TryReplace(Content content, out Content newContent)
        {
            if (content.IsEmpty)
            {
                newContent = default;
                return false;
            }

            if (content.IsSingleValue)
            {
                return TryReplace(content._value, out newContent);
            }

            Debug.Assert(content.IsMultiPart);

            var builder = new MemoryBuilder<ReadOnlyMemory<char>>(content._data.PartCount * 2);
            try
            {
                var hasReplacements = false;

                foreach (var part in content.Parts.NonEmpty)
                {
                    if (TryReplaceInPart(part, ref builder))
                    {
                        hasReplacements = true;
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

        private bool TryReplaceInPart(ReadOnlyMemory<char> part, ref MemoryBuilder<ReadOnlyMemory<char>> builder)
        {
            var span = part.Span;
            var firstIndex = span.IndexOf(_oldValue);

            if (firstIndex < 0)
            {
                // No occurrences in this memory - add it as is.
                builder.Append(part);
                return false;
            }

            // We found at least one occurrence in this memory
            var lastIndex = 0;

            do
            {
                // Add the portion before the match
                if (firstIndex > lastIndex)
                {
                    builder.Append(part[lastIndex..firstIndex]);
                }

                // Add the replacement character
                AppendNewValue(ref builder);

                lastIndex = firstIndex + 1;

                // Find the next occurrence
                firstIndex = span[lastIndex..].IndexOf(_oldValue);
                if (firstIndex >= 0)
                {
                    firstIndex += lastIndex; // Adjust to absolute position
                }
            }
            while (firstIndex >= 0);

            // Add any remaining portion after the last match
            if (lastIndex < span.Length)
            {
                builder.Append(part[lastIndex..]);
            }

            return true;
        }
    }
}
