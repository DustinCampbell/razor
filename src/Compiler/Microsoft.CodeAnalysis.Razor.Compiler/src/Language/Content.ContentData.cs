// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Razor.Language;

public readonly partial struct Content
{
    /// <summary>
    /// Encapsulates packed metadata for Content in a single 32-bit value.
    /// <para>
    /// Bit Layout (MSB to LSB):
    /// <code>
    /// [31-30] ContentKind (2 bits) - Type of content storage
    ///   00 = Value (single ReadOnlyMemory)
    ///   01 = ContentArray (nested Content[])
    ///   10 = MemoryArray (ReadOnlyMemory<char>[])
    ///   11 = StringArray (string[])
    ///   
    /// [29-16] PartCount (14 bits) - Number of flattened parts (max 16,383)
    ///   
    /// [15-0]  CharLength (16 bits) - Total character length (max 65,535)
    /// </code>
    /// </para>
    /// </summary>
    private readonly struct ContentData : IEquatable<ContentData>
    {
        public static ContentData Empty => default;

        private const byte KindShift = 30;
        private const ushort MaxPartCount = 0x3fff;  // 14 bits
        private const byte PartCountShift = 16;
        private const ushort MaxCharLength = 0xffff; // 16 bits

        private readonly uint _value;

        private ContentData(uint value)
        {
            _value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ContentData Value(int charLength)
            => Create(charLength, charLength > 0 ? 1 : 0, ContentKind.Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ContentData ContentArray(int charLength, int partsCount)
            => Create(charLength, partsCount, ContentKind.ContentArray);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ContentData MemoryArray(int charLength, int partsCount)
            => Create(charLength, partsCount, ContentKind.MemoryArray);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ContentData StringArray(int charLength, int partsCount)
            => Create(charLength, partsCount, ContentKind.StringArray);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ContentData Create(int charLength, int partsCount, ContentKind kind)
        {
            Debug.Assert(charLength <= MaxCharLength, $"Character length {charLength} exceeds maximum {MaxCharLength}");
            Debug.Assert(partsCount <= MaxPartCount, $"Part count {partsCount} exceeds maximum {MaxPartCount}");

            var value = ((uint)kind << KindShift)
                      | ((uint)(partsCount & MaxPartCount) << PartCountShift)
                      | (uint)(charLength & MaxCharLength);

            return new ContentData(value);
        }

        public ContentKind Kind
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ContentKind)(_value >> KindShift);
        }

        public int PartCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)((_value >> PartCountShift) & MaxPartCount);
        }

        public int CharLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)(_value & MaxCharLength);
        }

        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _value == 0;
        }

        public static bool operator ==(ContentData left, ContentData right)
            => left._value == right._value;

        public static bool operator !=(ContentData left, ContentData right)
            => left._value != right._value;

        public bool Equals(ContentData other)
            => _value == other._value;

        public override bool Equals(object? obj)
            => obj is ContentData other && Equals(other);

        public override int GetHashCode()
            => _value.GetHashCode();

        public override string ToString()
            => $"Kind={Kind}, PartCount={PartCount}, CharLength={CharLength}";
    }
}
