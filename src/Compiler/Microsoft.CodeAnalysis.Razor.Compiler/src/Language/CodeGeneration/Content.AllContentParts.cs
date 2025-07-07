// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public readonly partial record struct Content
{
    public readonly struct AllContentParts(Content content) : IEnumerable<ReadOnlyMemory<char>>
    {
        public Enumerator GetEnumerator()
            => new(content);

        IEnumerator<ReadOnlyMemory<char>> IEnumerable<ReadOnlyMemory<char>>.GetEnumerator()
            => new EnumeratorImpl(content);

        IEnumerator IEnumerable.GetEnumerator()
            => new EnumeratorImpl(content);

        public struct Enumerator(Content content) : IEnumerator<ReadOnlyMemory<char>>
        {
            // Hold onto the original content to make it possible to reset the enumerator.
            private Content _original = content;

            // Stack to track nested parts: (Array parts, int partIndex)
            private Stack<(Array parts, int partIndex)>? _stack = null;

            // Current parts and index within those parts.
            private Array? _parts = content._parts;
            private int _partIndex = -1;

            private ReadOnlyMemory<char> _value;
            private bool _valueSet;

            public readonly ReadOnlyMemory<char> Current => !_value.IsEmpty ? _value : throw new InvalidOperationException();

            readonly object IEnumerator.Current => Current;

            public void Dispose()
            {
                var stack = Interlocked.Exchange(ref _stack, null);
                if (stack is not null)
                {
                    StackPool<(Array parts, int partIndex)>.Default.Return(stack);
                }

                _original = default;
                _parts = null;
                _partIndex = -1;
                _value = default;
                _valueSet = false;
            }

            public bool MoveNext()
            {
                // Simplest case: if we have no parts, just return the value.
                // And, if the value is empty, we don't bother with it.
                if (_parts is null)
                {
                    if (_valueSet)
                    {
                        return false;
                    }

                    _value = _original._value;
                    _valueSet = true;

                    return !_value.IsEmpty;
                }

                // If we are iterating a parts array, move to next part
                while (_parts is not null)
                {
                    _partIndex++;

                    if (_partIndex == _parts.Length)
                    {
                        // No more parts in this array, pop previous state
                        if (TryPopParts())
                        {
                            continue;
                        }

                        // No more parts in the stack. We're done.
                        return false;
                    }

                    switch (_parts)
                    {
                        case Content[] contentParts:
                            if (_partIndex < contentParts.Length)
                            {
                                // Note: Content arrays are a bit more tricky, because the can contain nested content.

                                var nextContent = contentParts[_partIndex];

                                // If this part has nested parts, push to the stack and try again.
                                if (nextContent._parts is not null)
                                {
                                    PushParts();

                                    _parts = nextContent._parts;
                                    _partIndex = -1;
                                    _value = default;

                                    continue;
                                }

                                // Otherwise, set the value.
                                var next = nextContent._value;

                                if (!next.IsEmpty)
                                {
                                    _value = next;
                                    return true;
                                }

                                // If the next part is empty, we skip it and continue to the next part.
                                continue;
                            }

                            break;

                        case ReadOnlyMemory<char>[] memoryParts:
                            if (_partIndex < memoryParts.Length)
                            {
                                var next = memoryParts[_partIndex];

                                if (!next.IsEmpty)
                                {
                                    _value = next;
                                    return true;
                                }

                                // If the next part is empty, we skip it and continue to the next part.
                                continue;
                            }

                            break;

                        case string[] stringParts:
                            if (_partIndex < stringParts.Length)
                            {
                                var next = stringParts[_partIndex].AsMemory();

                                if (!next.IsEmpty)
                                {
                                    _value = next;
                                    return true;
                                }

                                // If the next part is empty, we skip it and continue to the next part.
                                continue;
                            }

                            break;
                    }
                }

                return false;
            }

            public void Reset()
            {
                // Reset to the original content state and clear the stack (if used)
                _parts = _original._parts;
                _partIndex = -1;
                _value = default;
                _valueSet = false;

                _stack?.Clear();
            }

            private void PushParts()
            {
                if (_parts is null)
                {
                    return;
                }

                _stack ??= StackPool<(Array parts, int partIndex)>.Default.Get();
                _stack.Push((_parts, _partIndex));
            }

            private bool TryPopParts()
            {
                if (_stack is { Count: > 0 } stack)
                {
                    (_parts, _partIndex) = stack.Pop();
                    return true;
                }

                return false;
            }
        }

        private sealed class EnumeratorImpl : IEnumerator<ReadOnlyMemory<char>>
        {
            private Enumerator _enumerator;

            internal EnumeratorImpl(Content content)
            {
                _enumerator = new Enumerator(content);
            }

            public ReadOnlyMemory<char> Current => _enumerator.Current;

            object IEnumerator.Current => _enumerator.Current;

            public bool MoveNext() => _enumerator.MoveNext();

            public void Reset() => throw new NotSupportedException();

            public void Dispose()
            {
            }
        }
    }
}
