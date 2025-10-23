// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

/// <summary>
/// Focused benchmarks for Content enumeration operations (< 5 minutes)
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[BenchmarkCategory("Fast", "Enumeration")]
public class ContentEnumerationBenchmark
{
    private readonly Content _shortContent;
    private readonly Content _mediumContent;
    private readonly Content _smallMultiPartContent;
    private readonly Content _mediumMultiPartContent;
    private readonly Content _largeMultiPartContent;
    private readonly Content _nestedContent;

    public ContentEnumerationBenchmark()
    {
        _shortContent = new Content("Hello World");
        _mediumContent = new Content("The quick brown fox jumps over the lazy dog. Lorem ipsum dolor sit amet.");

        _smallMultiPartContent = new Content(["Hello", " ", "World", "!"]);
        _mediumMultiPartContent = new Content([
            "The", " ", "quick", " ", "brown", " ", "fox", " ",
            "jumps", " ", "over", " ", "the", " ", "lazy", " ", "dog", "."
        ]);

        // Smaller large content for faster benchmarking
        var largeParts = new string[200];
        for (var i = 0; i < largeParts.Length; i++)
        {
            largeParts[i] = i % 10 == 0 ? $"Part{i}" : " ";
        }
        _largeMultiPartContent = new Content(largeParts.ToImmutableArray());

        var level1 = _smallMultiPartContent;
        _nestedContent = new Content([level1, new Content(" - "), _mediumMultiPartContent]);
    }

    [Benchmark(Baseline = true, Description = "Char enum: short single")]
    public int CharEnumeration_ShortSingle()
    {
        var count = 0;
        foreach (var _ in _shortContent)
        {
            count++;
        }

        return count;
    }

    [Benchmark(Description = "Char enum: medium single")]
    public int CharEnumeration_MediumSingle()
    {
        var count = 0;
        foreach (var _ in _mediumContent)
        {
            count++;
        }

        return count;
    }

    [Benchmark(Description = "Char enum: small multi-part")]
    public int CharEnumeration_SmallMultiPart()
    {
        var count = 0;
        foreach (var _ in _smallMultiPartContent)
        {
            count++;
        }

        return count;
    }

    [Benchmark(Description = "Char enum: medium multi-part")]
    public int CharEnumeration_MediumMultiPart()
    {
        var count = 0;
        foreach (var _ in _mediumMultiPartContent)
        {
            count++;
        }

        return count;
    }

    [Benchmark(Description = "Char enum: large multi-part")]
    public int CharEnumeration_LargeMultiPart()
    {
        var count = 0;
        foreach (var _ in _largeMultiPartContent)
        {
            count++;
        }

        return count;
    }

    [Benchmark(Description = "Char enum: nested")]
    public int CharEnumeration_Nested()
    {
        var count = 0;
        foreach (var _ in _nestedContent)
        {
            count++;
        }

        return count;
    }

    [Benchmark(Description = "Parts enum: small multi-part")]
    public int PartsEnumeration_SmallMultiPart()
    {
        var count = 0;
        foreach (var _ in _smallMultiPartContent.Parts)
        {
            count++;
        }

        return count;
    }

    [Benchmark(Description = "Parts enum: medium multi-part")]
    public int PartsEnumeration_MediumMultiPart()
    {
        var count = 0;
        foreach (var _ in _mediumMultiPartContent.Parts)
        {
            count++;
        }

        return count;
    }

    [Benchmark(Description = "Parts enum: large multi-part")]
    public int PartsEnumeration_LargeMultiPart()
    {
        var count = 0;
        foreach (var _ in _largeMultiPartContent.Parts)
        {
            count++;
        }

        return count;
    }

    [Benchmark(Description = "Parts enum: nested")]
    public int PartsEnumeration_Nested()
    {
        var count = 0;
        foreach (var _ in _nestedContent.Parts)
        {
            count++;
        }

        return count;
    }

    [Benchmark(Description = "NonEmpty parts: medium")]
    public int NonEmptyPartsEnumeration_Medium()
    {
        var count = 0;
        foreach (var _ in _mediumMultiPartContent.Parts.NonEmpty)
        {
            count++;
        }

        return count;
    }

    [Benchmark(Description = "NonEmpty parts: large")]
    public int NonEmptyPartsEnumeration_Large()
    {
        var count = 0;
        foreach (var _ in _largeMultiPartContent.Parts.NonEmpty)
        {
            count++;
        }

        return count;
    }
}
