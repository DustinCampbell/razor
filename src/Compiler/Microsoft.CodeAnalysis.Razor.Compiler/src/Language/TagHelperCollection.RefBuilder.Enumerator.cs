// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperCollection
{

    public ref partial struct RefBuilder
    {
        public ref struct Enumerator(RefBuilder builder)
        {
            private readonly RefBuilder _builder = builder;
            private int _index = -1;

            public readonly TagHelperDescriptor Current => _builder[_index];

            public bool MoveNext()
            {
                if (_index < _builder.Count - 1)
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
    }
}
