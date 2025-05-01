// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

/// <summary>
/// Represents a read-only list of <see cref="SyntaxToken"/>.
/// </summary>
[CollectionBuilder(typeof(SyntaxTokenList), methodName: "Create")]
internal readonly partial struct SyntaxTokenList : IEquatable<SyntaxTokenList>, IReadOnlyList<SyntaxToken>
{
    private readonly SyntaxNode? _parent;
    private readonly int _index;

    internal SyntaxTokenList(SyntaxNode? parent, GreenNode? tokenOrList, int position, int index)
    {
        Debug.Assert(tokenOrList != null || (position == 0 && index == 0 && parent == null));
        Debug.Assert(position >= 0);
        Debug.Assert(tokenOrList == null || (tokenOrList.IsToken) || (tokenOrList.IsList));
        _parent = parent;
        Node = tokenOrList;
        Position = position;
        _index = index;
    }

    public SyntaxTokenList(SyntaxToken token)
    {
        _parent = token.Parent;
        Node = token.Node;
        Position = token.Position;
        _index = 0;
    }

    /// <summary>
    /// Creates a list of tokens.
    /// </summary>
    /// <param name="tokens">An array of tokens.</param>
    public SyntaxTokenList(params SyntaxToken[] tokens)
        : this(null, CreateNodeFromSpan(tokens), 0, 0)
    {
    }

    /// <summary>
    /// Creates a list of tokens.
    /// </summary>
    public SyntaxTokenList(IEnumerable<SyntaxToken> tokens)
        : this(null, CreateNode(tokens), 0, 0)
    {
    }

    public static SyntaxTokenList Create(ReadOnlySpan<SyntaxToken> tokens)
        => tokens.Length == 0
            ? default
            : new(parent: null, CreateNodeFromSpan(tokens), position: 0, index: 0);

    private static GreenNode? CreateNodeFromSpan(ReadOnlySpan<SyntaxToken> tokens)
    {
        switch (tokens.Length)
        {
            // Also handles case where tokens is `null`.
            case 0: return null;
            case 1: return tokens[0].Node;
            case 2: return InternalSyntax.SyntaxList.List(tokens[0].Node!, tokens[1].Node!);
            case 3: return InternalSyntax.SyntaxList.List(tokens[0].Node!, tokens[1].Node!, tokens[2].Node!);
            default:
                {
                    var copy = new ArrayElement<GreenNode>[tokens.Length];

                    for (int i = 0, n = tokens.Length; i < n; i++)
                    {
                        copy[i].Value = tokens[i].Node!;
                    }

                    return Syntax.InternalSyntax.SyntaxList.List(copy);
                }
        }
    }

    private static GreenNode? CreateNode(IEnumerable<SyntaxToken> tokens)
    {
        if (tokens == null)
        {
            return null;
        }

        var initialCapacity = tokens.TryGetCount(out var count) ? count : 4;

        using var builder = new MemoryBuilder<SyntaxToken>(initialCapacity);

        foreach (var nodeOrToken in tokens)
        {
            builder.Append(nodeOrToken);
        }

        return CreateNodeFromSpan(builder.AsMemory().Span);
    }

    internal GreenNode? Node { get; }

    internal int Position { get; }

    /// <summary>
    /// Returns the number of tokens in the list.
    /// </summary>
    public int Count => Node == null ? 0 : (Node.IsList ? Node.SlotCount : 1);

    /// <summary>
    /// Gets the token at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the token to get.</param>
    /// <returns>The token at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is less than 0.-or-<paramref name="index" /> is equal to or greater than <see cref="Count" />. </exception>
    public SyntaxToken this[int index]
    {
        get
        {
            if (Node != null)
            {
                if (Node.IsList)
                {
                    if (unchecked((uint)index < (uint)Node.SlotCount))
                    {
                        return new SyntaxToken(Node.GetSlot(index), _parent, Position + Node.GetSlotOffset(index), _index + index);
                    }
                }
                else if (index == 0)
                {
                    return new SyntaxToken(Node, _parent, Position, _index);
                }
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    /// <summary>
    /// The absolute span of the list elements in characters, not including the leading and trailing trivia of the first and last elements.
    /// </summary>
    public TextSpan Span
    {
        get
        {
            if (Node == null)
            {
                return default;
            }

            return TextSpan.FromBounds(Position, Position + Node.Width);
        }
    }

    /// <summary>
    /// Returns the string representation of the tokens in this list, not including 
    /// the first token's leading trivia and the last token's trailing trivia.
    /// </summary>
    /// <returns>
    /// The string representation of the tokens in this list, not including 
    /// the first token's leading trivia and the last token's trailing trivia.
    /// </returns>
    public override string ToString()
    {
        return Node != null ? Node.ToString() : string.Empty;
    }

    /// <summary>
    /// Returns the first token in the list.
    /// </summary>
    /// <returns>The first token in the list.</returns>
    /// <exception cref="InvalidOperationException">The list is empty.</exception>        
    public SyntaxToken First()
    {
        if (Any())
        {
            return this[0];
        }

        throw new InvalidOperationException();
    }

    /// <summary>
    /// Returns the last token in the list.
    /// </summary>
    /// <returns> The last token in the list.</returns>
    /// <exception cref="InvalidOperationException">The list is empty.</exception>        
    public SyntaxToken Last()
    {
        if (Any())
        {
            return this[^1];
        }

        throw new InvalidOperationException();
    }

    /// <summary>
    /// Tests whether the list is non-empty.
    /// </summary>
    /// <returns>True if the list contains any tokens.</returns>
    public bool Any()
    {
        return Node != null;
    }

    /// <summary>
    /// Returns a list which contains all elements of <see cref="SyntaxTokenList"/> in reversed order.
    /// </summary>
    /// <returns><see cref="Reversed"/> which contains all elements of <see cref="SyntaxTokenList"/> in reversed order</returns>
    public Reversed Reverse()
    {
        return new Reversed(this);
    }

    internal void CopyTo(int offset, GreenNode?[] array, int arrayOffset, int count)
    {
        Debug.Assert(this.Count >= offset + count);

        for (int i = 0; i < count; i++)
        {
            array[arrayOffset + i] = GetGreenNodeAt(offset + i);
        }
    }

    /// <summary>
    /// get the green node at the given slot
    /// </summary>
    private GreenNode? GetGreenNodeAt(int i)
    {
        Debug.Assert(Node is object);
        return GetGreenNodeAt(Node, i);
    }

    /// <summary>
    /// get the green node at the given slot
    /// </summary>
    private static GreenNode? GetGreenNodeAt(GreenNode node, int i)
    {
        Debug.Assert(node.IsList || (i == 0 && !node.IsList));
        return node.IsList ? node.GetSlot(i) : node;
    }

    public int IndexOf(SyntaxToken tokenInList)
    {
        for (int i = 0, n = this.Count; i < n; i++)
        {
            var token = this[i];
            if (token == tokenInList)
            {
                return i;
            }
        }

        return -1;
    }

    internal int IndexOf(SyntaxKind kind)
    {
        for (int i = 0, n = this.Count; i < n; i++)
        {
            if (this[i].Kind == kind)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Creates a new <see cref="SyntaxTokenList"/> with the specified token added to the end.
    /// </summary>
    /// <param name="token">The token to add.</param>
    public SyntaxTokenList Add(SyntaxToken token)
    {
        return Insert(this.Count, token);
    }

    /// <summary>
    /// Creates a new <see cref="SyntaxTokenList"/> with the specified tokens added to the end.
    /// </summary>
    /// <param name="tokens">The tokens to add.</param>
    public SyntaxTokenList AddRange(IEnumerable<SyntaxToken> tokens)
    {
        return InsertRange(this.Count, tokens);
    }

    /// <summary>
    /// Creates a new <see cref="SyntaxTokenList"/> with the specified token insert at the index.
    /// </summary>
    /// <param name="index">The index to insert the new token.</param>
    /// <param name="token">The token to insert.</param>
    public SyntaxTokenList Insert(int index, SyntaxToken token)
    {
        if (token == default(SyntaxToken))
        {
            throw new ArgumentOutOfRangeException(nameof(token));
        }

        return InsertRange(index, new[] { token });
    }

    /// <summary>
    /// Creates a new <see cref="SyntaxTokenList"/> with the specified tokens insert at the index.
    /// </summary>
    /// <param name="index">The index to insert the new tokens.</param>
    /// <param name="tokens">The tokens to insert.</param>
    public SyntaxTokenList InsertRange(int index, IEnumerable<SyntaxToken> tokens)
    {
        if (index < 0 || index > this.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (tokens == null)
        {
            throw new ArgumentNullException(nameof(tokens));
        }

        var items = tokens.ToList();
        if (items.Count == 0)
        {
            return this;
        }

        var list = this.ToList();
        list.InsertRange(index, tokens);

        if (list.Count == 0)
        {
            return this;
        }

        return new SyntaxTokenList(null, GreenNode.CreateList(list, static n => n.RequiredNode), 0, 0);
    }

    /// <summary>
    /// Creates a new <see cref="SyntaxTokenList"/> with the token at the specified index removed.
    /// </summary>
    /// <param name="index">The index of the token to remove.</param>
    public SyntaxTokenList RemoveAt(int index)
    {
        if (index < 0 || index >= this.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var list = this.ToList();
        list.RemoveAt(index);
        return new SyntaxTokenList(null, GreenNode.CreateList(list, static n => n.RequiredNode), 0, 0);
    }

    /// <summary>
    /// Creates a new <see cref="SyntaxTokenList"/> with the specified token removed.
    /// </summary>
    /// <param name="tokenInList">The token to remove.</param>
    public SyntaxTokenList Remove(SyntaxToken tokenInList)
    {
        var index = this.IndexOf(tokenInList);
        if (index >= 0 && index <= this.Count)
        {
            return RemoveAt(index);
        }

        return this;
    }

    /// <summary>
    /// Creates a new <see cref="SyntaxTokenList"/> with the specified token replaced with a new token.
    /// </summary>
    /// <param name="tokenInList">The token to replace.</param>
    /// <param name="newToken">The new token.</param>
    public SyntaxTokenList Replace(SyntaxToken tokenInList, SyntaxToken newToken)
    {
        if (newToken == default(SyntaxToken))
        {
            throw new ArgumentOutOfRangeException(nameof(newToken));
        }

        return ReplaceRange(tokenInList, new[] { newToken });
    }

    /// <summary>
    /// Creates a new <see cref="SyntaxTokenList"/> with the specified token replaced with new tokens.
    /// </summary>
    /// <param name="tokenInList">The token to replace.</param>
    /// <param name="newTokens">The new tokens.</param>
    public SyntaxTokenList ReplaceRange(SyntaxToken tokenInList, IEnumerable<SyntaxToken> newTokens)
    {
        var index = this.IndexOf(tokenInList);
        if (index >= 0 && index <= this.Count)
        {
            var list = this.ToList();
            list.RemoveAt(index);
            list.InsertRange(index, newTokens);
            return new SyntaxTokenList(null, GreenNode.CreateList(list, static n => n.RequiredNode), 0, 0);
        }

        throw new ArgumentOutOfRangeException(nameof(tokenInList));
    }

    // for debugging
    private SyntaxToken[] Nodes => this.ToArray();

    /// <summary>
    /// Returns an enumerator for the tokens in the <see cref="SyntaxTokenList"/>
    /// </summary>
    public Enumerator GetEnumerator()
    {
        return new Enumerator(in this);
    }

    IEnumerator<SyntaxToken> IEnumerable<SyntaxToken>.GetEnumerator()
    {
        if (Node == null)
        {
            return SpecializedCollections.EmptyEnumerator<SyntaxToken>();
        }

        return new EnumeratorImpl(in this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        if (Node == null)
        {
            return SpecializedCollections.EmptyEnumerator<SyntaxToken>();
        }

        return new EnumeratorImpl(in this);
    }

    /// <summary>
    /// Compares <paramref name="left"/> and <paramref name="right"/> for equality.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns>True if the two <see cref="SyntaxTokenList"/>s are equal.</returns>
    public static bool operator ==(SyntaxTokenList left, SyntaxTokenList right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares <paramref name="left"/> and <paramref name="right"/> for inequality.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns>True if the two <see cref="SyntaxTokenList"/>s are not equal.</returns>
    public static bool operator !=(SyntaxTokenList left, SyntaxTokenList right)
    {
        return !left.Equals(right);
    }

    public bool Equals(SyntaxTokenList other)
    {
        return Node == other.Node && _parent == other._parent && _index == other._index;
    }

    /// <summary>
    /// Compares this <see cref=" SyntaxTokenList"/> with the <paramref name="obj"/> for equality.
    /// </summary>
    /// <returns>True if the two objects are equal.</returns>
    public override bool Equals(object? obj)
    {
        return obj is SyntaxTokenList list && Equals(list);
    }

    /// <summary>
    /// Serves as a hash function for the <see cref="SyntaxTokenList"/>
    /// </summary>
    public override int GetHashCode()
    {
        // Not call GHC on parent as it's expensive
        var hash = HashCodeCombiner.Start();
        hash.Add(Node);
        hash.Add(_index);

        return hash.CombinedHash;
    }

    /// <summary>
    /// Create a new Token List
    /// </summary>
    /// <param name="token">Element of the return Token List</param>
    public static SyntaxTokenList Create(SyntaxToken token)
    {
        return new SyntaxTokenList(token);
    }
    /// <summary>
    /// A structure for enumerating a <see cref="SyntaxTokenList"/>
    /// </summary>
    public struct Enumerator
    {
        // This enumerator allows us to enumerate through two types of lists.
        // either it looks like:
        //
        //   Parent
        //   |
        //   List
        //   |   \
        //   c1  c2
        //
        // or
        //
        //   Parent
        //   |
        //   c1
        //
        // I.e. in the single child case, we optimize and store the child
        // directly (without any list parent).
        //
        // Enumerating over the single child case is simple.  We just 
        // return it and we're done.
        //
        // In the multi child case, things are a bit more difficult.  We need
        // to return the children in order, while also keeping their offset
        // correct.

        private readonly SyntaxNode? _parent;
        private readonly GreenNode? _singleNodeOrList;
        private readonly int _baseIndex;
        private readonly int _count;

        private int _index;
        private GreenNode? _current;
        private int _position;

        internal Enumerator(in SyntaxTokenList list)
        {
            _parent = list._parent;
            _singleNodeOrList = list.Node;
            _baseIndex = list._index;
            _count = list.Count;

            _index = -1;
            _current = null;
            _position = list.Position;
        }

        /// <summary>
        /// Advances the enumerator to the next token in the collection.
        /// </summary>
        /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator
        /// has passed the end of the collection.</returns>
        public bool MoveNext()
        {
            if (_count == 0 || _count <= _index + 1)
            {
                // invalidate iterator
                _current = null;
                return false;
            }

            _index++;

            // Add the length of the previous node to the offset so that
            // the next node's offset is reported correctly.
            if (_current != null)
            {
                _position += _current.Width;
            }

            Debug.Assert(_singleNodeOrList is object);
            _current = GetGreenNodeAt(_singleNodeOrList, _index);
            Debug.Assert(_current is object);
            return true;
        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        public SyntaxToken Current
        {
            get
            {
                if (_current == null)
                {
                    throw new InvalidOperationException();
                }

                // In both the list and the single node case we want to 
                // return the original root parent as the parent of this
                // token.
                return new SyntaxToken(_current, _parent, _position, _baseIndex + _index);
            }
        }

        public override bool Equals(object? obj)
        {
            throw new NotSupportedException();
        }

        public override int GetHashCode()
        {
            throw new NotSupportedException();
        }
    }

    private class EnumeratorImpl : IEnumerator<SyntaxToken>
    {
        private Enumerator _enumerator;

        // SyntaxTriviaList is a relatively big struct so is passed by ref
        internal EnumeratorImpl(in SyntaxTokenList list)
        {
            _enumerator = new Enumerator(in list);
        }

        public SyntaxToken Current => _enumerator.Current;

        object IEnumerator.Current => _enumerator.Current;

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Reversed enumerable.
    /// </summary>
    public readonly struct Reversed : IEnumerable<SyntaxToken>, IEquatable<Reversed>
    {
        private readonly SyntaxTokenList _list;

        public Reversed(SyntaxTokenList list)
        {
            _list = list;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(in _list);
        }

        IEnumerator<SyntaxToken> IEnumerable<SyntaxToken>.GetEnumerator()
        {
            if (_list.Count == 0)
            {
                return SpecializedCollections.EmptyEnumerator<SyntaxToken>();
            }

            return new EnumeratorImpl(in _list);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (_list.Count == 0)
            {
                return SpecializedCollections.EmptyEnumerator<SyntaxToken>();
            }

            return new EnumeratorImpl(in _list);
        }

        public override bool Equals(object? obj)
        {
            return obj is Reversed r && Equals(r);
        }

        public bool Equals(Reversed other)
        {
            return _list.Equals(other._list);
        }

        public override int GetHashCode()
        {
            return _list.GetHashCode();
        }

        [SuppressMessage("Performance", "CA1067", Justification = "Equality not actually implemented")]
        [StructLayout(LayoutKind.Auto)]
        public struct Enumerator
        {
            private readonly SyntaxNode? _parent;
            private readonly GreenNode? _singleNodeOrList;
            private readonly int _baseIndex;
            private readonly int _count;

            private int _index;
            private GreenNode? _current;
            private int _position;

            internal Enumerator(in SyntaxTokenList list)
                : this()
            {
                if (list.Any())
                {
                    _parent = list._parent;
                    _singleNodeOrList = list.Node;
                    _baseIndex = list._index;
                    _count = list.Count;

                    _index = _count;
                    _current = null;

                    var last = list.Last();
                    _position = last.Position + last.Width;
                }
            }

            public bool MoveNext()
            {
                if (_count == 0 || _index <= 0)
                {
                    _current = null;
                    return false;
                }

                _index--;

                Debug.Assert(_singleNodeOrList is object);
                _current = GetGreenNodeAt(_singleNodeOrList, _index);
                Debug.Assert(_current is object);
                _position -= _current.Width;

                return true;
            }

            public SyntaxToken Current
            {
                get
                {
                    if (_current == null)
                    {
                        throw new InvalidOperationException();
                    }

                    return new SyntaxToken(_current, _parent, _position, _baseIndex + _index);
                }
            }

            public override bool Equals(object? obj)
            {
                throw new NotSupportedException();
            }

            public override int GetHashCode()
            {
                throw new NotSupportedException();
            }
        }

        private class EnumeratorImpl : IEnumerator<SyntaxToken>
        {
            private Enumerator _enumerator;

            // SyntaxTriviaList is a relatively big struct so is passed as ref
            internal EnumeratorImpl(in SyntaxTokenList list)
            {
                _enumerator = new Enumerator(in list);
            }

            public SyntaxToken Current => _enumerator.Current;

            object IEnumerator.Current => _enumerator.Current;

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {
            }
        }
    }
}
