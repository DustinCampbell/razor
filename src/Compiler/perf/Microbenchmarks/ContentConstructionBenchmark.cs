// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

/// <summary>
/// Focused benchmarks for Content construction operations (< 3 minutes)
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[BenchmarkCategory("Fast", "Construction")]
public class ContentConstructionBenchmark
{
    private readonly string _shortString = "Hello World";
    private readonly string _mediumString = "The quick brown fox jumps over the lazy dog. " +
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit.";
    private readonly string _longString = string.Concat(Enumerable.Repeat(
        "This is a longer string used for performance testing. ", 20));

    private readonly string[] _smallParts = ["Hello", " ", "World", "!"];
    private readonly string[] _mediumParts = [
        "The", " ", "quick", " ", "brown", " ", "fox", " ",
        "jumps", " ", "over", " ", "the", " ", "lazy", " ", "dog", "."
    ];
    private readonly string[] _largeParts;

    public ContentConstructionBenchmark()
    {
        _largeParts = new string[100]; // Smaller for faster benchmarking
        for (var i = 0; i < _largeParts.Length; i++)
        {
            _largeParts[i] = i % 10 == 0 ? $"Part{i}" : " ";
        }
    }

    [Benchmark(Baseline = true, Description = "String: short")]
    public Content Construction_ShortString() => new(_shortString);

    [Benchmark(Description = "String: medium")]
    public Content Construction_MediumString() => new(_mediumString);

    [Benchmark(Description = "String: long")]
    public Content Construction_LongString() => new(_longString);

    [Benchmark(Description = "Array: small parts")]
    public Content Construction_SmallParts() => new(_smallParts.ToImmutableArray());

    [Benchmark(Description = "Array: medium parts")]
    public Content Construction_MediumParts() => new(_mediumParts.ToImmutableArray());

    [Benchmark(Description = "Array: large parts")]
    public Content Construction_LargeParts() => new(_largeParts.ToImmutableArray());

    [Benchmark(Description = "Nested: simple")]
    public Content Construction_NestedSimple()
    {
        var inner1 = new Content(_smallParts.ToImmutableArray());
        var inner2 = new Content(_mediumParts.ToImmutableArray());
        return new Content([inner1, inner2]);
    }

    [Benchmark(Description = "Nested: deep")]
    public Content Construction_NestedDeep()
    {
        var level1 = new Content(["A", "B"]);
        var level2 = new Content([level1, new Content(["C", "D"])]);
        var level3 = new Content([level2, new Content(["E", "F"])]);
        return level3;
    }

    [Benchmark(Description = "Memory: small")]
    public Content Construction_MemorySmall()
    {
        return new Content([
            "Hello".AsMemory(),
            " ".AsMemory(),
            "World".AsMemory()
        ]);
    }

    [Benchmark(Description = "Empty variations")]
    public Content Construction_Empty()
    {
        var empty1 = new Content((string)null);
        var empty2 = new Content("");
        var empty3 = new Content(ImmutableArray<string>.Empty);
        return new Content([empty1, empty2, empty3, new Content("Test")]);
    }
}
