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
/// Comprehensive benchmarks for Content operations focusing on stress tests,
/// large datasets, and operations not covered by focused benchmarks (15-20 minutes)
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[BenchmarkCategory("Slow", "Comprehensive", "Stress")]
public class ContentComprehensiveBenchmark
{
    private readonly string _longString = string.Concat(Enumerable.Repeat(
        "This is a longer string used for performance testing of Content operations. " +
        "It contains multiple sentences and should be representative of real-world content. " +
        "The goal is to measure performance characteristics under realistic conditions. ", 50));

    private readonly string[] _largeParts;
    private readonly string[] _manySmallParts;

    private readonly Content _longContent;
    private readonly Content _largeMultiPartContent;
    private readonly Content _manySmallPartsContent;
    private readonly Content _deeplyNestedContent;

    public ContentComprehensiveBenchmark()
    {
        // Large datasets for stress testing
        _largeParts = new string[1000];
        for (var i = 0; i < _largeParts.Length; i++)
        {
            _largeParts[i] = i % 10 == 0 ? $"Part{i}" : " ";
        }

        _manySmallParts = new string[500];
        for (var i = 0; i < _manySmallParts.Length; i++)
        {
            _manySmallParts[i] = ((char)('A' + (i % 26))).ToString();
        }

        _longContent = new Content(_longString);
        _largeMultiPartContent = new Content(_largeParts.ToImmutableArray());
        _manySmallPartsContent = new Content(_manySmallParts.ToImmutableArray());

        // Create deeply nested content for stress testing
        var deep1 = new Content(["Deep", "1"]);
        var deep2 = new Content([deep1, new Content(["Deep", "2"])]);
        var deep3 = new Content([deep2, new Content(["Deep", "3"])]);
        _deeplyNestedContent = deep3;
    }

    #region String Operations - Large Scale
    
    [Benchmark(Baseline = true, Description = "ToString: long single-value")]
    public string ToString_LongSingle() => _longContent.ToString();

    [Benchmark(Description = "ToString: large multi-part")]
    public string ToString_LargeMultiPart() => _largeMultiPartContent.ToString();

    [Benchmark(Description = "ToString: many small parts")]
    public string ToString_ManySmallParts() => _manySmallPartsContent.ToString();

    #endregion

    #region Equality and Hashing - Missing from Focused Benchmarks

    [Benchmark(Description = "Equality: large multi-part")]
    public bool Equality_LargeMultiPart()
    {
        var other = new Content(_largeParts.ToImmutableArray());
        return _largeMultiPartContent.Equals(other);
    }

    [Benchmark(Description = "GetHashCode: long single")]
    public int GetHashCode_LongSingle() => _longContent.GetHashCode();

    [Benchmark(Description = "GetHashCode: large multi-part")]
    public int GetHashCode_LargeMultiPart() => _largeMultiPartContent.GetHashCode();

    [Benchmark(Description = "GetHashCode: many small parts")]
    public int GetHashCode_ManySmallParts() => _manySmallPartsContent.GetHashCode();

    #endregion

    #region Search Operations - Missing from Focused Benchmarks

    [Benchmark(Description = "IndexOf char: large multi-part")]
    public int IndexOf_CharInLargeMultiPart() => _largeMultiPartContent.IndexOf('P');

    [Benchmark(Description = "IndexOf substring: large multi-part")]
    public int IndexOf_SubstringInLargeMultiPart() => 
        _largeMultiPartContent.IndexOf("Part".AsSpan(), StringComparison.Ordinal);

    [Benchmark(Description = "Contains char: large multi-part")]
    public bool Contains_CharInLargeMultiPart() => _largeMultiPartContent.Contains('P');

    [Benchmark(Description = "ContainsAnyExcept: large multi-part")]
    public bool ContainsAnyExcept_LargeMultiPart() => 
        _largeMultiPartContent.ContainsAnyExcept([' ', '\t', '\n', '\r']);

    #endregion

    #region Utility Methods - Missing from Focused Benchmarks

    [Benchmark(Description = "IsWhiteSpace: large multi-part")]
    public bool IsWhiteSpace_LargeMultiPart() => _largeMultiPartContent.IsWhiteSpace();

    [Benchmark(Description = "IsNullOrEmpty check")]
    public bool IsNullOrEmpty_Check() => Content.IsNullOrEmpty(_longContent);

    [Benchmark(Description = "IsNullOrWhiteSpace check")]
    public bool IsNullOrWhiteSpace_Check() => Content.IsNullOrWhiteSpace(_longContent);

    #endregion

    #region Large Scale Construction - Stress Tests

    [Benchmark(Description = "Construction: long string")]
    public Content Construction_LongString() => new(_longString);

    [Benchmark(Description = "Construction: large parts array")]
    public Content Construction_LargeParts() => new(_largeParts.ToImmutableArray());

    [Benchmark(Description = "Construction: many small parts")]
    public Content Construction_ManySmallParts() => new(_manySmallParts.ToImmutableArray());

    #endregion

    #region Large Scale Enumeration - Stress Tests

    [Benchmark(Description = "Char enumeration: long single")]
    public int CharEnumeration_LongSingle()
    {
        var count = 0;
        foreach (var _ in _longContent)
        {
            count++;
        }

        return count;
    }

    [Benchmark(Description = "Char enumeration: large multi-part")]
    public int CharEnumeration_LargeMultiPart()
    {
        var count = 0;
        foreach (var _ in _largeMultiPartContent)
        {
            count++;
        }

        return count;
    }

    [Benchmark(Description = "Parts enumeration: large multi-part")]
    public int PartsEnumeration_LargeMultiPart()
    {
        var count = 0;
        foreach (var _ in _largeMultiPartContent.Parts)
        {
            count++;
        }

        return count;
    }

    [Benchmark(Description = "NonEmpty parts: large multi-part")]
    public int NonEmptyPartsEnumeration_LargeMultiPart()
    {
        var count = 0;
        foreach (var _ in _largeMultiPartContent.Parts.NonEmpty)
        {
            count++;
        }

        return count;
    }

    #endregion

    #region Large Scale Manipulation - Stress Tests

    [Benchmark(Description = "Slice: large multi-part")]
    public Content Slice_LargeMultiPart() => _largeMultiPartContent.Slice(100, 200);

    [Benchmark(Description = "Replace: large multi-part")]
    public Content Replace_LargeMultiPart() => _largeMultiPartContent.Replace(" ", "_");

    #endregion

    #region Builder Stress Tests

    [Benchmark(Description = "Builder: many additions")]
    public Content Builder_ManyAdditions()
    {
        using var builder = new Content.Builder(200);
        for (var i = 0; i < 100; i++)
        {
            builder.Add($"Part{i}");
            builder.Add(" ");
        }
        return builder.ToContent();
    }

    #endregion

    #region Join Stress Tests

    [Benchmark(Description = "Join: large string array")]
    public Content Join_LargeStringArray()
    {
        var separator = new Content(", ");
        return Content.Join(separator, _largeParts.AsSpan());
    }

    #endregion

    #region Memory and Performance Stress Tests

    [Benchmark(Description = "Deep nesting stress test")]
    public int DeepNesting_Performance()
    {
        var count = 0;
        foreach (var _ in _deeplyNestedContent.Parts)
        {
            count++;
        }

        return count;
    }

    [Benchmark(Description = "Complex nested enumeration")]
    public int ComplexNested_Enumeration()
    {
        // Create complex nested structure on-the-fly
        var branch1 = new Content(_largeParts.Take(100).ToImmutableArray());
        var branch2 = new Content(_manySmallParts.Take(100).ToImmutableArray());
        var complex = new Content([branch1, new Content(" -- "), branch2]);
        
        var count = 0;
        foreach (var _ in complex.Parts.NonEmpty)
        {
            count++;
        }

        return count;
    }

    [Benchmark(Description = "Memory allocation stress")]
    public Content MemoryAllocationStress()
    {
        // Test memory pressure scenarios
        var results = new Content[10];
        for (var i = 0; i < 10; i++)
        {
            results[i] = new Content(_largeParts.Skip(i * 100).Take(100).ToImmutableArray());
        }
        return new Content(results.ToImmutableArray());
    }

    #endregion
}
