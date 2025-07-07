// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public readonly partial record struct Content
{
    public struct Enumerator(Content content) : IEnumerator<char>
    {
        private AllContentParts.Enumerator _partEnumerator = content.AllParts.GetEnumerator();

        // Current value and index within that value.
        private ReadOnlyMemory<char> _value = default;
        private int _index = -1;

        public readonly char Current => _value.Span[_index];

        readonly object IEnumerator.Current => Current;

        public void Dispose()
        {
            _partEnumerator.Dispose();

            _value = default;
            _index = -1;
        }

        public bool MoveNext()
        {
            // Advance index within current value
            if (_index + 1 < _value.Length)
            {
                _index++;
                return true;
            }

            // If we reached the end of the current value, try to get the next part
            if (_partEnumerator.MoveNext())
            {
                _value = _partEnumerator.Current;
                _index = 0;

                Debug.Assert(!_value.IsEmpty, "PartEnumerator should not return empty content!");

                return true;
            }

            return false;
        }

        public void Reset()
        {
            _partEnumerator.Reset();
            _index = -1;
        }
    }

    private sealed class EnumeratorImpl : IEnumerator<char>
    {
        private Enumerator _enumerator;

        internal EnumeratorImpl(Content content)
        {
            _enumerator = new Enumerator(content);
        }

        public char Current => _enumerator.Current;

        object IEnumerator.Current => _enumerator.Current;

        public bool MoveNext() => _enumerator.MoveNext();

        public void Reset() => throw new NotSupportedException();

        public void Dispose()
        {
        }
    }
}
