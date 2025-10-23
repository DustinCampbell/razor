// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

public readonly partial struct Content
{
    /// <summary>
    ///  Provides efficient character-by-character enumeration over <see cref="Content"/>.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   This enumerator is a ref struct that flattens nested content structures and
    ///   enumerates all characters sequentially without heap allocation. It uses the
    ///   underlying <see cref="PartList.Enumerator"/> to traverse parts and maintains
    ///   state for the current position within each part.
    ///  </para>
    ///  <para>
    ///   The enumerator should be disposed to ensure proper cleanup of internal resources.
    ///   This happens automatically when used in a foreach statement.
    ///  </para>
    /// </remarks>
    public ref struct CharEnumerator
    {
        private PartList.NonEmptyParts.Enumerator _partEnumerator;
        private ReadOnlySpan<char> _currentPart;
        private int _currentIndex;
        private char _current;
        private bool _disposed;

        /// <summary>
        ///  Initializes a new instance of the <see cref="CharEnumerator"/> struct.
        /// </summary>
        /// <param name="content">The <see cref="Content"/> to enumerate.</param>
        internal CharEnumerator(Content content)
        {
            _partEnumerator = content.Parts.NonEmpty.GetEnumerator();
            _currentPart = default;
            _currentIndex = 0;
            _current = default;
            _disposed = false;
        }

        /// <summary>
        ///  Releases resources used by the enumerator.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _partEnumerator.Dispose();
            }
        }

        /// <summary>
        ///  Gets the character at the current position of the enumerator.
        /// </summary>
        /// <returns>
        ///  The character at the current position.
        /// </returns>
        /// <remarks>
        ///  This property is only valid after <see cref="MoveNext"/> has returned <see langword="true"/>
        ///  and before it returns false or <see cref="Dispose"/> is called.
        /// </remarks>
        public readonly char Current => _current;

        /// <summary>
        ///  Advances the enumerator to the next character in the sequence.
        /// </summary>
        /// <returns>
        ///  <see langword="true"/> if the enumerator successfully advanced to the next character;
        ///  <see langword="false"/> if the enumerator has passed the end of the sequence.
        /// </returns>
        public bool MoveNext()
        {
            if (_disposed)
            {
                return false;
            }

            // Try to get the next character from the current part
            if (_currentIndex < _currentPart.Length)
            {
                _current = _currentPart[_currentIndex++];
                return true;
            }

            // Current part is exhausted, try to move to the next part
            while (_partEnumerator.MoveNext())
            {
                _currentPart = _partEnumerator.Current.Span;
                _currentIndex = 0;

                if (_currentPart.Length > 0)
                {
                    _current = _currentPart[_currentIndex++];
                    return true;
                }

                // Empty part, continue to next
            }

            // No more parts
            _current = default;
            return false;
        }
    }
}
