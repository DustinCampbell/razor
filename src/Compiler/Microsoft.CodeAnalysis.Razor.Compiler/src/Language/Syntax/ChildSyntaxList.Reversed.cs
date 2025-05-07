// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal readonly partial struct ChildSyntaxList
{
    public readonly partial struct Reversed : IEnumerable<SyntaxNodeOrToken>, IEquatable<Reversed>
    {
        private readonly SyntaxNode _node;
        private readonly int _count;

        internal Reversed(SyntaxNode node, int count)
        {
            _node = node;
            _count = count;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_node, _count);
        }

        IEnumerator<SyntaxNodeOrToken> IEnumerable<SyntaxNodeOrToken>.GetEnumerator()
            => _node == null
                ? SpecializedCollections.EmptyEnumerator<SyntaxNodeOrToken>()
                : new EnumeratorImpl(_node, _count);

        IEnumerator IEnumerable.GetEnumerator()
            => _node == null
                ? SpecializedCollections.EmptyEnumerator<SyntaxNodeOrToken>()
                : (IEnumerator)new EnumeratorImpl(_node, _count);

        public bool Equals(Reversed other)
            => _node == other._node &&
               _count == other._count;

        public override bool Equals([NotNullWhen(true)] object? obj)
            => obj is Reversed reversed && Equals(reversed);

        public override int GetHashCode()
        {
            if (_node == null)
            {
                return 0;
            }

            var hash = HashCodeCombiner.Start();
            hash.Add(_node.GetHashCode());
            hash.Add(_count);

            return hash.CombinedHash;
        }

        public struct Enumerator
        {
            private readonly SyntaxNode _node;
            private readonly int _count;
            private int _childIndex;

            internal Enumerator(SyntaxNode node, int count)
            {
                _node = node;
                _count = count;
                _childIndex = count;
            }

            public bool MoveNext()
            {
                return --_childIndex >= 0;
            }

            public readonly SyntaxNodeOrToken Current
                => ItemInternal(_node, _childIndex);

            public void Reset()
            {
                _childIndex = _count;
            }
        }

        private sealed class EnumeratorImpl : IEnumerator<SyntaxNodeOrToken>
        {
            private Enumerator _enumerator;

            internal EnumeratorImpl(SyntaxNode node, int count)
            {
                _enumerator = new Enumerator(node, count);
            }

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <returns>
            /// The element in the collection at the current position of the enumerator.
            /// </returns>
            public SyntaxNodeOrToken Current
                => _enumerator.Current;

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <returns>
            /// The element in the collection at the current position of the enumerator.
            /// </returns>
            object IEnumerator.Current
                => _enumerator.Current;

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created. </exception>
            public bool MoveNext()
                => _enumerator.MoveNext();

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created. </exception>
            public void Reset()
                => _enumerator.Reset();

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
            }
        }
    }
}
