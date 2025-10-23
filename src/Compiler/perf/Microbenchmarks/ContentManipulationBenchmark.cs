// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

/// <summary>
/// Focused benchmarks for Content manipulation operations (< 5 minutes)
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[BenchmarkCategory("Fast", "Manipulation")]
public class ContentManipulationBenchmark
{
    private readonly Content _shortContent;
    private readonly Content _mediumContent;
    private readonly Content _smallMultiPartContent;
    private readonly Content _mediumMultiPartContent;

    public ContentManipulationBenchmark()
    {
        _shortContent = new Content("Hello World");
        _mediumContent = new Content("The quick brown fox jumps over the lazy dog. Lorem ipsum dolor sit amet.");
        _smallMultiPartContent = new Content(["Hello", " ", "World", "!"]);
        _mediumMultiPartContent = new Content([
            "The", " ", "quick", " ", "brown", " ", "fox", " ",
            "jumps", " ", "over", " ", "the", " ", "lazy", " ", "dog", "."
        ]);
    }

    [Benchmark(Baseline = true, Description = "Slice: short single")]
    public Content Slice_ShortSingle() => _shortContent.Slice(6, 5);

    [Benchmark(Description = "Slice: medium single")]
    public Content Slice_MediumSingle() => _mediumContent.Slice(10, 20);

    [Benchmark(Description = "Slice: small multi-part")]
    public Content Slice_SmallMultiPart() => _smallMultiPartContent.Slice(2, 6);

    [Benchmark(Description = "Slice: medium multi-part")]
    public Content Slice_MediumMultiPart() => _mediumMultiPartContent.Slice(5, 10);

    [Benchmark(Description = "Insert: short single")]
    public Content Insert_ShortSingle() => _shortContent.Insert(6, "Beautiful ");

    [Benchmark(Description = "Insert: medium single")]
    public Content Insert_MediumSingle() => _mediumContent.Insert(10, " INSERTED ");

    [Benchmark(Description = "Insert: small multi-part")]
    public Content Insert_SmallMultiPart() => _smallMultiPartContent.Insert(5, " INSERTED ");

    [Benchmark(Description = "Insert: medium multi-part")]
    public Content Insert_MediumMultiPart() => _mediumMultiPartContent.Insert(8, " INSERTED ");

    [Benchmark(Description = "Replace: short single")]
    public Content Replace_ShortSingle() => _shortContent.Replace("World", "Universe");

    [Benchmark(Description = "Replace: medium single")]
    public Content Replace_MediumSingle() => _mediumContent.Replace("fox", "cat");

    [Benchmark(Description = "Replace: small multi-part")]
    public Content Replace_SmallMultiPart() => _smallMultiPartContent.Replace("World", "Universe");

    [Benchmark(Description = "Replace: medium multi-part")]
    public Content Replace_MediumMultiPart() => _mediumMultiPartContent.Replace("the", "THE");

    [Benchmark(Description = "Replace char: single")]
    public Content ReplaceChar_Single() => _mediumContent.Replace('o', 'a');

    [Benchmark(Description = "Replace char: multi-part")]
    public Content ReplaceChar_MultiPart() => _mediumMultiPartContent.Replace('o', 'a');

    [Benchmark(Description = "Remove: short single")]
    public Content Remove_ShortSingle() => _shortContent.Remove(6, 5);

    [Benchmark(Description = "Remove: medium single")]
    public Content Remove_MediumSingle() => _mediumContent.Remove(10, 20);

    [Benchmark(Description = "Remove: small multi-part")]
    public Content Remove_SmallMultiPart() => _smallMultiPartContent.Remove(2, 4);

    [Benchmark(Description = "Remove: medium multi-part")]
    public Content Remove_MediumMultiPart() => _mediumMultiPartContent.Remove(3, 6);

    [Benchmark(Description = "Chained operations")]
    public Content ChainedOperations()
    {
        return _mediumContent
            .Insert(10, " INSERTED ")
            .Replace("fox", "cat")
            .Remove(0, 4);
    }
}
