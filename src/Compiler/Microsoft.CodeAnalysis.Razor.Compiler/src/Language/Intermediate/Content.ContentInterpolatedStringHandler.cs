// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public readonly partial record struct Content
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [InterpolatedStringHandler]
    public ref struct ContentInterpolatedStringHandler
    {
        private MemoryBuilder<Content> _builder;

        public ContentInterpolatedStringHandler(int literalLength, int formattedCount)
        {
            _builder = new MemoryBuilder<Content>(initialCapacity: Math.Max(formattedCount, 4));
        }

        public Content[] ToArray()
        {
            var result = _builder.AsMemory().ToArray();

            _builder.Dispose();

            return result;
        }

        public void WriteTo(CodeWriter writer)
        {
            foreach (var part in _builder.AsMemory().Span)
            {
                part.WriteTo(writer);
            }

            _builder.Dispose();
        }

        public void AppendLiteral(string value)
        {
            if (value is not null)
            {
                _builder.Append(value);
            }
        }

        public void AppendFormatted<T>(T value)
        {
            if (value is null)
            {
                return;
            }

            switch (value)
            {
                case Content content:
                    _builder.Append(content);
                    break;

                case ReadOnlyMemory<char> memory:
                    _builder.Append(memory);
                    break;

                case string s:
                    _builder.Append(s);
                    break;

                case ImmutableArray<Content> array:
                    _builder.Append(array);
                    break;

                case ImmutableArray<ReadOnlyMemory<char>> array:
                    _builder.Append(array);
                    break;

                case ImmutableArray<string> array:
                    _builder.Append(array);
                    break;

                default:
                    _builder.Append(value.ToString().AsMemory());
                    break;
            }
        }
    }
}
