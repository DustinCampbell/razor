﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class MetadataCollectionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(64)]
    public void CreateAndCompareCollections(int size)
    {
        var pairs = new List<KeyValuePair<string, string?>>();

        for (var i = 0; i < size; i++)
        {
            var key = i.ToString(CultureInfo.InvariantCulture);
            var value = (int.MaxValue - i).ToString(CultureInfo.InvariantCulture);

            pairs.Add(new(key, value));
        }

        var collection1 = MetadataCollection.Create(pairs.ToArray());
        var reversed = Enumerable.Reverse(pairs.ToArray());
        var collection2 = MetadataCollection.Create(reversed.ToArray());

        Assert.Equal(collection1, collection2);

        foreach (var (key, value) in pairs)
        {
            Assert.True(collection1.TryGetValue(key, out var value1));
            Assert.Equal(value, value1);

            Assert.True(collection2.TryGetValue(key, out var value2));
            Assert.Equal(value, value2);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(64)]
    public void EnumeratorReturnsAllItemsInCollection(int size)
    {
        var pairs = new List<KeyValuePair<string, string?>>();
        var map = new Dictionary<string, bool>();

        for (var i = 0; i < size; i++)
        {
            var key = i.ToString(CultureInfo.InvariantCulture);
            var value = (int.MaxValue - i).ToString(CultureInfo.InvariantCulture);

            pairs.Add(new(key, value));
            map.Add(key, false);
        }

        Assert.True(map.All(kvp => kvp.Value == false));

        var collection = MetadataCollection.Create(pairs);

        foreach (var (key, _) in collection)
        {
            map[key] = true;
        }

        Assert.True(map.All(kvp => kvp.Value == true));

        var enumerator = collection.GetEnumerator();

        while (enumerator.MoveNext())
        {
            var (key, _) = enumerator.Current;
            map[key] = false;
        }

        Assert.True(map.All(kvp => kvp.Value == false));

        // Verify that reset works.
        enumerator.Reset();

        while (enumerator.MoveNext())
        {
            var (key, _) = enumerator.Current;
            map[key] = true;
        }

        Assert.True(map.All(kvp => kvp.Value == true));
    }

    [Fact]
    public void CreateThrowsOnDuplicateKeys()
    {
        var one = new KeyValuePair<string, string?>("Key1", "Value1");
        var two = new KeyValuePair<string, string?>("Key2", "Value2");
        var three = new KeyValuePair<string, string?>("Key3", "Value3");
        var four = new KeyValuePair<string, string?>("Key4", "Value4");

        Assert.Throws<ArgumentException>(() => MetadataCollection.Create(one, one));
        Assert.Throws<ArgumentException>(() => MetadataCollection.Create(new[] { one, one }));

        Assert.Throws<ArgumentException>(() => MetadataCollection.Create(one, one, three));
        Assert.Throws<ArgumentException>(() => MetadataCollection.Create(new[] { one, one, three }));
        Assert.Throws<ArgumentException>(() => MetadataCollection.Create(one, three, three));
        Assert.Throws<ArgumentException>(() => MetadataCollection.Create(new[] { one, three, three }));
        Assert.Throws<ArgumentException>(() => MetadataCollection.Create(one, two, one));
        Assert.Throws<ArgumentException>(() => MetadataCollection.Create(new[] { one, two, one }));

        Assert.Throws<ArgumentException>(() => MetadataCollection.Create(one, one, three, four));
        Assert.Throws<ArgumentException>(() => MetadataCollection.Create(new[] { one, one, three, four }));
        Assert.Throws<ArgumentException>(() => MetadataCollection.Create(one, two, one, four));
        Assert.Throws<ArgumentException>(() => MetadataCollection.Create(new[] { one, two, one, four }));
        Assert.Throws<ArgumentException>(() => MetadataCollection.Create(one, two, three, one));
        Assert.Throws<ArgumentException>(() => MetadataCollection.Create(new[] { one, two, three, one }));
        Assert.Throws<ArgumentException>(() => MetadataCollection.Create(one, two, two, four));
        Assert.Throws<ArgumentException>(() => MetadataCollection.Create(new[] { one, two, two, four }));
        Assert.Throws<ArgumentException>(() => MetadataCollection.Create(one, two, three, two));
        Assert.Throws<ArgumentException>(() => MetadataCollection.Create(new[] { one, one, three, two }));
        Assert.Throws<ArgumentException>(() => MetadataCollection.Create(one, two, three, three));
        Assert.Throws<ArgumentException>(() => MetadataCollection.Create(new[] { one, one, three, three }));
    }

    private static TheoryData<ImmutableArray<KeyValuePair<string, string?>>, ImmutableArray<KeyValuePair<string, string?>>> GetPairPermutations(int count)
    {
        var pairs = new KeyValuePair<string, string?>[count];

        for (var i = 0; i < count; i++)
        {
            pairs[i] = new($"Key{i}", $"Value{i}");
        }

        var list = new List<ImmutableArray<KeyValuePair<string, string?>>>();
        CollectPermutations(pairs, 0, count - 1, list);

        var data = new TheoryData<ImmutableArray<KeyValuePair<string, string?>>, ImmutableArray<KeyValuePair<string, string?>>>();

        for (var i = 0; i < list.Count; i++)
        {
            for (var j = 0; j < list.Count; j++)
            {
                data.Add(list[i], list[j]);
            }
        }

        return data;

        static void CollectPermutations(KeyValuePair<string, string?>[] pairs, int start, int end, List<ImmutableArray<KeyValuePair<string, string?>>> list)
        {
            if (start == end)
            {
                list.Add(pairs.ToImmutableArray());
            }
            else
            {
                for (var i = start; i <= end; i++)
                {
                    Swap(ref pairs[start], ref pairs[i]);
                    CollectPermutations(pairs, start + 1, end, list);
                    Swap(ref pairs[start], ref pairs[i]);
                }
            }

            static void Swap(ref KeyValuePair<string, string?> pair1, ref KeyValuePair<string, string?> pair2)
            {
                var temp = pair1;
                pair1 = pair2;
                pair2 = temp;
            }
        }
    }

    public static readonly TheoryData TwoPairs = GetPairPermutations(2);
    public static readonly TheoryData ThreePairs = GetPairPermutations(3);
    public static readonly TheoryData FourPairs = GetPairPermutations(4);

    [Theory]
    [MemberData(nameof(TwoPairs))]
    public void TestEquality_TwoItems(ImmutableArray<KeyValuePair<string, string?>> pairs1, ImmutableArray<KeyValuePair<string, string?>> pairs2)
    {
        var collection1 = MetadataCollection.Create(pairs1);
        var collection2 = MetadataCollection.Create(pairs2);

        Assert.Equal(collection1, collection2);
        Assert.Equal(collection1.GetHashCode(), collection2.GetHashCode());
    }

    [Theory]
    [MemberData(nameof(ThreePairs))]
    public void TestEquality_ThreeItems(ImmutableArray<KeyValuePair<string, string?>> pairs1, ImmutableArray<KeyValuePair<string, string?>> pairs2)
    {
        var collection1 = MetadataCollection.Create(pairs1);
        var collection2 = MetadataCollection.Create(pairs2);

        Assert.Equal(collection1, collection2);
        Assert.Equal(collection1.GetHashCode(), collection2.GetHashCode());
    }

    [Theory]
    [MemberData(nameof(FourPairs))]
    public void TestEquality_FourItems(ImmutableArray<KeyValuePair<string, string?>> pairs1, ImmutableArray<KeyValuePair<string, string?>> pairs2)
    {
        var collection1 = MetadataCollection.Create(pairs1);
        var collection2 = MetadataCollection.Create(pairs2);

        Assert.Equal(collection1, collection2);
        Assert.Equal(collection1.GetHashCode(), collection2.GetHashCode());
    }

    [Fact]
    public void HashCodesAreSameRegardlessOfOrdering()
    {
        var one = new KeyValuePair<string, string?>("Key1", "Value1");
        var two = new KeyValuePair<string, string?>("Key2", "Value2");
        var three = new KeyValuePair<string, string?>("Key3", "Value3");
        var four = new KeyValuePair<string, string?>("Key4", "Value4");

        Assert.Equal(
            MetadataCollection.Create(one, two).GetHashCode(),
            MetadataCollection.Create(two, one).GetHashCode());

        Assert.Equal(
            MetadataCollection.Create(one, two, three).GetHashCode(),
            MetadataCollection.Create(two, one, three).GetHashCode());

        Assert.Equal(
            MetadataCollection.Create(one, two, three).GetHashCode(),
            MetadataCollection.Create(three, two, one).GetHashCode());

        Assert.Equal(
            MetadataCollection.Create(one, two, three).GetHashCode(),
            MetadataCollection.Create(one, three, two).GetHashCode());

        Assert.Equal(
            MetadataCollection.Create(one, two, three).GetHashCode(),
            MetadataCollection.Create(two, three, one).GetHashCode());

        Assert.Equal(
            MetadataCollection.Create(one, two, three).GetHashCode(),
            MetadataCollection.Create(three, one, two).GetHashCode());

        Assert.Equal(
            MetadataCollection.Create(one, two, three, four).GetHashCode(),
            MetadataCollection.Create(four, three, two, one).GetHashCode());
    }
}
