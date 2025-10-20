// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language;

public readonly partial struct Content
{
    /// <summary>
    ///  Represents a list of flattened character parts from a <see cref="Content"/> structure.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   <see cref="PartList"/> provides enumeration over the leaf character sequences in a
    ///   potentially nested <see cref="Content"/> structure. Nested content is automatically
    ///   flattened during enumeration, exposing only the actual character data.
    ///  </para>
    ///  <para>
    ///   This type supports efficient foreach enumeration using a ref struct enumerator
    ///   that avoids heap allocations for the common case.
    ///  </para>
    /// </remarks>
    public struct PartList(Content content)
    {
        private readonly Content _content = content;
        private ContentKind? _kind;
        private int? _count;

        private ContentKind Kind => _kind ??= _content._data.Kind;

        /// <summary>
        ///  Gets the total number of flattened parts in the content.
        /// </summary>
        /// <returns>
        ///  The number of individual character sequences after flattening all nested structures.
        ///  Returns 0 for empty content and 1 for single-value content.
        /// </returns>
        public int Count
            => _count ??= _content._parts is null
                ? (_content._value.IsEmpty ? 0 : 1)
                : _content._data.PartCount;

        /// <summary>
        ///  Returns an enumerator that iterates through the flattened parts.
        /// </summary>
        /// <returns>
        ///  An <see cref="Enumerator"/> that can be used to iterate through the parts.
        /// </returns>
        /// <remarks>
        ///  The enumerator is a ref struct that performs depth-first traversal of nested
        ///  content structures, yielding only the leaf character sequences.
        /// </remarks>
        public readonly Enumerator GetEnumerator() => new(this);

        /// <summary>
        ///  Provides efficient enumeration over the flattened parts of a <see cref="PartList"/>.
        /// </summary>
        /// <remarks>
        ///  <para>
        ///   This enumerator is a ref struct that uses stack-based traversal to enumerate
        ///   nested content structures without allocating on the heap. It implements a
        ///   depth-first traversal algorithm that automatically descends into nested
        ///   <see cref="Content"/> objects.
        ///  </para>
        ///  <para>
        ///   The enumerator should be disposed to ensure proper cleanup of internal resources.
        ///   This happens automatically when used in a foreach statement.
        ///  </para>
        /// </remarks>
        public ref struct Enumerator
        {
            private Content _content;
            private int _index = -1;
            private ContentKind _kind;
            private int _partsLength;
            private Content[]? _contentParts;
            private bool _disposed;

            private MemoryBuilder<(Content content, int index, ContentKind kind, int partsLength, Content[]? contentParts)> _stack;

            /// <summary>
            ///  Initializes a new instance of the <see cref="Enumerator"/> struct.
            /// </summary>
            /// <param name="list">The <see cref="PartList"/> to enumerate.</param>
            public Enumerator(PartList list)
            {
                _content = list._content;
                _kind = list.Kind;
                _partsLength = _content._parts?.Length ?? 0;
                _contentParts = _kind == ContentKind.ContentArray ? _content.ContentParts : null;
                _disposed = false;
            }

            /// <summary>
            ///  Releases resources used by the enumerator.
            /// </summary>
            /// <remarks>
            ///  This method disposes the internal stack used for traversing nested content.
            ///  It is called automatically when the enumerator is used in a foreach statement.
            /// </remarks>
            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _stack.Dispose();
                }
            }

            /// <summary>
            ///  Gets the character sequence at the current position of the enumerator.
            /// </summary>
            /// <returns>
            ///  A <see cref="ReadOnlyMemory{T}"/> of characters representing the current part.
            /// </returns>
            /// <remarks>
            ///  This property is only valid after <see cref="MoveNext"/> has returned <see langword="true"/>
            ///  and before it returns false or <see cref="Dispose"/> is called.
            /// </remarks>
            public readonly ReadOnlyMemory<char> Current
            {
                get
                {
                    if (_content.HasValue)
                    {
                        Debug.Assert(_index == 0);
                        return _content.Value;
                    }

                    Debug.Assert(_content.IsMultiPart);
                    Debug.Assert(_index >= 0 && _index < _partsLength);

                    return _kind switch
                    {
                        ContentKind.ContentArray => _contentParts![_index]._value,
                        ContentKind.MemoryArray => _content.MemoryParts[_index],
                        ContentKind.StringArray => _content.StringParts[_index].AsMemory(),

                        _ => Assumed.Unreachable<ReadOnlyMemory<char>>()
                    };
                }
            }

            /// <summary>
            ///  Advances the enumerator to the next part in the sequence.
            /// </summary>
            /// <returns>
            ///  <see langword="true"/> if the enumerator successfully advanced to the next part;
            ///  <see langword="false"/> if the enumerator has passed the end of the sequence.
            /// </returns>
            /// <remarks>
            ///  <para>
            ///   This method performs depth-first traversal of nested content structures.
            ///   When it encounters a nested <see cref="Content"/> object, it descends into
            ///   it automatically, maintaining a stack of positions to resume at parent levels.
            ///  </para>
            ///  <para>
            ///   After this method returns false, the <see cref="Current"/> property is undefined.
            ///  </para>
            /// </remarks>
            public bool MoveNext()
            {
                if (_disposed)
                {
                    return false;
                }

                while (true)
                {
                    var nextIndex = _index + 1;

                    if (_content.HasValue)
                    {
                        if (nextIndex == 0)
                        {
                            _index = 0;
                            return true;
                        }
                    }
                    else if (_content.IsMultiPart)
                    {
                        if (_kind == ContentKind.ContentArray)
                        {
                            if (nextIndex < _partsLength)
                            {
                                Debug.Assert(_contentParts is not null);

                                ref readonly var content = ref _contentParts[nextIndex];

                                if (content.IsMultiPart)
                                {
                                    // Push current position and descend into nested content
                                    _stack.Push((_content, nextIndex, _kind, _partsLength, _contentParts));
                                    _content = content;
                                    _kind = content._data.Kind;
                                    _partsLength = content._parts!.Length;
                                    _contentParts = _kind == ContentKind.ContentArray ? content.ContentParts : null;
                                    _index = -1;
                                    continue;
                                }

                                _index = nextIndex;
                                return true;
                            }
                        }
                        else if (nextIndex < _partsLength)
                        {
                            _index = nextIndex;
                            return true;
                        }
                    }

                    // Pop from stack if available
                    if (!_stack.IsEmpty)
                    {
                        (_content, _index, _kind, _partsLength, _contentParts) = _stack.Pop();
                        continue;
                    }

                    return false;
                }
            }
        }
    }
}
