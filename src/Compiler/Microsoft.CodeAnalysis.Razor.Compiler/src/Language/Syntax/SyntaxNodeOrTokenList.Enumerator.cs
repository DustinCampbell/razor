// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal readonly partial struct SyntaxNodeOrTokenList
{
    public struct Enumerator : IEnumerator<SyntaxNodeOrToken>
    {
        private readonly SyntaxNodeOrTokenList _list;
        private int _index;

        internal Enumerator(ref readonly SyntaxNodeOrTokenList list)
        {
            _list = list;
            _index = -1;
        }

        public readonly void Dispose()
        {
        }

        public readonly SyntaxNodeOrToken Current => _list[_index];

        readonly object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_index < _list.Count)
            {
                _index++;
            }

            return _index < _list.Count;
        }

        public readonly void Reset()
            => throw new NotSupportedException();

        public override readonly bool Equals([NotNullWhen(true)] object? obj)
            => throw new NotSupportedException();

        public override readonly int GetHashCode()
            => throw new NotSupportedException();
    }
}
