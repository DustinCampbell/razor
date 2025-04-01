// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Razor.Utilities;

[StructLayout(LayoutKind.Explicit, Size = HashSize)]
internal readonly partial record struct Checksum(
    [field: FieldOffset(0)] long Data1,
    [field: FieldOffset(8)] long Data2,
    [field: FieldOffset(16)] long Data3,
    [field: FieldOffset(24)] long Data4)
{
    // Size of SHA-256
    private const int HashSize = 256 / 8;

    public static readonly Checksum Null = default;

    public static Checksum From(ReadOnlySpan<byte> source)
    {
        if (source.Length == 0)
        {
            return Null;
        }

        if (source.Length != HashSize)
        {
            throw new ArgumentException($"{nameof(source)} size must be equal to {HashSize}", nameof(source));
        }

        if (!MemoryMarshal.TryRead(source, out Checksum result))
        {
            throw new InvalidOperationException("Could not read hash data");
        }

        return result;
    }

    public string ToBase64String()
    {
#if NET
        Span<byte> bytes = stackalloc byte[HashSize];
        WriteTo(bytes);
        return Convert.ToBase64String(bytes);
#else
        var bytes = new byte[HashSize];
        WriteTo(bytes.AsSpan());
        return Convert.ToBase64String(bytes);
#endif
    }

    public void WriteTo(Span<byte> span)
    {
        ArgHelper.ThrowIfLessThan(span.Length, HashSize, nameof(span));
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), this);
    }

    public static Checksum FromBase64String(string value)
        => value is null
            ? Null
            : From(Convert.FromBase64String(value));

    // Explicitly implement this method as default jit for records on netfx doesn't properly devirtualize the
    // standard calls to EqualityComparer<long>.Default.Equals
    public bool Equals(Checksum other)
        => Data1 == other.Data1 &&
           Data2 == other.Data2 &&
           Data3 == other.Data3 &&
           Data4 == other.Data4;

    // Directly override to any overhead that records add when hashing things like the EqualityContract
    public override int GetHashCode()
    {
        // The checksum is already a hash. Just read a 4-byte value to get a well-distributed hash code.
        return (int)Data1;
    }

    public override string ToString()
        => ToBase64String();
}
