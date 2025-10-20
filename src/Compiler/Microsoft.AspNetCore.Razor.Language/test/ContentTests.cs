// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class ContentTests
{
    [Fact]
    public void Constructor_WithReadOnlyMemory_CreatesContent()
    {
        // Arrange
        var memory = "Hello".AsMemory();

        // Act
        var content = new Content(memory);

        // Assert
        Assert.False(content.IsEmpty);
        Assert.True(content.HasValue);
        Assert.False(content.IsMultiPart);
        Assert.Equal(5, content.Length);
        Assert.Equal("Hello", content.Value.ToString());
    }

    [Fact]
    public void Constructor_WithString_CreatesContent()
    {
        // Arrange & Act
        var content = new Content("World");

        // Assert
        Assert.False(content.IsEmpty);
        Assert.True(content.HasValue);
        Assert.False(content.IsMultiPart);
        Assert.Equal(5, content.Length);
        Assert.Equal("World", content.Value.ToString());
    }

    [Fact]
    public void Constructor_WithNullString_CreatesEmptyContent()
    {
        // Arrange & Act
        var content = new Content((string?)null);

        // Assert
        Assert.True(content.IsEmpty);
        Assert.False(content.HasValue);
        Assert.False(content.IsMultiPart);
        Assert.Equal(0, content.Length);
    }

    [Fact]
    public void Constructor_WithEmptyString_CreatesEmptyContent()
    {
        // Arrange & Act
        var content = new Content(string.Empty);

        // Assert
        Assert.True(content.IsEmpty);
        Assert.False(content.HasValue);
        Assert.False(content.IsMultiPart);
        Assert.Equal(0, content.Length);
    }

    [Fact]
    public void Constructor_WithImmutableArrayOfContent_CreatesContent()
    {
        // Arrange
        ImmutableArray<Content> parts = ["Hello", " ", "World"];

        // Act
        var content = new Content(parts);

        // Assert
        Assert.False(content.IsEmpty);
        Assert.False(content.HasValue);
        Assert.True(content.IsMultiPart);
        Assert.Equal(11, content.Length);
        Assert.Equal(3, content.Parts.Count);
    }

    [Fact]
    public void Constructor_WithImmutableArrayOfMemory_CreatesContent()
    {
        // Arrange
        ImmutableArray<ReadOnlyMemory<char>> parts = [
            "Hello".AsMemory(),
            " ".AsMemory(),
            "World".AsMemory()];

        // Act
        var content = new Content(parts);

        // Assert
        Assert.False(content.IsEmpty);
        Assert.False(content.HasValue);
        Assert.True(content.IsMultiPart);
        Assert.Equal(11, content.Length);
        Assert.Equal(3, content.Parts.Count);
    }

    [Fact]
    public void Constructor_WithImmutableArrayOfStrings_CreatesContent()
    {
        // Arrange
        ImmutableArray<string> parts = ["Hello", " ", "World"];

        // Act
        var content = new Content(parts);

        // Assert
        Assert.False(content.IsEmpty);
        Assert.False(content.HasValue);
        Assert.True(content.IsMultiPart);
        Assert.Equal(11, content.Length);
        Assert.Equal(3, content.Parts.Count);
    }

    [Fact]
    public void Constructor_WithEmptyImmutableArray_CreatesEmptyContent()
    {
        // Arrange
        var parts = ImmutableArray<string>.Empty;

        // Act
        var content = new Content(parts);

        // Assert
        Assert.True(content.IsEmpty);
        Assert.False(content.HasValue);
        Assert.False(content.IsMultiPart);
        Assert.Equal(0, content.Length);
    }

    [Fact]
    public void Constructor_WithSingleItemImmutableArray_FlattenedToValue()
    {
        // Arrange
        ImmutableArray<string> parts = ["Hello"];

        // Act
        var content = new Content(parts);

        // Assert
        Assert.False(content.IsEmpty);
        Assert.True(content.HasValue);
        Assert.False(content.IsMultiPart);
        Assert.Equal(5, content.Length);
        Assert.Equal("Hello", content.Value.ToString());
    }

    [Fact]
    public void Constructor_WithDeeplyNestedContent_HandlesCorrectly()
    {
        // Arrange
        var level1 = new Content(["A", "B"]);
        var level2 = new Content([level1, new Content("C")]);
        var level3 = new Content([level2, new Content("D")]);

        // Act & Assert
        Assert.Equal(4, level3.Length);
        Assert.Equal(4, level3.Parts.Count);

        // Verify using enumerator
        var items = new List<string>();
        foreach (var part in level3.Parts)
        {
            items.Add(part.ToString());
        }

        Assert.Equal("A", items[0]);
        Assert.Equal("B", items[1]);
        Assert.Equal("C", items[2]);
        Assert.Equal("D", items[3]);
    }

    [Fact]
    public void Constructor_WithMixedContentTypes_HandlesCorrectly()
    {
        // Arrange
        var memoryParts = new Content(["A".AsMemory(), "B".AsMemory()]);
        var stringParts = new Content(["C", "D"]);
        var content = new Content([memoryParts, stringParts]);

        // Act & Assert
        Assert.Equal(4, content.Length);
        Assert.Equal(4, content.Parts.Count);
    }

    [Fact]
    public void Constructor_WithSingleNestedContent_FlattensToValue()
    {
        // Arrange
        var inner = new Content("Hello");
        ImmutableArray<Content> parts = [inner];

        // Act
        var content = new Content(parts);

        // Assert
        Assert.True(content.HasValue);
        Assert.False(content.IsMultiPart);
        Assert.Equal("Hello", content.Value.ToString());
    }

    [Fact]
    public void Empty_ReturnsEmptyContent()
    {
        // Act
        var content = Content.Empty;

        // Assert
        Assert.True(content.IsEmpty);
        Assert.False(content.HasValue);
        Assert.False(content.IsMultiPart);
        Assert.Equal(0, content.Length);
    }

    [Fact]
    public void Length_WithNestedContent_CalculatesCorrectly()
    {
        // Arrange
        var inner1 = new Content(["Hello", " "]);
        var inner2 = new Content(["World", "!"]);
        var content = new Content([inner1, inner2]);

        // Act
        var length = content.Length;

        // Assert
        Assert.Equal(12, length); // "Hello" (5) + " " (1) + "World" (5) + "!" (1)
    }

    [Fact]
    public void ImplicitOperator_FromString_CreatesContent()
    {
        // Act
        Content content = "Test";

        // Assert
        Assert.False(content.IsEmpty);
        Assert.True(content.HasValue);
        Assert.Equal(4, content.Length);
        Assert.Equal("Test", content.Value.ToString());
    }

    [Fact]
    public void ImplicitOperator_FromReadOnlyMemory_CreatesContent()
    {
        // Arrange
        var memory = "Test".AsMemory();

        // Act
        Content content = memory;

        // Assert
        Assert.False(content.IsEmpty);
        Assert.True(content.HasValue);
        Assert.Equal(4, content.Length);
        Assert.Equal("Test", content.Value.ToString());
    }

    [Fact]
    public void ImplicitOperator_FromImmutableArrayOfContent_CreatesContent()
    {
        // Arrange
        ImmutableArray<Content> parts = [new Content("A"), new Content("B")];

        // Act
        Content content = parts;

        // Assert
        Assert.False(content.IsEmpty);
        Assert.True(content.IsMultiPart);
        Assert.Equal(2, content.Length);
    }

    [Fact]
    public void Parts_WithSingleValue_ReturnsOneItem()
    {
        // Arrange
        var content = new Content("Hello");

        // Act
        var parts = content.Parts;
        var items = new List<string>();

        foreach (var part in parts)
        {
            items.Add(part.ToString());
        }

        // Assert
        Assert.Equal(1, parts.Count);
        Assert.Equal("Hello", items[0]);
    }

    [Fact]
    public void Parts_WithMultipleStrings_ReturnsAllItems()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act
        var parts = content.Parts;
        var items = new List<string>();

        foreach (var part in parts)
        {
            items.Add(part.ToString());
        }

        // Assert
        Assert.Equal(3, parts.Count);
        Assert.Equal("Hello", items[0]);
        Assert.Equal(" ", items[1]);
        Assert.Equal("World", items[2]);
    }

    [Fact]
    public void Parts_WithNestedContent_FlattensCorrectly()
    {
        // Arrange
        var inner1 = new Content(["A", "B"]);
        var inner2 = new Content(["C", "D"]);
        var content = new Content([inner1, inner2]);

        // Act
        var parts = content.Parts;
        var items = new List<string>();

        foreach (var part in parts)
        {
            items.Add(part.ToString());
        }

        // Assert
        Assert.Equal(4, parts.Count);
        Assert.Equal("A", items[0]);
        Assert.Equal("B", items[1]);
        Assert.Equal("C", items[2]);
        Assert.Equal("D", items[3]);
    }

    [Fact]
    public void Parts_WithEmptyContent_ReturnsZeroCount()
    {
        // Arrange
        var content = Content.Empty;

        // Act
        var parts = content.Parts;

        // Assert
        Assert.Equal(0, parts.Count);
    }

    [Fact]
    public void PartsEnumerator_WithSingleValue_EnumeratesOneItem()
    {
        // Arrange
        var content = new Content("Hello");
        var parts = content.Parts;

        // Act
        var enumerator = parts.GetEnumerator();

        // Assert
        Assert.True(enumerator.MoveNext());
        Assert.Equal("Hello", enumerator.Current.ToString());
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void PartsEnumerator_WithMultipleStrings_EnumeratesAllItems()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);
        var parts = content.Parts;
        var items = new List<string>();

        // Act
        foreach (var part in parts)
        {
            items.Add(part.ToString());
        }

        // Assert
        Assert.Equal(3, items.Count);
        Assert.Equal("Hello", items[0]);
        Assert.Equal(" ", items[1]);
        Assert.Equal("World", items[2]);
    }

    [Fact]
    public void PartsEnumerator_WithNestedContent_EnumeratesFlattened()
    {
        // Arrange
        var inner1 = new Content(["A", "B"]);
        var inner2 = new Content(["C", "D"]);
        var content = new Content([inner1, inner2]);
        var items = new List<string>();

        // Act
        foreach (var part in content.Parts)
        {
            items.Add(part.ToString());
        }

        // Assert
        Assert.Equal(4, items.Count);
        Assert.Equal("A", items[0]);
        Assert.Equal("B", items[1]);
        Assert.Equal("C", items[2]);
        Assert.Equal("D", items[3]);
    }

    [Fact]
    public void PartsEnumerator_WithEmptyContent_DoesNotEnumerate()
    {
        // Arrange
        var content = Content.Empty;
        var count = 0;

        // Act
        foreach (var _ in content.Parts)
        {
            count++;
        }

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void PartsEnumerator_ManualIteration_WorksCorrectly()
    {
        // Arrange
        var content = new Content(["A", "B", "C"]);
        using var enumerator = content.Parts.GetEnumerator();

        // Act & Assert
        Assert.True(enumerator.MoveNext());
        Assert.Equal("A", enumerator.Current.ToString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("B", enumerator.Current.ToString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("C", enumerator.Current.ToString());

        Assert.False(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext()); // Verify stays false
    }

    [Fact]
    public void PartsEnumerator_WithDeeplyNestedContent_EnumeratesCorrectly()
    {
        // Arrange
        var level1 = new Content(["A", "B"]);
        var level2 = new Content([level1, new Content("C")]);
        var level3 = new Content([level2, new Content("D")]);
        var items = new List<string>();

        // Act
        foreach (var part in level3.Parts)
        {
            items.Add(part.ToString());
        }

        // Assert
        Assert.Equal(4, items.Count);
        Assert.Equal("A", items[0]);
        Assert.Equal("B", items[1]);
        Assert.Equal("C", items[2]);
        Assert.Equal("D", items[3]);
    }

    [Fact]
    public void PartsEnumerator_WithComplexNestedStructure_EnumeratesCorrectly()
    {
        // Arrange
        // Create a complex nested structure to test enumerator edge cases:
        // Top level: [branch1, branch2, branch3, single]
        //   branch1: [leaf1, leaf2, nested1]
        //     nested1: [leaf3, leaf4]
        //   branch2: [nested2]
        //     nested2: [leaf5, nested3, leaf6]
        //       nested3: [leaf7, leaf8]
        //   branch3: [leaf9, nested4]
        //     nested4: [nested5]
        //       nested5: [leaf10, leaf11, leaf12]
        //   single: leaf13

        var leaf1 = new Content("1");
        var leaf2 = new Content("2");
        var leaf3 = new Content("3");
        var leaf4 = new Content("4");
        var nested1 = new Content([leaf3, leaf4]);
        var branch1 = new Content([leaf1, leaf2, nested1]);

        var leaf5 = new Content("5");
        var leaf6 = new Content("6");
        var leaf7 = new Content("7");
        var leaf8 = new Content("8");
        var nested3 = new Content([leaf7, leaf8]);
        var nested2 = new Content([leaf5, nested3, leaf6]);
        var branch2 = new Content([nested2]);

        var leaf9 = new Content("9");
        var leaf10 = new Content("10");
        var leaf11 = new Content("11");
        var leaf12 = new Content("12");
        var nested5 = new Content([leaf10, leaf11, leaf12]);
        var nested4 = new Content([nested5]);
        var branch3 = new Content([leaf9, nested4]);

        var single = new Content("13");

        var content = new Content([branch1, branch2, branch3, single]);

        var items = new List<string>();

        // Act
        foreach (var part in content.Parts)
        {
            items.Add(part.ToString());
        }

        // Assert
        Assert.Equal(13, content.Parts.Count);
        Assert.Equal(13, items.Count);

        // Verify correct order from depth-first traversal
        Assert.Equal("1", items[0]);
        Assert.Equal("2", items[1]);
        Assert.Equal("3", items[2]);
        Assert.Equal("4", items[3]);
        Assert.Equal("5", items[4]);
        Assert.Equal("7", items[5]);
        Assert.Equal("8", items[6]);
        Assert.Equal("6", items[7]);
        Assert.Equal("9", items[8]);
        Assert.Equal("10", items[9]);
        Assert.Equal("11", items[10]);
        Assert.Equal("12", items[11]);
        Assert.Equal("13", items[12]);
    }
}
