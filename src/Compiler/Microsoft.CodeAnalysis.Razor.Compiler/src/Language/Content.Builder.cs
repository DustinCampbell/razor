// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language;

public readonly partial struct Content
{
    public ref struct Builder(int initialCapacity, bool flatten = false)
    {
        private MemoryBuilder<Content> _parts = new(initialCapacity, clearArray: true);

        public void Dispose()
        {
            _parts.Dispose();
        }

        public void Add(Content content)
        {
            if (content.IsEmpty)
            {
                return;
            }

            if (!flatten)
            {
                _parts.Append(content);
            }
            else
            {
                foreach (var part in content.Parts.NonEmpty)
                {
                    _parts.Append(part);
                }
            }
        }

        public void Add(ReadOnlyMemory<char> value)
        {
            if (value.IsEmpty)
            {
                return;
            }

            _parts.Append(value);
        }

        public void Add(string value)
        {
            if (value.IsNullOrEmpty())
            {
                return;
            }

            _parts.Append(value);
        }

        internal void Add<T>(T value)
            where T : IWriteableValue
        {
            value.AddTo(ref this);
        }

        internal void Add(IWriteableValue value)
        {
            value.AddTo(ref this);
        }

        public readonly Content ToContent()
            => _parts.ToContent();
    }
}
