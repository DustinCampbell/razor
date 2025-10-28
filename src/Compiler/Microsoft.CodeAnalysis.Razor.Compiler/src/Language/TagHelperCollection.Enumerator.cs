// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperCollection
{
    // Generic state container that can hold different enumeration states
    private protected struct EnumerationState
    {
        public int Index;           // Primary index (global position)
        public int SegmentIndex;    // Current segment/collection index  
        public int LocalIndex;      // Local position within current segment
        public int Offset;          // Running offset for calculations

        public EnumerationState()
        {
            Index = -1;
            SegmentIndex = 0;
            LocalIndex = -1;
            Offset = 0;
        }

        public void Reset()
        {
            Index = -1;
            SegmentIndex = 0;
            LocalIndex = -1;
            Offset = 0;
        }
    }

    public ref struct Enumerator
    {
        private readonly TagHelperCollection _collection;
        private EnumerationState _state;
        private TagHelperDescriptor? _current;

        internal Enumerator(TagHelperCollection collection)
        {
            _collection = collection;
            _state = new EnumerationState();
            _current = null;
        }

        public readonly TagHelperDescriptor Current => _current!;

        public bool MoveNext()
        {
            return _collection.TryMoveNext(ref _state, out _current);
        }

        public void Reset()
        {
            _state.Reset();
            _current = null;
        }

        public void Dispose()
        {
            Reset();
        }
    }

    private sealed class EnumeratorImpl : IEnumerator<TagHelperDescriptor>
    {
        private readonly TagHelperCollection _collection;
        private EnumerationState _state;
        private TagHelperDescriptor? _current;

        internal EnumeratorImpl(TagHelperCollection collection)
        {
            _collection = collection;
            _state = new EnumerationState();
            _current = null;
        }

        public TagHelperDescriptor Current => _current!;

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            return _collection.TryMoveNext(ref _state, out _current);
        }

        public void Reset()
        {
            _state.Reset();
            _current = null!;
        }

        public void Dispose()
        {
            Reset();
        }
    }
}
