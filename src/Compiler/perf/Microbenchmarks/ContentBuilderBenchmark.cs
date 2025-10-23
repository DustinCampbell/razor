// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

/// <summary>
/// Focused benchmarks for Content.Builder and Join operations (< 5 minutes)
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[BenchmarkCategory("Fast", "Builder")]
public class ContentBuilderBenchmark
{
    private readonly string[] _smallParts = ["Hello", " ", "World", "!"];
    private readonly string[] _mediumParts = [
        "The", " ", "quick", " ", "brown", " ", "fox", " ",
        "jumps", " ", "over", " ", "the", " ", "lazy", " ", "dog", "."
    ];
    private readonly string[] _largeParts;

    public ContentBuilderBenchmark()
    {
        _largeParts = new string[100];
        for (var i = 0; i < _largeParts.Length; i++)
        {
            _largeParts[i] = i % 10 == 0 ? $"Part{i}" : " ";
        }
    }

    [Benchmark(Baseline = true, Description = "Builder: small additions")]
    public Content Builder_SmallAdditions()
    {
        using var builder = new Content.Builder(10);
        builder.Add("Hello");
        builder.Add(" ");
        builder.Add("World");
        builder.Add("!");
        return builder.ToContent();
    }

    [Benchmark(Description = "Builder: medium additions")]
    public Content Builder_MediumAdditions()
    {
        using var builder = new Content.Builder(20);
        foreach (var part in _mediumParts)
        {
            builder.Add(part);
        }

        return builder.ToContent();
    }

    [Benchmark(Description = "Builder: many small additions")]
    public Content Builder_ManySmallAdditions()
    {
        using var builder = new Content.Builder(50);
        for (var i = 0; i < 25; i++)
        {
            builder.Add($"Part{i}");
            builder.Add(" ");
        }

        return builder.ToContent();
    }

    [Benchmark(Description = "Builder: with flatten")]
    public Content Builder_WithFlatten()
    {
        using var builder = new Content.Builder(10, flatten: true);
        var nestedContent = new Content(["A", "B", "C"]);
        builder.Add(nestedContent);
        builder.Add(new Content(["D", "E", "F"]));
        return builder.ToContent();
    }

    [Benchmark(Description = "Builder: mixed types")]
    public Content Builder_MixedTypes()
    {
        using var builder = new Content.Builder(10);
        builder.Add(new Content("String"));
        builder.Add("Raw".AsMemory());
        builder.Add("Direct");
        return builder.ToContent();
    }

    [Benchmark(Description = "Builder: zero capacity")]
    public Content Builder_ZeroCapacity()
    {
        using var builder = new Content.Builder(0);
        builder.Add("Hello");
        builder.Add(" ");
        builder.Add("World");
        return builder.ToContent();
    }

    [Benchmark(Description = "Join: small string array")]
    public Content Join_SmallStringArray()
    {
        var separator = new Content(", ");
        return Content.Join(separator, _smallParts.AsSpan());
    }

    [Benchmark(Description = "Join: medium string array")]
    public Content Join_MediumStringArray()
    {
        var separator = new Content(", ");
        return Content.Join(separator, _mediumParts.AsSpan());
    }

    [Benchmark(Description = "Join: large string array")]
    public Content Join_LargeStringArray()
    {
        var separator = new Content(", ");
        return Content.Join(separator, _largeParts.AsSpan());
    }

    [Benchmark(Description = "Join: Content array")]
    public Content Join_ContentArray()
    {
        var separator = new Content(" | ");
        var contentParts = _smallParts.Select(p => new Content(p)).ToArray();
        return Content.Join(separator, contentParts.AsSpan());
    }

    [Benchmark(Description = "Join: empty separator")]
    public Content Join_EmptySeparator()
    {
        var separator = Content.Empty;
        return Content.Join(separator, _mediumParts.AsSpan());
    }

    [Benchmark(Description = "Join: enumerable")]
    public Content Join_Enumerable()
    {
        var separator = new Content(" - ");
        return Content.Join(separator, _smallParts.AsEnumerable());
    }

    [Benchmark(Description = "Join: memory array")]
    public Content Join_MemoryArray()
    {
        var separator = new Content(" :: ");
        var memoryParts = _smallParts.Select(s => s.AsMemory()).ToArray();
        return Content.Join(separator, memoryParts.AsSpan());
    }
}
