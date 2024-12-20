// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using MessagePack;

namespace Microsoft.AspNetCore.Razor.Serialization.MessagePack.Formatters;

internal static class Extensions
{
    public static void ReadArrayHeaderAndVerify(this ref MessagePackReader reader, int expectedCount)
    {
        if (reader.NextMessagePackType != MessagePackType.Array)
        {
            throw new MessagePackSerializationException($"Expected next type to be {MessagePackType.Array}, but it was {reader.NextMessagePackType}");
        }

        var count = reader.ReadArrayHeader();

        if (count != expectedCount)
        {
            throw new MessagePackSerializationException($"Expected {expectedCount} values, but there were {count}");
        }
    }

    public delegate T MessagePackValueReader<T>(ref MessagePackReader reader);

    public static ImmutableArray<T> ReadArray<T>(
        this ref MessagePackReader reader,
        ref int countRemaining,
        MessagePackValueReader<T> valueReader)
    {
        return reader.ReadArray(ref countRemaining, valueSize: 1, valueReader);
    }

    public static ImmutableArray<KeyValuePair<TKey, TValue>> ReadArray<TKey, TValue>(
        this ref MessagePackReader reader,
        ref int countRemaining,
        MessagePackValueReader<KeyValuePair<TKey, TValue>> valueReader)
    {
        return reader.ReadArray(ref countRemaining, valueSize: 2, valueReader);
    }

    public static ImmutableArray<T> ReadArray<T>(
        this ref MessagePackReader reader,
        ref int countRemaining,
        int valueSize,
        MessagePackValueReader<T> valueReader)
    {
        ArgHelper.ThrowIfLessThanOrEqual(countRemaining, 0);
        ArgHelper.ThrowIfLessThan(valueSize, 1);

        var length = reader.ReadInt32();
        countRemaining--;

        Assumed.True(countRemaining >= length * valueSize);

        var array = new T[length];

        for (var i = 0; i < length; i++)
        {
            array[i] = valueReader(ref reader);
        }

        countRemaining -= length * valueSize;

        return ImmutableCollectionsMarshal.AsImmutableArray(array);
    }

    public static void Serialize<T>(this ref MessagePackWriter writer, T? value, MessagePackSerializerOptions options)
        where T : class
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        options.Resolver.GetFormatterWithVerify<T>().Serialize(ref writer, value, options);
    }

    public static T Deserialize<T>(this ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return options.Resolver.GetFormatterWithVerify<T>().Deserialize(ref reader, options);
    }

    public static T? DeserializeOrNull<T>(this ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return default;
        }

        return options.Resolver.GetFormatterWithVerify<T>().Deserialize(ref reader, options);
    }
}

internal static class ExtraExtensions
{
    // C# allows extension method overloads to differ only by generic constraints, but they must be declared on
    // different classes, since they'll have the same signature.
    public static void Serialize<T>(this ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
        where T : struct
    {
        options.Resolver.GetFormatterWithVerify<T>().Serialize(ref writer, value, options);
    }
}
