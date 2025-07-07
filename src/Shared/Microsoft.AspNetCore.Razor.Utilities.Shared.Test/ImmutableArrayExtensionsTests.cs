// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test;

public class ImmutableArrayExtensionsTests
{
    [Fact]
    public void GetMostRecentUniqueItems()
    {
        ImmutableArray<string> items =
        [
            "Hello",
            "HELLO",
            "HeLlO",
            new string([',', ' ']),
            new string([',', ' ']),
            "World",
            "WORLD",
            "WoRlD"
        ];

        var mostRecent = items.GetMostRecentUniqueItems(StringComparer.OrdinalIgnoreCase);

        Assert.Collection(mostRecent,
            s => Assert.Equal("HeLlO", s),
            s =>
            {
                // make sure it's the most recent ", "
                Assert.NotSame(items[3], s);
                Assert.Same(items[4], s);
            },
            s => Assert.Equal("WoRlD", s));
    }

    [Fact]
    public void SelectAsArray()
    {
        ImmutableArray<int> data = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        ImmutableArray<int> expected = [2, 4, 6, 8, 10, 12, 14, 16, 18, 20];

        var actual = data.SelectAsArray(static x => x * 2);
        Assert.Equal<int>(expected, actual);
    }

    [Fact]
    public void SelectAsArray_ReadOnlyList()
    {
        ImmutableArray<int> data = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        ImmutableArray<int> expected = [2, 4, 6, 8, 10, 12, 14, 16, 18, 20];

        var list = (IReadOnlyList<int>)data;

        var actual = list.SelectAsArray(static x => x * 2);
        Assert.Equal<int>(expected, actual);
    }

    [Fact]
    public void SelectAsArray_Enumerable()
    {
        ImmutableArray<int> data = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        ImmutableArray<int> expected = [2, 4, 6, 8, 10, 12, 14, 16, 18, 20];

        var enumerable = (IEnumerable<int>)data;

        var actual = enumerable.SelectAsArray(static x => x * 2);
        Assert.Equal<int>(expected, actual);
    }

    [Fact]
    public void SelectAsArray_Index()
    {
        ImmutableArray<int> data = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        ImmutableArray<int> expected = [1, 3, 5, 7, 9, 11, 13, 15, 17, 19];

        var actual = data.SelectAsArray(static (x, index) => x + index);
        Assert.Equal<int>(expected, actual);
    }

    [Fact]
    public void SelectAsArray_Index_ReadOnlyList()
    {
        ImmutableArray<int> data = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        ImmutableArray<int> expected = [1, 3, 5, 7, 9, 11, 13, 15, 17, 19];

        var list = (IReadOnlyList<int>)data;

        var actual = list.SelectAsArray(static (x, index) => x + index);
        Assert.Equal<int>(expected, actual);
    }

    [Fact]
    public void SelectAsArray_Index_Enumerable()
    {
        ImmutableArray<int> data = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        ImmutableArray<int> expected = [1, 3, 5, 7, 9, 11, 13, 15, 17, 19];

        var enumerable = (IEnumerable<int>)data;

        var actual = enumerable.SelectAsArray(static (x, index) => x + index);
        Assert.Equal<int>(expected, actual);
    }

    [Fact]
    public void InsertRange_EmptySpan_DoesNotModifyBuilder()
    {
        // Arrange
        var builder = ImmutableArray.CreateBuilder<int>();
        builder.Add(1);
        builder.Add(2);
        var originalCount = builder.Count;
        
        // Act
        builder.InsertRange(1, ReadOnlySpan<int>.Empty);
        
        // Assert
        Assert.Equal(originalCount, builder.Count);
        Assert.Equal(1, builder[0]);
        Assert.Equal(2, builder[1]);
    }
    
    [Fact]
    public void InsertRange_AtEndOfBuilder_AppendsItems()
    {
        // Arrange
        var builder = ImmutableArray.CreateBuilder<int>();
        builder.Add(1);
        builder.Add(2);
        var itemsToInsert = new[] { 3, 4, 5 };
        
        // Act
        builder.InsertRange(builder.Count, itemsToInsert.AsSpan());
        
        // Assert
        Assert.Equal(5, builder.Count);
        Assert.Equal([1, 2, 3, 4, 5], builder.ToArray());
    }
    
    [Fact]
    public void InsertRange_SingleItem_InsertsCorrectly()
    {
        // Arrange
        var builder = ImmutableArray.CreateBuilder<int>();
        builder.Add(1);
        builder.Add(3);
        var itemToInsert = new[] { 2 };
        
        // Act
        builder.InsertRange(1, itemToInsert.AsSpan());
        
        // Assert
        Assert.Equal(3, builder.Count);
        Assert.Equal([1, 2, 3], builder.ToArray());
    }
    
    [Fact]
    public void InsertRange_MultipleItems_InsertsCorrectly()
    {
        // Arrange
        var builder = ImmutableArray.CreateBuilder<int>();
        builder.Add(1);
        builder.Add(6);
        var itemsToInsert = new[] { 2, 3, 4, 5 };
        
        // Act
        builder.InsertRange(1, itemsToInsert.AsSpan());
        
        // Assert
        Assert.Equal(6, builder.Count);
        Assert.Equal([1, 2, 3, 4, 5, 6], builder.ToArray());
    }
    
    [Fact]
    public void InsertRange_AtBeginning_InsertsCorrectly()
    {
        // Arrange
        var builder = ImmutableArray.CreateBuilder<int>();
        builder.Add(3);
        builder.Add(4);
        var itemsToInsert = new[] { 1, 2 };
        
        // Act
        builder.InsertRange(0, itemsToInsert.AsSpan());
        
        // Assert
        Assert.Equal(4, builder.Count);
        Assert.Equal([1, 2, 3, 4], builder.ToArray());
    }
    
    [Fact]
    public void InsertRange_NegativeIndex_ThrowsArgumentException()
    {
        // Arrange
        var builder = ImmutableArray.CreateBuilder<int>();
        builder.Add(1);
        var itemsToInsert = new[] { 2, 3 };
        
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.InsertRange(-1, itemsToInsert.AsSpan()));
    }
    
    [Fact]
    public void InsertRange_IndexGreaterThanCount_ThrowsArgumentException()
    {
        // Arrange
        var builder = ImmutableArray.CreateBuilder<int>();
        builder.Add(1);
        var itemsToInsert = new[] { 2, 3 };
        
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.InsertRange(builder.Count + 1, itemsToInsert.AsSpan()));
    }

    [Fact]
    public void InsertRange_WithReferenceTypes_InsertsCorrectly()
    {
        // Arrange
        var builder = ImmutableArray.CreateBuilder<string>();
        builder.Add("apple");
        builder.Add("banana");
        var itemsToInsert = new[] { "cherry", "date" };
        
        // Act
        builder.InsertRange(1, itemsToInsert.AsSpan());
        
        // Assert
        Assert.Equal(4, builder.Count);
        Assert.Equal(["apple", "cherry", "date", "banana"], builder.ToArray());
    }

    [Fact]
    public void ToJoinedString_EmptyArray_ReturnsEmptyString()
    {
        ImmutableArray<ReadOnlyMemory<char>> array = [];
        
        var result = array.ToJoinedString();
        
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToJoinedString_WithSeparator_EmptyArray_ReturnsEmptyString()
    {
        ImmutableArray<ReadOnlyMemory<char>> array = [];
        
        var result = array.ToJoinedString(", ");
        
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToJoinedString_SingleElement_ReturnsElement()
    {
        ImmutableArray<ReadOnlyMemory<char>> array = ["Hello".AsMemory()];
        
        var result = array.ToJoinedString();
        
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void ToJoinedString_WithSeparator_SingleElement_ReturnsElement()
    {
        ImmutableArray<ReadOnlyMemory<char>> array = ["Hello".AsMemory()];
        
        var result = array.ToJoinedString(", ");
        
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void ToJoinedString_MultipleElements_ConcatenatesWithoutSeparator()
    {
        ImmutableArray<ReadOnlyMemory<char>> array = [
            "Hello".AsMemory(),
            "World".AsMemory(),
            "Test".AsMemory()
        ];
        
        var result = array.ToJoinedString();
        
        Assert.Equal("HelloWorldTest", result);
    }

    [Fact]
    public void ToJoinedString_WithSeparator_MultipleElements_ConcatenatesWithSeparator()
    {
        ImmutableArray<ReadOnlyMemory<char>> array = [
            "Hello".AsMemory(),
            "World".AsMemory(),
            "Test".AsMemory()
        ];
        
        var result = array.ToJoinedString(", ");
        
        Assert.Equal("Hello, World, Test", result);
    }

    [Fact]
    public void ToJoinedString_WithEmptyElements_SkipsEmptyElements()
    {
        ImmutableArray<ReadOnlyMemory<char>> array = [
            "Hello".AsMemory(),
            ReadOnlyMemory<char>.Empty,
            "World".AsMemory(),
            "".AsMemory(),
            "Test".AsMemory()
        ];
        
        var result = array.ToJoinedString();
        
        Assert.Equal("HelloWorldTest", result);
    }

    [Fact]
    public void ToJoinedString_WithSeparator_WithEmptyElements_SkipsEmptyElements()
    {
        ImmutableArray<ReadOnlyMemory<char>> array = [
            "Hello".AsMemory(),
            ReadOnlyMemory<char>.Empty,
            "World".AsMemory(),
            "".AsMemory(),
            "Test".AsMemory()
        ];
        
        var result = array.ToJoinedString(", ");
        
        Assert.Equal("Hello, World, Test", result);
    }

    [Fact]
    public void ToJoinedString_AllEmptyElements_ReturnsEmptyString()
    {
        ImmutableArray<ReadOnlyMemory<char>> array = [
            ReadOnlyMemory<char>.Empty,
            "".AsMemory(),
            ReadOnlyMemory<char>.Empty
        ];
        
        var result = array.ToJoinedString();
        
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToJoinedString_WithSeparator_AllEmptyElements_ReturnsEmptyString()
    {
        ImmutableArray<ReadOnlyMemory<char>> array = [
            ReadOnlyMemory<char>.Empty,
            "".AsMemory(),
            ReadOnlyMemory<char>.Empty
        ];
        
        var result = array.ToJoinedString(", ");
        
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToJoinedString_WithComplexSeparator_WorksCorrectly()
    {
        ImmutableArray<ReadOnlyMemory<char>> array = [
            "A".AsMemory(),
            "B".AsMemory(),
            "C".AsMemory()
        ];
        
        var result = array.ToJoinedString(" -> ");
        
        Assert.Equal("A -> B -> C", result);
    }

    [Fact]
    public void ToJoinedString_WithSubstring_WorksCorrectly()
    {
        var source = "HelloWorldTest";
        ImmutableArray<ReadOnlyMemory<char>> array = [
            source.AsMemory(0, 5),  // "Hello"
            source.AsMemory(5, 5),  // "World"
            source.AsMemory(10, 4)  // "Test"
        ];
        
        var result = array.ToJoinedString(", ");
        
        Assert.Equal("Hello, World, Test", result);
    }

    [Fact]
    public void ToJoinedString_Builder_EmptyBuilder_ReturnsEmptyString()
    {
        var builder = ImmutableArray.CreateBuilder<ReadOnlyMemory<char>>();
        
        var result = builder.ToJoinedString();
        
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToJoinedString_Builder_WithSeparator_EmptyBuilder_ReturnsEmptyString()
    {
        var builder = ImmutableArray.CreateBuilder<ReadOnlyMemory<char>>();
        
        var result = builder.ToJoinedString(", ");
        
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToJoinedString_Builder_SingleElement_ReturnsElement()
    {
        var builder = ImmutableArray.CreateBuilder<ReadOnlyMemory<char>>();
        builder.Add("Hello".AsMemory());
        
        var result = builder.ToJoinedString();
        
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void ToJoinedString_Builder_WithSeparator_SingleElement_ReturnsElement()
    {
        var builder = ImmutableArray.CreateBuilder<ReadOnlyMemory<char>>();
        builder.Add("Hello".AsMemory());
        
        var result = builder.ToJoinedString(", ");
        
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void ToJoinedString_Builder_MultipleElements_ConcatenatesWithoutSeparator()
    {
        var builder = ImmutableArray.CreateBuilder<ReadOnlyMemory<char>>();
        builder.Add("Hello".AsMemory());
        builder.Add("World".AsMemory());
        builder.Add("Test".AsMemory());
        
        var result = builder.ToJoinedString();
        
        Assert.Equal("HelloWorldTest", result);
    }

    [Fact]
    public void ToJoinedString_Builder_WithSeparator_MultipleElements_ConcatenatesWithSeparator()
    {
        var builder = ImmutableArray.CreateBuilder<ReadOnlyMemory<char>>();
        builder.Add("Hello".AsMemory());
        builder.Add("World".AsMemory());
        builder.Add("Test".AsMemory());
        
        var result = builder.ToJoinedString(", ");
        
        Assert.Equal("Hello, World, Test", result);
    }

    [Fact]
    public void ToJoinedString_Builder_WithEmptyElements_SkipsEmptyElements()
    {
        var builder = ImmutableArray.CreateBuilder<ReadOnlyMemory<char>>();
        builder.Add("Hello".AsMemory());
        builder.Add(ReadOnlyMemory<char>.Empty);
        builder.Add("World".AsMemory());
        builder.Add("".AsMemory());
        builder.Add("Test".AsMemory());
        
        var result = builder.ToJoinedString();
        
        Assert.Equal("HelloWorldTest", result);
    }

    [Fact]
    public void ToJoinedString_Builder_WithSeparator_WithEmptyElements_SkipsEmptyElements()
    {
        var builder = ImmutableArray.CreateBuilder<ReadOnlyMemory<char>>();
        builder.Add("Hello".AsMemory());
        builder.Add(ReadOnlyMemory<char>.Empty);
        builder.Add("World".AsMemory());
        builder.Add("".AsMemory());
        builder.Add("Test".AsMemory());
        
        var result = builder.ToJoinedString(", ");
        
        Assert.Equal("Hello, World, Test", result);
    }

    [Fact]
    public void ToJoinedString_Builder_AllEmptyElements_ReturnsEmptyString()
    {
        var builder = ImmutableArray.CreateBuilder<ReadOnlyMemory<char>>();
        builder.Add(ReadOnlyMemory<char>.Empty);
        builder.Add("".AsMemory());
        builder.Add(ReadOnlyMemory<char>.Empty);
        
        var result = builder.ToJoinedString();
        
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToJoinedString_Builder_WithSeparator_AllEmptyElements_ReturnsEmptyString()
    {
        var builder = ImmutableArray.CreateBuilder<ReadOnlyMemory<char>>();
        builder.Add(ReadOnlyMemory<char>.Empty);
        builder.Add("".AsMemory());
        builder.Add(ReadOnlyMemory<char>.Empty);
        
        var result = builder.ToJoinedString(", ");
        
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToJoinedString_LargeArray_WorksCorrectly()
    {
        var builder = ImmutableArray.CreateBuilder<ReadOnlyMemory<char>>();

        for (var i = 0; i < 1000; i++)
        {
            builder.Add(i.ToString().AsMemory());
        }

        var array = builder.ToImmutable();
        
        var result = array.ToJoinedString(",");
        
        var expected = string.Join(",", Enumerable.Range(0, 1000));
        Assert.Equal(expected, result);
    }
}
