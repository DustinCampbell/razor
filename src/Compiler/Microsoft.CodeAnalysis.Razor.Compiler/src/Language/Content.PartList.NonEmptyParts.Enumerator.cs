// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

public readonly partial struct Content
{
    public readonly ref partial struct PartList
    {
        public readonly ref partial struct NonEmptyParts
        {
            /// <summary>
            ///  Provides efficient enumeration over only the non-empty parts of a <see cref="PartList"/>.
            /// </summary>
            /// <remarks>
            ///  <para>
            ///   This enumerator wraps the underlying <see cref="PartList.Enumerator"/> and automatically
            ///   filters out any parts that have zero length. It maintains the same performance
            ///   characteristics as the underlying enumerator while providing the filtering behavior.
            ///  </para>
            ///  <para>
            ///   The enumerator should be disposed to ensure proper cleanup of internal resources.
            ///   This happens automatically when used in a foreach statement.
            ///  </para>
            /// </remarks>
            /// <param name="parts">The <see cref="PartList"/> to enumerate non-empty parts from.</param>
            public ref struct Enumerator(PartList parts)
            {
                private PartList.Enumerator _enumerator = parts.GetEnumerator();

                /// <summary>
                ///  Releases resources used by the enumerator.
                /// </summary>
                /// <remarks>
                ///  This method disposes the underlying <see cref="PartList.Enumerator"/>.
                ///  It is called automatically when the enumerator is used in a foreach statement.
                /// </remarks>
                public void Dispose()
                {
                    _enumerator.Dispose();
                }

                /// <summary>
                ///  Gets the non-empty character sequence at the current position of the enumerator.
                /// </summary>
                /// <returns>
                ///  A <see cref="ReadOnlyMemory{T}"/> of characters representing the current non-empty part.
                /// </returns>
                /// <remarks>
                ///  This property is only valid after <see cref="MoveNext"/> has returned <see langword="true"/>
                ///  and before it returns false or <see cref="Dispose"/> is called.
                ///  The returned part is guaranteed to have at least one character.
                /// </remarks>
                public readonly ReadOnlyMemory<char> Current => _enumerator.Current;

                /// <summary>
                ///  Advances the enumerator to the next non-empty part in the sequence.
                /// </summary>
                /// <returns>
                ///  <see langword="true"/> if the enumerator successfully advanced to the next non-empty part;
                ///  <see langword="false"/> if the enumerator has passed the end of the sequence.
                /// </returns>
                /// <remarks>
                ///  <para>
                ///   This method advances through the underlying parts, automatically skipping any parts
                ///   that have zero length. It will continue advancing until it finds a non-empty part
                ///   or reaches the end of the sequence.
                ///  </para>
                ///  <para>
                ///   After this method returns false, the <see cref="Current"/> property is undefined.
                ///  </para>
                /// </remarks>
                public bool MoveNext()
                {
                    while (_enumerator.MoveNext())
                    {
                        if (!_enumerator.Current.IsEmpty)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }
        }
    }
}
