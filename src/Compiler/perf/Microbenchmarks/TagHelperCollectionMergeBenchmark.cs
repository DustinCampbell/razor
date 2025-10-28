// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

[MemoryDiagnoser]
public class TagHelperCollectionMergeBenchmark
{
    private TagHelperCollection[]? _smallCollections;
    private TagHelperCollection[]? _mediumCollections;
    private TagHelperCollection[]? _largeCollections;

    [Params(10, 100, 1000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _smallCollections = TagHelperCollectionHelpers.CreateTagHelperCollections(5, 10);
        _mediumCollections = TagHelperCollectionHelpers.CreateTagHelperCollections(10, 50);
        _largeCollections = TagHelperCollectionHelpers.CreateTagHelperCollections(20, 100);
    }

    [Benchmark(Description = "Merge Two Collections")]
    public TagHelperCollection MergeTwoCollections()
    {
        var collections = GetCollectionsByCount(Count);

        if (collections.Length >= 2)
        {
            return TagHelperCollection.Merge(collections[0], collections[1]);
        }

        return TagHelperCollection.Empty;
    }

    [Benchmark(Description = "Merge Multiple Collections")]
    public TagHelperCollection MergeMultipleCollections()
    {
        var collections = GetCollectionsByCount(Count);
        return TagHelperCollection.Merge(collections);
    }

    [Benchmark(Description = "Merge Collections ImmutableArray")]
    public TagHelperCollection MergeCollectionsImmutableArray()
    {
        var collections = GetCollectionsByCount(Count);
        var immutableArray = ImmutableArray.Create(collections);
        return TagHelperCollection.Merge(immutableArray);
    }

    [Benchmark(Description = "Merge Collections IEnumerable")]
    public TagHelperCollection MergeCollectionsIEnumerable()
    {
        var collections = GetCollectionsByCount(Count);
        return TagHelperCollection.Merge((IEnumerable<TagHelperCollection>)collections);
    }

    [Benchmark(Description = "Merge with Duplicates")]
    public TagHelperCollection MergeWithDuplicates()
    {
        var helpers = TagHelperCollectionHelpers.CreateTagHelpers(Count);
        var collection1 = TagHelperCollection.Create(helpers.Take(Count / 2).ToArray());
        var collection2 = TagHelperCollection.Create(helpers.Skip(Count / 4).ToArray());
        return TagHelperCollection.Merge(collection1, collection2);
    }

    private TagHelperCollection[] GetCollectionsByCount(int count) => count switch
    {
        <= 10 => _smallCollections!,
        <= 100 => _mediumCollections!,
        _ => _largeCollections!
    };
}
