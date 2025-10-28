// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

/// <summary>
/// Quick benchmark for inner dev loop - single size, key scenarios only
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 3)] // Much faster than default
public class TagHelperCollectionQuickBenchmark
{
    private TagHelperDescriptor[]? _tagHelpers;
    private TagHelperCollection[]? _collections;

    [Params(100)] // Single size for speed
    public int Count { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _tagHelpers = TagHelperCollectionHelpers.CreateTagHelpers(Count);
        _collections = TagHelperCollectionHelpers.CreateTagHelperCollections(5, Count / 5);
    }

    [Benchmark(Description = "Create from Array")]
    public TagHelperCollection CreateFromArray()
    {
        return TagHelperCollection.Create(_tagHelpers!);
    }

    [Benchmark(Description = "Merge Two Collections")]
    public TagHelperCollection MergeTwoCollections()
    {
        return TagHelperCollection.Merge(_collections![0], _collections[1]);
    }

    [Benchmark(Description = "Collection Indexer Access")]
    public TagHelperDescriptor IndexerAccess()
    {
        var collection = TagHelperCollection.Create(_tagHelpers!);
        TagHelperDescriptor result = null!;
        
        // Test all indices to get accurate scaling behavior
        for (var i = 0; i < collection.Count; i++)
        {
            result = collection[i];
        }
        
        return result;
    }
}
