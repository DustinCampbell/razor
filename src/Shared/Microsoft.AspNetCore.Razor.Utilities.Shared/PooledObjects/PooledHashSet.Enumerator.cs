// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal partial struct PooledHashSet<T>
{
    [NonCopyable]
    public struct Enumerator
    {
        private readonly bool _useEnumerator;
        private readonly HashSet<T>.Enumerator _enumerator;

        private readonly int _inlineCount;
        private readonly T _element0;
        private readonly T _element1;

        private int _index = 0;
        private T _current = default!;

        public Enumerator(in PooledHashSet<T> set)
        {
            if (set._set is { } innerSet)
            {
                _useEnumerator = true;
                _enumerator = innerSet.GetEnumerator();
                _element0 = default!;
                _element1 = default!;
            }
            else
            {
                _inlineCount = set._inlineCount;
                _element0 = set._element0;
                _element1 = set._element1;
            }
        }

        public readonly T Current => _current;

        public bool MoveNext()
        {
            if (_useEnumerator)
            {
                if (!_enumerator.MoveNext())
                {
                    return false;
                }

                _current = _enumerator.Current;
                return true;
            }

            if (_index >= _inlineCount)
            {
                return false;
            }

            _current = _index switch
            {
                0 => _element0,
                1 => _element1,
                _ => Assumed.Unreachable<T>()
            };

            _index++;
            return true;
        }
    }
}
