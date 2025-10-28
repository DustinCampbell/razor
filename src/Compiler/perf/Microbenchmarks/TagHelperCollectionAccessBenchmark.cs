// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

[MemoryDiagnoser]
public class TagHelperCollectionAccessBenchmark
{
    private TagHelperDescriptor[]? _smallTagHelpers;
    private TagHelperDescriptor[]? _mediumTagHelpers;
    private TagHelperDescriptor[]? _largeTagHelpers;

    [Params(10, 100, 1000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _smallTagHelpers = TagHelperCollectionHelpers.CreateTagHelpers(10);
        _mediumTagHelpers = TagHelperCollectionHelpers.CreateTagHelpers(100);
        _largeTagHelpers = TagHelperCollectionHelpers.CreateTagHelpers(1000);
    }

    [Benchmark(Description = "Collection Indexer Access")]
    public TagHelperDescriptor IndexerAccess()
    {
        var collection = TagHelperCollection.Create(GetTagHelpersByCount(Count));

        TagHelperDescriptor result = null!;
        for (var i = 0; i < collection.Count; i++)
        {
            result = collection[i];
        }

        return result;
    }

    [Benchmark(Description = "Collection Contains")]
    public bool ContainsCheck()
    {
        var helpers = GetTagHelpersByCount(Count);
        var collection = TagHelperCollection.Create(helpers);
        var result = false;

        foreach (var helper in helpers)
        {
            result = collection.Contains(helper);
        }

        return result;
    }

    [Benchmark(Description = "Collection IndexOf")]
    public int IndexOfCheck()
    {
        var helpers = GetTagHelpersByCount(Count);
        var collection = TagHelperCollection.Create(helpers);
        var result = -1;

        foreach (var helper in helpers)
        {
            result = collection.IndexOf(helper);
        }

        return result;
    }

    [Benchmark(Description = "Collection Enumeration")]
    public int EnumerateCollection()
    {
        var collection = TagHelperCollection.Create(GetTagHelpersByCount(Count));
        var count = 0;

        foreach (var item in collection)
        {
            count++;
        }

        return count;
    }

    [Benchmark(Description = "Collection CopyTo")]
    public void CopyToArray()
    {
        var collection = TagHelperCollection.Create(GetTagHelpersByCount(Count));
        var destination = new TagHelperDescriptor[collection.Count];
        collection.CopyTo(destination);
    }

    [Benchmark(Description = "Collection Equality")]
    public bool EqualityCheck()
    {
        var helpers = GetTagHelpersByCount(Count);
        var collection1 = TagHelperCollection.Create(helpers);
        var collection2 = TagHelperCollection.Create(helpers);
        return collection1.Equals(collection2);
    }

    [Benchmark(Description = "Collection GetHashCode")]
    public int GetHashCodeCheck()
    {
        var collection = TagHelperCollection.Create(GetTagHelpersByCount(Count));
        return collection.GetHashCode();
    }

    [Benchmark(Description = "Collection Where")]
    public TagHelperCollection WhereFilter()
    {
        var collection = TagHelperCollection.Create(GetTagHelpersByCount(Count));
        return collection.Where(th => th.Name.Contains("TagHelper"));
    }

    private TagHelperDescriptor[] GetTagHelpersByCount(int count) => count switch
    {
        <= 10 => _smallTagHelpers!,
        <= 100 => _mediumTagHelpers!,
        _ => _largeTagHelpers!
    };
}
