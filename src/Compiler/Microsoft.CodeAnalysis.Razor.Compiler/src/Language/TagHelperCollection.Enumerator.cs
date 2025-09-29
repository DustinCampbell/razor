// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperCollection
{
    public ref struct Enumerator(TagHelperCollection collection)
    {
        private int _index = -1;

        public readonly TagHelperDescriptor Current => collection[_index];

        public bool MoveNext()
        {
            if (_index < collection.Count - 1)
            {
                _index++;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            _index = -1;
        }

        public void Dispose()
        {
            Reset();
        }
    }

    private sealed class EnumeratorImpl(TagHelperCollection collection) : IEnumerator<TagHelperDescriptor>
    {
        private int _index = -1;

        public TagHelperDescriptor Current => collection[_index];

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_index < collection.Count - 1)
            {
                _index++;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            _index = -1;
        }

        public void Dispose()
        {
            _index = -1;
        }
    }
}
