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
    public readonly struct PartList(Content content)
    {
        private readonly Content _content = content;

        /// <summary>
        ///  Gets the total number of flattened parts in the content.
        /// </summary>
        /// <returns>
        ///  The number of individual character sequences after flattening all nested structures.
        ///  Returns 0 for empty content and 1 for single-value content.
        /// </returns>
        public int Count { get; } = content._data.PartCount;

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
        /// <param name="list">The <see cref="PartList"/> to enumerate.</param>
        public ref struct Enumerator(PartList list)
        {
            private Context _context = new(list._content);
            private readonly bool _hasNestedContent = list._content.HasNestedContent;
            private MemoryBuilder<Context> _stack;
            private bool _disposed;

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

                    // Only dispose stack if it was potentially used
                    if (_hasNestedContent)
                    {
                        _stack.Dispose();
                    }
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
            public readonly ReadOnlyMemory<char> Current => _context.Current;

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

                // Fast path: no nested content, use simple iteration
                if (!_hasNestedContent)
                {
                    return !_context.IsEmpty && _context.TryMoveNext();
                }

                // Slow path: handle potential nested content
                return MoveNextNested();
            }

            private bool MoveNextNested()
            {
                while (true)
                {
                    // Try to advance within current content
                    if (_context.TryMoveNext())
                    {
                        // Check if we landed on nested content that needs descent
                        if (_context.TryGetNestedContent(out var nestedContent))
                        {
                            // Push current position and descend into nested content
                            _stack.Push(_context);
                            _context = new(nestedContent);
                            continue;
                        }

                        // Successfully advanced to a regular part
                        return true;
                    }

                    // Current content exhausted, try to pop from stack
                    if (_stack.IsEmpty)
                    {
                        return false;
                    }

                    _context = _stack.Pop();
                }
            }

            private struct Context
            {
                private readonly Content _content;
                private readonly ContentKind _kind;
                private readonly int _partsLength;

                private int _index;

                public Context(Content content)
                {
                    _content = content;
                    _kind = _content._data.Kind;
                    _partsLength = _content._parts?.Length ?? 0;
                    _index = -1;
                }

                public readonly bool IsEmpty => _content.IsEmpty;

                /// <summary>
                ///  Gets the character sequence at the current position.
                /// </summary>
                /// <returns>
                ///  A <see cref="ReadOnlyMemory{T}"/> of characters representing the current part.
                /// </returns>
                /// <remarks>
                ///  This property is only valid when the context represents a valid current position.
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
                            ContentKind.ContentArray => _content.ContentParts[_index]._value,
                            ContentKind.MemoryArray => _content.MemoryParts[_index],
                            ContentKind.StringArray => _content.StringParts[_index].AsMemory(),

                            _ => Assumed.Unreachable<ReadOnlyMemory<char>>()
                        };
                    }
                }

                /// <summary>
                ///  Attempts to advance to the next part within the current content.
                /// </summary>
                /// <returns>
                ///  <see langword="true"/> if the context successfully advanced to the next part;
                ///  <see langword="false"/> if the context has passed the end of the current content.
                /// </returns>
                /// <remarks>
                ///  This method handles basic advancement within the current content level.
                ///  It does not handle nested content structures or stack operations.
                /// </remarks>
                public bool TryMoveNext()
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
                    else if (nextIndex < _partsLength)
                    {
                        Debug.Assert(_content.IsMultiPart);
                        _index = nextIndex;
                        return true;
                    }

                    return false;
                }

                /// <summary>
                ///  Gets nested content at the current index, if it exists and is multi-part.
                /// </summary>
                /// <param name="nestedContent">The nested content if it exists and is multi-part.</param>
                /// <returns>True if nested multi-part content exists at the current index.</returns>
                public readonly bool TryGetNestedContent(out Content nestedContent)
                {
                    if (_kind == ContentKind.ContentArray && _index >= 0 && _index < _partsLength)
                    {
                        var content = _content.ContentParts[_index];
                        
                        if (content.IsMultiPart)
                        {
                            nestedContent = content;
                            return true;
                        }
                    }

                    nestedContent = default;
                    return false;
                }
            }
        }
    }
}
