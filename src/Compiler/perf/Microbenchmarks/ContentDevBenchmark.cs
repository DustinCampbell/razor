// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

/// <summary>
/// Quick development benchmarks for Content operations (< 2 minutes)
/// Use this for rapid iteration during active development.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 3)]
[BenchmarkCategory("Fast", "Dev")]
public class ContentDevBenchmark
{
    private readonly Content _shortContent = new("Hello World");
    private readonly Content _multiPartContent = new(["Hello", " ", "World"]);
    private readonly Content _nestedContent;
    
    public ContentDevBenchmark()
    {
        var inner = new Content(["Quick", " ", "Test"]);
        _nestedContent = new Content([inner, new Content(" Data")]);
    }
    
    [Benchmark(Baseline = true, Description = "ToString() single value")]
    public string ToString_Single() => _shortContent.ToString();
    
    [Benchmark(Description = "ToString() multi-part")]
    public string ToString_MultiPart() => _multiPartContent.ToString();
    
    [Benchmark(Description = "ToString() nested")]
    public string ToString_Nested() => _nestedContent.ToString();
    
    [Benchmark(Description = "Equality check")]
    public bool Equals_Single() => _shortContent.Equals(new Content("Hello World"));
    
    [Benchmark(Description = "Character enumeration")]
    public int CharEnum_MultiPart()
    {
        var count = 0;
        foreach (var _ in _multiPartContent)
        {
            count++;
        }

        return count;
    }
    
    [Benchmark(Description = "Parts enumeration")]
    public int PartsEnum_Nested()
    {
        var count = 0;
        foreach (var _ in _nestedContent.Parts)
        {
            count++;
        }

        return count;
    }
    
    [Benchmark(Description = "Slice operation")]
    public Content Slice_Single() => _shortContent.Slice(6, 5);
    
    [Benchmark(Description = "Replace operation")]
    public Content Replace_Single() => _shortContent.Replace("World", "Universe");
    
    [Benchmark(Description = "Builder small")]
    public Content Builder_Small()
    {
        using var builder = new Content.Builder(5);
        builder.Add("Hello");
        builder.Add(" ");
        builder.Add("World");
        return builder.ToContent();
    }
    
    [Benchmark(Description = "Join operation")]
    public Content Join_Small()
    {
        var separator = new Content(", ");
        return Content.Join(separator, ["A", "B", "C"]);
    }
}
