// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

[MemoryDiagnoser]
public class TagHelperCollectionCreationBenchmark
{
    private TagHelperDescriptor[]? _smallTagHelpers;
    private TagHelperDescriptor[]? _mediumTagHelpers;
    private TagHelperDescriptor[]? _largeTagHelpers;
    private TagHelperDescriptor[]? _duplicateTagHelpers;

    [Params(10, 100, 1000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _smallTagHelpers = TagHelperCollectionHelpers.CreateTagHelpers(10);
        _mediumTagHelpers = TagHelperCollectionHelpers.CreateTagHelpers(100);
        _largeTagHelpers = TagHelperCollectionHelpers.CreateTagHelpers(1000);
        _duplicateTagHelpers = TagHelperCollectionHelpers.CreateTagHelpersWithDuplicates(Count);
    }

    [Benchmark(Description = "Create from Array")]
    public TagHelperCollection CreateFromArray()
    {
        var helpers = GetTagHelpersByCount(Count);
        return TagHelperCollection.Create(helpers);
    }

    [Benchmark(Description = "Create from ImmutableArray")]
    public TagHelperCollection CreateFromImmutableArray()
    {
        var helpers = GetTagHelpersByCount(Count);
        var immutableArray = ImmutableArray.Create(helpers);
        return TagHelperCollection.Create(immutableArray);
    }

    [Benchmark(Description = "Create from ReadOnlySpan")]
    public TagHelperCollection CreateFromReadOnlySpan()
    {
        var helpers = GetTagHelpersByCount(Count);
        return TagHelperCollection.Create(helpers.AsSpan());
    }

    [Benchmark(Description = "Create from IEnumerable")]
    public TagHelperCollection CreateFromIEnumerable()
    {
        var helpers = GetTagHelpersByCount(Count);
        return TagHelperCollection.Create((IEnumerable<TagHelperDescriptor>)helpers);
    }

    [Benchmark(Description = "Create with Duplicates")]
    public TagHelperCollection CreateWithDuplicates()
    {
        return TagHelperCollection.Create(_duplicateTagHelpers!);
    }

    private TagHelperDescriptor[] GetTagHelpersByCount(int count) => count switch
    {
        <= 10 => _smallTagHelpers!,
        <= 100 => _mediumTagHelpers!,
        _ => _largeTagHelpers!
    };
}
