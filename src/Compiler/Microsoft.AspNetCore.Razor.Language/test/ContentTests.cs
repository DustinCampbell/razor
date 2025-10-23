// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        Assert.True(content.IsSingleValue);
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
        Assert.True(content.IsSingleValue);
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
        Assert.False(content.IsSingleValue);
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
        Assert.False(content.IsSingleValue);
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
        Assert.False(content.IsSingleValue);
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
        Assert.False(content.IsSingleValue);
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
        Assert.False(content.IsSingleValue);
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
        Assert.False(content.IsSingleValue);
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
        Assert.True(content.IsSingleValue);
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
        Assert.True(content.IsSingleValue);
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
        Assert.False(content.IsSingleValue);
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
        Assert.True(content.IsSingleValue);
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
        Assert.True(content.IsSingleValue);
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
    public void Parts_EmptyContentHandling_BehavesConsistently()
    {
        // Arrange - Various scenarios with empty content
        var standaloneEmpty = Content.Empty;
        var standaloneEmptyString = new Content("");
        var standaloneEmptyMemory = new Content(ReadOnlyMemory<char>.Empty);

        var multiPartWithEmpty = new Content([Content.Empty, new Content("A"), Content.Empty, new Content("B"), Content.Empty]);
        var nestedEmptyContent = new Content([Content.Empty, new Content([Content.Empty, Content.Empty]), Content.Empty]);
        var mixedEmptyArrays = new Content(["", "A", "", "B", ""]);

        // Act & Assert - Standalone empty content should have Count = 0 and no enumeration
        Assert.Equal(0, standaloneEmpty.Parts.Count);
        Assert.Equal(0, standaloneEmptyString.Parts.Count);
        Assert.Equal(0, standaloneEmptyMemory.Parts.Count);

        var enumeratedCount = 0;
        foreach (var _ in standaloneEmpty.Parts)
        {
            enumeratedCount++;
        }

        Assert.Equal(0, enumeratedCount);

        // Multi-part content should include empty parts in both Count and enumeration
        Assert.Equal(5, multiPartWithEmpty.Parts.Count); // All 5 parts including empty ones

        var parts = new List<string>();
        foreach (var part in multiPartWithEmpty.Parts)
        {
            parts.Add(part.ToString());
        }

        Assert.Equal(5, parts.Count);
        Assert.Equal("", parts[0]); // Empty part
        Assert.Equal("A", parts[1]);
        Assert.Equal("", parts[2]); // Empty part  
        Assert.Equal("B", parts[3]);
        Assert.Equal("", parts[4]); // Empty part

        // Nested empty content should also be enumerated
        Assert.Equal(4, nestedEmptyContent.Parts.Count); // 4 empty parts total

        var nestedParts = new List<string>();
        foreach (var part in nestedEmptyContent.Parts)
        {
            nestedParts.Add(part.ToString());
        }

        Assert.Equal(4, nestedParts.Count);
        Assert.All(nestedParts, part => Assert.Equal("", part));

        // Mixed empty arrays should behave the same
        Assert.Equal(5, mixedEmptyArrays.Parts.Count);

        var mixedParts = new List<string>();
        foreach (var part in mixedEmptyArrays.Parts)
        {
            mixedParts.Add(part.ToString());
        }

        Assert.Equal(["", "A", "", "B", ""], mixedParts);
    }

    [Fact]
    public void Parts_CountMatchesEnumeration_ForAllContentTypes()
    {
        // Arrange - Various content structures
        var testCases = new[]
        {
            Content.Empty,
            new Content(""),
            new Content("Hello"),
            new Content(["A", "B", "C"]),
            new Content([Content.Empty, new Content("Test"), Content.Empty]),
            new Content(["", "Hello", "", "World", ""]),
            new Content([new Content(["A", "B"]), new Content(["C", "D"])]),
            new Content([ReadOnlyMemory<char>.Empty, "Test".AsMemory(), ReadOnlyMemory<char>.Empty])
        };

        foreach (var content in testCases)
        {
            // Act
            var reportedCount = content.Parts.Count;

            var actualCount = 0;
            foreach (var _ in content.Parts)
            {
                actualCount++;
            }

            // Assert
            Assert.Equal(reportedCount, actualCount);
        }
    }

    [Fact]
    public void Parts_EmptyPartsPreserveStructure()
    {
        // Arrange - Content that should maintain empty parts
        var content = new Content([
            new Content("Start"),
            Content.Empty,
            new Content([Content.Empty, new Content("Middle"), Content.Empty]),
            Content.Empty,
            new Content("End")
        ]);

        // Act
        var parts = new List<string>();
        foreach (var part in content.Parts)
        {
            parts.Add(part.ToString());
        }

        // Assert - Should preserve all parts including empty ones
        Assert.Equal(7, content.Parts.Count);
        Assert.Equal(7, parts.Count);
        Assert.Equal("Start", parts[0]);
        Assert.Equal("", parts[1]);    // Empty part preserved
        Assert.Equal("", parts[2]);    // Empty part from nested structure
        Assert.Equal("Middle", parts[3]);
        Assert.Equal("", parts[4]);    // Empty part from nested structure
        Assert.Equal("", parts[5]);    // Empty part preserved
        Assert.Equal("End", parts[6]);
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

    [Fact]
    public void PartsEnumerator_Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var content = new Content(["Test", "Data"]);
        var enumerator = content.Parts.GetEnumerator();

        // Act - Dispose multiple times should not throw
        enumerator.Dispose();
        enumerator.Dispose();
        enumerator.Dispose();

        // Assert - No exception thrown
    }

    [Fact]
    public void PartsEnumerator_AfterDispose_MoveNextReturnsFalse()
    {
        // Arrange
        var content = new Content(["Test", "Data"]);
        var enumerator = content.Parts.GetEnumerator();

        // Act
        enumerator.MoveNext(); // Move to first part
        enumerator.Dispose();
        var result = enumerator.MoveNext();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void NonEmptyParts_EmptyContent_ReturnsNoItems()
    {
        // Arrange
        var content = Content.Empty;

        // Act
        var nonEmptyParts = new List<string>();
        foreach (var part in content.Parts.NonEmpty)
        {
            nonEmptyParts.Add(part.ToString());
        }

        // Assert
        Assert.Empty(nonEmptyParts);
    }

    [Fact]
    public void NonEmptyParts_SingleNonEmptyValue_ReturnsOneItem()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var nonEmptyParts = new List<string>();
        foreach (var part in content.Parts.NonEmpty)
        {
            nonEmptyParts.Add(part.ToString());
        }

        // Assert
        Assert.Single(nonEmptyParts);
        Assert.Equal("Hello World", nonEmptyParts[0]);
    }

    [Fact]
    public void NonEmptyParts_SingleEmptyValue_ReturnsNoItems()
    {
        // Arrange
        var content = new Content("");

        // Act
        var nonEmptyParts = new List<string>();
        foreach (var part in content.Parts.NonEmpty)
        {
            nonEmptyParts.Add(part.ToString());
        }

        // Assert
        Assert.Empty(nonEmptyParts);
    }

    [Fact]
    public void NonEmptyParts_MultiPartAllNonEmpty_ReturnsAllItems()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World", "!"]);

        // Act
        var nonEmptyParts = new List<string>();
        foreach (var part in content.Parts.NonEmpty)
        {
            nonEmptyParts.Add(part.ToString());
        }

        // Assert
        Assert.Equal(4, nonEmptyParts.Count);
        Assert.Equal("Hello", nonEmptyParts[0]);
        Assert.Equal(" ", nonEmptyParts[1]);
        Assert.Equal("World", nonEmptyParts[2]);
        Assert.Equal("!", nonEmptyParts[3]);
    }

    [Fact]
    public void NonEmptyParts_MultiPartWithEmptyParts_FiltersEmptyParts()
    {
        // Arrange
        var content = new Content(["", "Hello", "", " ", "", "World", ""]);

        // Act
        var nonEmptyParts = new List<string>();
        foreach (var part in content.Parts.NonEmpty)
        {
            nonEmptyParts.Add(part.ToString());
        }

        // Assert
        Assert.Equal(3, nonEmptyParts.Count);
        Assert.Equal("Hello", nonEmptyParts[0]);
        Assert.Equal(" ", nonEmptyParts[1]);
        Assert.Equal("World", nonEmptyParts[2]);
    }

    [Fact]
    public void NonEmptyParts_MultiPartAllEmpty_ReturnsNoItems()
    {
        // Arrange
        var content = new Content(["", "", "", ""]);

        // Act
        var nonEmptyParts = new List<string>();
        foreach (var part in content.Parts.NonEmpty)
        {
            nonEmptyParts.Add(part.ToString());
        }

        // Assert
        Assert.Empty(nonEmptyParts);
    }

    [Fact]
    public void NonEmptyParts_NestedContentWithEmptyParts_FiltersCorrectly()
    {
        // Arrange
        var inner1 = new Content(["", "A", ""]);
        var inner2 = new Content(["", "", "B", ""]);
        var content = new Content([inner1, new Content(""), inner2]);

        // Act
        var nonEmptyParts = new List<string>();
        foreach (var part in content.Parts.NonEmpty)
        {
            nonEmptyParts.Add(part.ToString());
        }

        // Assert
        Assert.Equal(2, nonEmptyParts.Count);
        Assert.Equal("A", nonEmptyParts[0]);
        Assert.Equal("B", nonEmptyParts[1]);
    }

    [Fact]
    public void NonEmptyParts_DeeplyNestedWithEmptyParts_FiltersCorrectly()
    {
        // Arrange
        var level1 = new Content(["", "Deep", ""]);
        var level2 = new Content([level1, new Content(""), new Content("Nested")]);
        var level3 = new Content([new Content(""), level2, new Content("Content")]);

        // Act
        var nonEmptyParts = new List<string>();
        foreach (var part in level3.Parts.NonEmpty)
        {
            nonEmptyParts.Add(part.ToString());
        }

        // Assert
        Assert.Equal(3, nonEmptyParts.Count);
        Assert.Equal("Deep", nonEmptyParts[0]);
        Assert.Equal("Nested", nonEmptyParts[1]);
        Assert.Equal("Content", nonEmptyParts[2]);
    }

    [Fact]
    public void NonEmptyParts_WithMemoryParts_FiltersEmptyMemory()
    {
        // Arrange
        var content = new Content([
            ReadOnlyMemory<char>.Empty,
            "Hello".AsMemory(),
            ReadOnlyMemory<char>.Empty,
            "World".AsMemory(),
            ReadOnlyMemory<char>.Empty
        ]);

        // Act
        var nonEmptyParts = new List<string>();
        foreach (var part in content.Parts.NonEmpty)
        {
            nonEmptyParts.Add(part.ToString());
        }

        // Assert
        Assert.Equal(2, nonEmptyParts.Count);
        Assert.Equal("Hello", nonEmptyParts[0]);
        Assert.Equal("World", nonEmptyParts[1]);
    }

    [Fact]
    public void NonEmptyParts_MixedContentTypes_FiltersCorrectly()
    {
        // Arrange
        var memoryContent = new Content("Memory".AsMemory());
        var stringContent = new Content("");
        var nestedContent = new Content(["", "Nested", ""]);
        var content = new Content([memoryContent, stringContent, nestedContent]);

        // Act
        var nonEmptyParts = new List<string>();
        foreach (var part in content.Parts.NonEmpty)
        {
            nonEmptyParts.Add(part.ToString());
        }

        // Assert
        Assert.Equal(2, nonEmptyParts.Count);
        Assert.Equal("Memory", nonEmptyParts[0]);
        Assert.Equal("Nested", nonEmptyParts[1]);
    }

    [Fact]
    public void NonEmptyParts_EnumeratorDispose_WorksCorrectly()
    {
        // Arrange
        var content = new Content(["", "Hello", "", "World", ""]);

        // Act & Assert - Should not throw
        using var enumerator = content.Parts.NonEmpty.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        Assert.Equal("Hello", enumerator.Current.ToString());
    }

    [Fact]
    public void NonEmptyParts_EnumeratorDisposeMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var content = new Content(["", "Test", ""]);
        var enumerator = content.Parts.NonEmpty.GetEnumerator();

        // Act - Multiple dispose calls should not throw
        enumerator.Dispose();
        enumerator.Dispose();
        enumerator.Dispose();

        // Assert - No exception thrown
    }

    [Fact]
    public void NonEmptyParts_EnumeratorAfterDispose_MoveNextReturnsFalse()
    {
        // Arrange
        var content = new Content(["", "Test", ""]);
        var enumerator = content.Parts.NonEmpty.GetEnumerator();

        // Act
        enumerator.MoveNext(); // Move to first non-empty part
        enumerator.Dispose();
        var result = enumerator.MoveNext();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void NonEmptyParts_ManualEnumeration_WorksCorrectly()
    {
        // Arrange
        var content = new Content(["", "A", "", "B", "", "C", ""]);
        using var enumerator = content.Parts.NonEmpty.GetEnumerator();

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
    public void NonEmptyParts_WithSingleCharacterParts_FiltersCorrectly()
    {
        // Arrange
        var content = new Content(["", "H", "", "e", "", "l", "", "l", "", "o", ""]);

        // Act
        var nonEmptyParts = new List<string>();
        foreach (var part in content.Parts.NonEmpty)
        {
            nonEmptyParts.Add(part.ToString());
        }

        // Assert
        Assert.Equal(5, nonEmptyParts.Count);
        Assert.Equal("H", nonEmptyParts[0]);
        Assert.Equal("e", nonEmptyParts[1]);
        Assert.Equal("l", nonEmptyParts[2]);
        Assert.Equal("l", nonEmptyParts[3]);
        Assert.Equal("o", nonEmptyParts[4]);
    }

    [Fact]
    public void NonEmptyParts_WithUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var content = new Content(["", "Hello", "", " ", "", "世界", ""]);

        // Act
        var nonEmptyParts = new List<string>();
        foreach (var part in content.Parts.NonEmpty)
        {
            nonEmptyParts.Add(part.ToString());
        }

        // Assert
        Assert.Equal(3, nonEmptyParts.Count);
        Assert.Equal("Hello", nonEmptyParts[0]);
        Assert.Equal(" ", nonEmptyParts[1]);
        Assert.Equal("世界", nonEmptyParts[2]);
    }

    [Fact]
    public void NonEmptyParts_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var content = new Content(["", "\t", "", "\n", "", "\r", ""]);

        // Act
        var nonEmptyParts = new List<string>();
        foreach (var part in content.Parts.NonEmpty)
        {
            nonEmptyParts.Add(part.ToString());
        }

        // Assert
        Assert.Equal(3, nonEmptyParts.Count);
        Assert.Equal("\t", nonEmptyParts[0]);
        Assert.Equal("\n", nonEmptyParts[1]);
        Assert.Equal("\r", nonEmptyParts[2]);
    }

    [Fact]
    public void NonEmptyParts_MultipleEnumerations_ProduceSameResults()
    {
        // Arrange
        var content = new Content(["", "Hello", "", "World", ""]);

        // Act
        var parts1 = new List<string>();
        foreach (var part in content.Parts.NonEmpty)
        {
            parts1.Add(part.ToString());
        }

        var parts2 = new List<string>();
        foreach (var part in content.Parts.NonEmpty)
        {
            parts2.Add(part.ToString());
        }

        // Assert
        Assert.Equal(parts1.Count, parts2.Count);
        for (var i = 0; i < parts1.Count; i++)
        {
            Assert.Equal(parts1[i], parts2[i]);
        }
    }

    [Fact]
    public void NonEmptyParts_ComplexNestedStructure_FiltersCorrectly()
    {
        // Arrange
        var branch1 = new Content([new Content(""), new Content("A"), new Content("B")]);
        var branch2 = new Content([new Content(["", "C", ""]), new Content(""), new Content("D")]);
        var content = new Content([branch1, new Content(""), branch2]);

        // Act
        var nonEmptyParts = new List<string>();
        foreach (var part in content.Parts.NonEmpty)
        {
            nonEmptyParts.Add(part.ToString());
        }

        // Assert
        Assert.Equal(4, nonEmptyParts.Count);
        Assert.Equal("A", nonEmptyParts[0]);
        Assert.Equal("B", nonEmptyParts[1]);
        Assert.Equal("C", nonEmptyParts[2]);
        Assert.Equal("D", nonEmptyParts[3]);
    }

    [Fact]
    public void NonEmptyParts_WithWhitespaceOnlyParts_IncludesWhitespace()
    {
        // Arrange
        var content = new Content(["", "   ", "", "\t\t", "", "\n\n", ""]);

        // Act
        var nonEmptyParts = new List<string>();
        foreach (var part in content.Parts.NonEmpty)
        {
            nonEmptyParts.Add(part.ToString());
        }

        // Assert
        Assert.Equal(3, nonEmptyParts.Count);
        Assert.Equal("   ", nonEmptyParts[0]);
        Assert.Equal("\t\t", nonEmptyParts[1]);
        Assert.Equal("\n\n", nonEmptyParts[2]);
    }

    [Fact]
    public void NonEmptyParts_EmptyAtBeginningMiddleAndEnd_FiltersCorrectly()
    {
        // Arrange
        var content = new Content(["", "", "Start", "", "", "Middle", "", "", "End", "", ""]);

        // Act
        var nonEmptyParts = new List<string>();
        foreach (var part in content.Parts.NonEmpty)
        {
            nonEmptyParts.Add(part.ToString());
        }

        // Assert
        Assert.Equal(3, nonEmptyParts.Count);
        Assert.Equal("Start", nonEmptyParts[0]);
        Assert.Equal("Middle", nonEmptyParts[1]);
        Assert.Equal("End", nonEmptyParts[2]);
    }

    [Fact]
    public void NonEmptyParts_VeryLongContent_FiltersCorrectly()
    {
        // Arrange
        var parts = new List<string>();
        for (var i = 0; i < 100; i++)
        {
            parts.Add(i % 3 == 0 ? "" : $"Part{i}");
        }

        var content = new Content(parts.ToImmutableArray());

        // Act
        var nonEmptyParts = new List<string>();
        foreach (var part in content.Parts.NonEmpty)
        {
            nonEmptyParts.Add(part.ToString());
        }

        // Assert
        var expectedNonEmptyCount = parts.Count(p => !string.IsNullOrEmpty(p));
        Assert.Equal(expectedNonEmptyCount, nonEmptyParts.Count);

        var nonEmptyIndex = 0;
        for (var i = 0; i < parts.Count; i++)
        {
            if (!string.IsNullOrEmpty(parts[i]))
            {
                Assert.Equal(parts[i], nonEmptyParts[nonEmptyIndex]);
                nonEmptyIndex++;
            }
        }
    }

    [Fact]
    public void NonEmptyParts_StressTest_ManyEmptyParts()
    {
        // Arrange - Create content with many empty parts interspersed with non-empty ones
        var parts = new string[1000];
        for (var i = 0; i < 1000; i++)
        {
            parts[i] = i % 10 == 0 ? $"NonEmpty{i}" : "";
        }

        var content = new Content(parts.ToImmutableArray());

        // Act
        var nonEmptyParts = new List<string>();
        foreach (var part in content.Parts.NonEmpty)
        {
            nonEmptyParts.Add(part.ToString());
        }

        // Assert
        Assert.Equal(100, nonEmptyParts.Count); // Every 10th part is non-empty
        for (var i = 0; i < 100; i++)
        {
            Assert.Equal($"NonEmpty{i * 10}", nonEmptyParts[i]);
        }
    }

    [Fact]
    public void NonEmptyParts_MatchesToString_SkipsEmptyParts()
    {
        // Arrange
        var content = new Content(["", "Hello", "", " ", "", "World", ""]);

        // Act
        var nonEmptyPartsString = string.Create(content.Length, content, static (span, content) =>
        {
            foreach (var part in content.Parts.NonEmpty)
            {
                part.Span.CopyTo(span);
                span = span[part.Length..];
            }
        });

        var toStringResult = content.ToString();

        // Assert
        Assert.Equal("Hello World", nonEmptyPartsString);
        Assert.Equal("Hello World", toStringResult);
        Assert.Equal(toStringResult, nonEmptyPartsString);
    }

    [Fact]
    public void NonEmptyParts_ConsistentWithPartsList_SkipsOnlyEmptyParts()
    {
        // Arrange
        var content = new Content(["", "A", "", "B", "", "C", ""]);

        // Act
        var allParts = new List<string>();
        foreach (var part in content.Parts)
        {
            allParts.Add(part.ToString());
        }

        var nonEmptyParts = new List<string>();
        foreach (var part in content.Parts.NonEmpty)
        {
            nonEmptyParts.Add(part.ToString());
        }

        // Assert
        Assert.Equal(7, allParts.Count); // Includes empty parts
        Assert.Equal(3, nonEmptyParts.Count); // Only non-empty parts

        var expectedNonEmpty = allParts.Where(p => p.Length > 0).ToList();
        Assert.Equal(expectedNonEmpty, nonEmptyParts);
    }

    [Fact]
    public void Equals_EmptyContent_ReturnsTrue()
    {
        // Arrange
        var content1 = Content.Empty;
        var content2 = new Content(string.Empty);
        var content3 = new Content((string?)null);

        // Act & Assert
        Assert.True(content1.Equals(content2));
        Assert.True(content2.Equals(content1));
        Assert.True(content1.Equals(content3));
        Assert.True(content3.Equals(content1));
        Assert.True(content2.Equals(content3));
    }

    [Fact]
    public void Equals_SameString_ReturnsTrue()
    {
        // Arrange
        var content1 = new Content("Hello World");
        var content2 = new Content("Hello World");

        // Act & Assert
        Assert.True(content1.Equals(content2));
        Assert.True(content2.Equals(content1));
    }

    [Fact]
    public void Equals_DifferentStrings_ReturnsFalse()
    {
        // Arrange
        var content1 = new Content("Hello");
        var content2 = new Content("World");

        // Act & Assert
        Assert.False(content1.Equals(content2));
        Assert.False(content2.Equals(content1));
    }

    [Fact]
    public void Equals_SingleValueAndMultiPart_SameContent_ReturnsTrue()
    {
        // Arrange
        var content1 = new Content("Hello World");
        var content2 = new Content(["Hello", " ", "World"]);

        // Act & Assert
        Assert.True(content1.Equals(content2));
        Assert.True(content2.Equals(content1));
    }

    [Fact]
    public void Equals_DifferentPartBoundaries_SameContent_ReturnsTrue()
    {
        // Arrange
        var content1 = new Content(["Hello", " ", "World"]);
        var content2 = new Content(["Hello ", "World"]);
        var content3 = new Content(["Hel", "lo Wor", "ld"]);

        // Act & Assert
        Assert.True(content1.Equals(content2));
        Assert.True(content2.Equals(content1));
        Assert.True(content1.Equals(content3));
        Assert.True(content3.Equals(content1));
        Assert.True(content2.Equals(content3));
    }

    [Fact]
    public void Equals_NestedContent_SameData_ReturnsTrue()
    {
        // Arrange
        var content1 = new Content([new Content(["A", "B"]), new Content(["C", "D"])]);
        var content2 = new Content(["A", "B", "C", "D"]);
        var content3 = new Content("ABCD");

        // Act & Assert
        Assert.True(content1.Equals(content2));
        Assert.True(content2.Equals(content1));
        Assert.True(content1.Equals(content3));
        Assert.True(content3.Equals(content1));
        Assert.True(content2.Equals(content3));
    }

    [Fact]
    public void Equals_DifferentLengths_ReturnsFalse()
    {
        // Arrange
        var content1 = new Content("Hello");
        var content2 = new Content("Hello World");

        // Act & Assert
        Assert.False(content1.Equals(content2));
        Assert.False(content2.Equals(content1));
    }

    [Fact]
    public void Equals_ComplexNestedStructures_SameData_ReturnsTrue()
    {
        // Arrange
        var content1 = new Content([
            new Content(["H", "e"]),
            new Content("l"),
            new Content(["l", "o"])
        ]);
        var content2 = new Content(["Hel", "lo"]);
        var content3 = new Content("Hello");

        // Act & Assert
        Assert.True(content1.Equals(content2));
        Assert.True(content2.Equals(content3));
        Assert.True(content1.Equals(content3));
    }

    [Fact]
    public void Equals_MixedContentTypes_SameData_ReturnsTrue()
    {
        // Arrange
        var content1 = new Content(["Hello".AsMemory(), " ".AsMemory(), "World".AsMemory()]);
        var content2 = new Content(["Hello", " ", "World"]);
        var content3 = new Content("Hello World");

        // Act & Assert
        Assert.True(content1.Equals(content2));
        Assert.True(content2.Equals(content3));
        Assert.True(content1.Equals(content3));
    }

    [Fact]
    public void Equals_WithObject_ReturnsCorrectResult()
    {
        // Arrange
        var content1 = new Content("Hello");
        object content2 = new Content("Hello");
        object content3 = new Content("World");
        object notContent = "Hello";

        // Act & Assert
        Assert.True(content1.Equals(content2));
        Assert.False(content1.Equals(content3));
        Assert.False(content1.Equals(notContent));
        Assert.False(content1.Equals(null!));
    }

    [Fact]
    public void OperatorEquals_SameContent_ReturnsTrue()
    {
        // Arrange
        var content1 = new Content("Hello World");
        var content2 = new Content(["Hello", " ", "World"]);

        // Act & Assert
        Assert.True(content1 == content2);
        Assert.False(content1 != content2);
    }

    [Fact]
    public void OperatorEquals_DifferentContent_ReturnsFalse()
    {
        // Arrange
        var content1 = new Content("Hello");
        var content2 = new Content("World");

        // Act & Assert
        Assert.False(content1 == content2);
        Assert.True(content1 != content2);
    }

    [Fact]
    public void GetHashCode_EmptyContent_ReturnsSameValue()
    {
        // Arrange
        var content1 = Content.Empty;
        var content2 = new Content(string.Empty);
        var content3 = new Content((string?)null);

        // Act
        var hash1 = content1.GetHashCode();
        var hash2 = content2.GetHashCode();
        var hash3 = content3.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.Equal(hash1, hash3);
        Assert.Equal(0, hash1);
    }

    [Fact]
    public void GetHashCode_SameString_ReturnsSameValue()
    {
        // Arrange
        var content1 = new Content("Hello World");
        var content2 = new Content("Hello World");

        // Act
        var hash1 = content1.GetHashCode();
        var hash2 = content2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_DifferentStrings_ReturnsDifferentValues()
    {
        // Arrange
        var content1 = new Content("Hello");
        var content2 = new Content("World");

        // Act
        var hash1 = content1.GetHashCode();
        var hash2 = content2.GetHashCode();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_SingleValueAndMultiPart_SameContent_ReturnsSameValue()
    {
        // Arrange
        var content1 = new Content("Hello World");
        var content2 = new Content(["Hello", " ", "World"]);

        // Act
        var hash1 = content1.GetHashCode();
        var hash2 = content2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_DifferentPartBoundaries_SameContent_ReturnsSameValue()
    {
        // Arrange
        var content1 = new Content(["Hello", " ", "World"]);
        var content2 = new Content(["Hello ", "World"]);
        var content3 = new Content(["Hel", "lo Wor", "ld"]);

        // Act
        var hash1 = content1.GetHashCode();
        var hash2 = content2.GetHashCode();
        var hash3 = content3.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.Equal(hash1, hash3);
    }

    [Fact]
    public void GetHashCode_NestedContent_SameData_ReturnsSameValue()
    {
        // Arrange
        var content1 = new Content([new Content(["A", "B"]), new Content(["C", "D"])]);
        var content2 = new Content(["A", "B", "C", "D"]);
        var content3 = new Content("ABCD");

        // Act
        var hash1 = content1.GetHashCode();
        var hash2 = content2.GetHashCode();
        var hash3 = content3.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.Equal(hash1, hash3);
    }

    [Fact]
    public void GetHashCode_ComplexNestedStructures_SameData_ReturnsSameValue()
    {
        // Arrange
        var content1 = new Content([
            new Content(["H", "e"]),
            new Content("l"),
            new Content(["l", "o"])
        ]);
        var content2 = new Content(["Hel", "lo"]);
        var content3 = new Content("Hello");

        // Act
        var hash1 = content1.GetHashCode();
        var hash2 = content2.GetHashCode();
        var hash3 = content3.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.Equal(hash2, hash3);
    }

    [Fact]
    public void GetHashCode_MixedContentTypes_SameData_ReturnsSameValue()
    {
        // Arrange
        var content1 = new Content(["Hello".AsMemory(), " ".AsMemory(), "World".AsMemory()]);
        var content2 = new Content(["Hello", " ", "World"]);
        var content3 = new Content("Hello World");

        // Act
        var hash1 = content1.GetHashCode();
        var hash2 = content2.GetHashCode();
        var hash3 = content3.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.Equal(hash2, hash3);
    }

    [Fact]
    public void GetHashCode_ConsistentAcrossMultipleCalls()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act
        var hash1 = content.GetHashCode();
        var hash2 = content.GetHashCode();
        var hash3 = content.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.Equal(hash2, hash3);
    }

    [Fact]
    public void GetHashCode_DeeplyNestedContent_SameData_ReturnsSameValue()
    {
        // Arrange
        var level1a = new Content(["A", "B"]);
        var level2a = new Content([level1a, new Content("C")]);
        var content1 = new Content([level2a, new Content("D")]);

        var content2 = new Content("ABCD");

        // Act
        var hash1 = content1.GetHashCode();
        var hash2 = content2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void EqualityContract_EqualObjectsHaveEqualHashCodes()
    {
        // Arrange
        var testCases = new[]
        {
            (new Content("Test"), new Content("Test")),
            (new Content(["A", "B"]), new Content("AB")),
            (new Content([new Content("X"), new Content("Y")]), new Content(["X", "Y"])),
            (Content.Empty, new Content(string.Empty)),
        };

        // Act & Assert
        foreach (var (content1, content2) in testCases)
        {
            Assert.True(content1.Equals(content2), "Contents should be equal");
            Assert.Equal(content1.GetHashCode(), content2.GetHashCode());
        }
    }

    [Fact]
    public void GetHashCode_CanBeUsedInDictionary()
    {
        // Arrange
        var dict = new Dictionary<Content, string>();
        var key1 = new Content("Hello World");
        var key2 = new Content(["Hello", " ", "World"]);
        var key3 = new Content(["Hello ", "World"]);

        // Act
        dict[key1] = "value1";

        // Assert - all three keys should map to the same value
        Assert.Equal("value1", dict[key1]);
        Assert.Equal("value1", dict[key2]);
        Assert.Equal("value1", dict[key3]);
        Assert.Single(dict);
    }

    [Fact]
    public void GetHashCode_CanBeUsedInHashSet()
    {
        // Arrange
        var set = new HashSet<Content>();
        var item1 = new Content("Test");
        var item2 = new Content(["Te", "st"]);
        var item3 = new Content(["T", "e", "s", "t"]);

        // Act
        set.Add(item1);
        set.Add(item2);
        set.Add(item3);

        // Assert - all three items represent the same content
        Assert.Single(set);
        Assert.Contains(item1, set);
        Assert.Contains(item2, set);
        Assert.Contains(item3, set);
    }

    [Fact]
    public void CharEnumerator_WithEmptyContent_DoesNotEnumerate()
    {
        // Arrange
        var content = Content.Empty;
        var count = 0;

        // Act
        foreach (var _ in content)
        {
            count++;
        }

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void CharEnumerator_WithSingleValue_EnumeratesAllCharacters()
    {
        // Arrange
        var content = new Content("Hello");
        var chars = new List<char>();

        // Act
        foreach (var ch in content)
        {
            chars.Add(ch);
        }

        // Assert
        Assert.Equal(5, chars.Count);
        Assert.Equal('H', chars[0]);
        Assert.Equal('e', chars[1]);
        Assert.Equal('l', chars[2]);
        Assert.Equal('l', chars[3]);
        Assert.Equal('o', chars[4]);
    }

    [Fact]
    public void CharEnumerator_WithMultipleParts_EnumeratesAllCharacters()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);
        var chars = new List<char>();

        // Act
        foreach (var ch in content)
        {
            chars.Add(ch);
        }

        // Assert
        Assert.Equal(11, chars.Count);
        Assert.Equal("Hello World", new string([.. chars]));
    }

    [Fact]
    public void CharEnumerator_WithNestedContent_EnumeratesAllCharacters()
    {
        // Arrange
        var inner1 = new Content(["A", "B"]);
        var inner2 = new Content(["C", "D"]);
        var content = new Content([inner1, inner2]);
        var chars = new List<char>();

        // Act
        foreach (var ch in content)
        {
            chars.Add(ch);
        }

        // Assert
        Assert.Equal(4, chars.Count);
        Assert.Equal("ABCD", new string([.. chars]));
    }

    [Fact]
    public void CharEnumerator_WithDeeplyNestedContent_EnumeratesCorrectly()
    {
        // Arrange
        var level1 = new Content(["A", "B"]);
        var level2 = new Content([level1, new Content("C")]);
        var level3 = new Content([level2, new Content("D")]);
        var chars = new List<char>();

        // Act
        foreach (var ch in level3)
        {
            chars.Add(ch);
        }

        // Assert
        Assert.Equal(4, chars.Count);
        Assert.Equal("ABCD", new string([.. chars]));
    }

    [Fact]
    public void CharEnumerator_ManualIteration_WorksCorrectly()
    {
        // Arrange
        var content = new Content("ABC");
        using var enumerator = content.GetEnumerator();

        // Act & Assert
        Assert.True(enumerator.MoveNext());
        Assert.Equal('A', enumerator.Current);

        Assert.True(enumerator.MoveNext());
        Assert.Equal('B', enumerator.Current);

        Assert.True(enumerator.MoveNext());
        Assert.Equal('C', enumerator.Current);

        Assert.False(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext()); // Verify stays false
    }

    [Fact]
    public void CharEnumerator_WithEmptyParts_SkipsEmptyParts()
    {
        // Arrange
        var content = new Content(["", "A", "", "B", "", "C", ""]);
        var chars = new List<char>();

        // Act
        foreach (var ch in content)
        {
            chars.Add(ch);
        }

        // Assert
        Assert.Equal(3, chars.Count);
        Assert.Equal("ABC", new string([.. chars]));
    }

    [Fact]
    public void CharEnumerator_WithMixedContentTypes_EnumeratesCorrectly()
    {
        // Arrange
        var memoryPart = new Content("Hello".AsMemory());
        var stringPart = new Content(" World");
        var content = new Content([memoryPart, stringPart]);
        var chars = new List<char>();

        // Act
        foreach (var ch in content)
        {
            chars.Add(ch);
        }

        // Assert
        Assert.Equal(11, chars.Count);
        Assert.Equal("Hello World", new string([.. chars]));
    }

    [Fact]
    public void CharEnumerator_WithSingleCharacterParts_EnumeratesCorrectly()
    {
        // Arrange
        var content = new Content(["H", "e", "l", "l", "o"]);
        var chars = new List<char>();

        // Act
        foreach (var ch in content)
        {
            chars.Add(ch);
        }

        // Assert
        Assert.Equal(5, chars.Count);
        Assert.Equal("Hello", new string([.. chars]));
    }

    [Fact]
    public void CharEnumerator_WithComplexNestedStructure_EnumeratesCorrectly()
    {
        // Arrange
        var branch1 = new Content([new Content("A"), new Content("B")]);
        var branch2 = new Content([new Content(["C", "D"]), new Content("E")]);
        var content = new Content([branch1, new Content("F"), branch2]);
        var chars = new List<char>();

        // Act
        foreach (var ch in content)
        {
            chars.Add(ch);
        }

        // Assert
        Assert.Equal(6, chars.Count);
        Assert.Equal("ABFCDE", new string([.. chars]));
    }

    [Fact]
    public void CharEnumerator_MultipleEnumerations_ProduceSameResults()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act
        var chars1 = new List<char>();
        foreach (var ch in content)
        {
            chars1.Add(ch);
        }

        var chars2 = new List<char>();
        foreach (var ch in content)
        {
            chars2.Add(ch);
        }

        // Assert
        Assert.Equal(chars1.Count, chars2.Count);
        for (var i = 0; i < chars1.Count; i++)
        {
            Assert.Equal(chars1[i], chars2[i]);
        }
    }

    [Fact]
    public void CharEnumerator_WithUnicodeCharacters_EnumeratesCorrectly()
    {
        // Arrange
        var content = new Content(["Hello", " ", "世界"]);
        var chars = new List<char>();

        // Act
        foreach (var ch in content)
        {
            chars.Add(ch);
        }

        // Assert
        Assert.Equal(8, chars.Count);
        Assert.Equal("Hello 世界", new string([.. chars]));
    }

    [Fact]
    public void CharEnumerator_WithSpecialCharacters_EnumeratesCorrectly()
    {
        // Arrange
        var content = new Content(["Tab:\t", "NewLine:\n", "Return:\r"]);
        var chars = new List<char>();

        // Act
        foreach (var ch in content)
        {
            chars.Add(ch);
        }

        // Assert
        Assert.Contains('\t', chars);
        Assert.Contains('\n', chars);
        Assert.Contains('\r', chars);
    }

    [Fact]
    public void CharEnumerator_Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var content = new Content("Test");
        var enumerator = content.GetEnumerator();

        // Act - Dispose multiple times should not throw
        enumerator.Dispose();
        enumerator.Dispose();
        enumerator.Dispose();

        // Assert - No exception thrown
    }

    [Fact]
    public void CharEnumerator_AfterDispose_MoveNextReturnsFalse()
    {
        // Arrange
        var content = new Content("Test");
        var enumerator = content.GetEnumerator();

        // Act
        enumerator.MoveNext(); // Move to first character
        enumerator.Dispose();
        var result = enumerator.MoveNext();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CharEnumerator_WithLongContent_EnumeratesAllCharacters()
    {
        // Arrange
        var parts = new List<string>();
        for (var i = 0; i < 100; i++)
        {
            parts.Add($"Part{i}");
        }

        var content = new Content(parts.ToImmutableArray());
        var charCount = 0;

        // Act
        foreach (var _ in content)
        {
            charCount++;
        }

        // Assert
        var expectedLength = 0;
        foreach (var p in parts)
        {
            expectedLength += p.Length;
        }

        Assert.Equal(expectedLength, charCount);
        Assert.Equal(content.Length, charCount);
    }

    [Fact]
    public void CharEnumerator_MatchesStringEnumeration()
    {
        // Arrange
        var text = "Hello World! This is a test.";
        var content = new Content(text);
        var contentChars = new List<char>();
        var stringChars = new List<char>();

        // Act
        foreach (var ch in content)
        {
            contentChars.Add(ch);
        }

        foreach (var ch in text)
        {
            stringChars.Add(ch);
        }

        // Assert
        Assert.Equal(stringChars.Count, contentChars.Count);

        for (var i = 0; i < stringChars.Count; i++)
        {
            Assert.Equal(stringChars[i], contentChars[i]);
        }
    }

    [Fact]
    public void CharEnumerator_WithNestedEmptyParts_SkipsCorrectly()
    {
        // Arrange
        var empty1 = new Content(string.Empty);
        var nonEmpty = new Content("Test");
        var empty2 = new Content(ImmutableArray<string>.Empty);
        var content = new Content([empty1, nonEmpty, empty2]);
        var chars = new List<char>();

        // Act
        foreach (var ch in content)
        {
            chars.Add(ch);
        }

        // Assert
        Assert.Equal(4, chars.Count);
        Assert.Equal("Test", new string([.. chars]));
    }

    [Fact]
    public void Contains_EmptyContent_ReturnsFalse()
    {
        // Arrange
        var content = Content.Empty;

        // Act & Assert
        Assert.False(content.Contains('a'));
        Assert.Equal(-1, content.IndexOf('a'));
    }

    [Fact]
    public void Contains_SingleValue_CharacterExists_ReturnsTrue()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.True(content.Contains('H'));
        Assert.Equal(0, content.IndexOf('H'));

        Assert.True(content.Contains('o'));
        Assert.Equal(4, content.IndexOf('o'));

        Assert.True(content.Contains(' '));
        Assert.Equal(5, content.IndexOf(' '));

        Assert.True(content.Contains('d'));
        Assert.Equal(10, content.IndexOf('d'));
    }

    [Fact]
    public void Contains_SingleValue_CharacterNotExists_ReturnsFalse()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.False(content.Contains('x'));
        Assert.Equal(-1, content.IndexOf('x'));

        Assert.False(content.Contains('Z'));
        Assert.Equal(-1, content.IndexOf('Z'));
    }

    [Fact]
    public void Contains_MultiPart_CharacterExists_ReturnsTrue()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act & Assert
        Assert.True(content.Contains('H'));
        Assert.Equal(0, content.IndexOf('H'));

        Assert.True(content.Contains(' '));
        Assert.Equal(5, content.IndexOf(' '));

        Assert.True(content.Contains('W'));
        Assert.Equal(6, content.IndexOf('W'));
    }

    [Fact]
    public void Contains_MultiPart_CharacterNotExists_ReturnsFalse()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act & Assert
        Assert.False(content.Contains('x'));
        Assert.Equal(-1, content.IndexOf('x'));
    }

    [Fact]
    public void Contains_Substring_EmptyValue_ReturnsTrue()
    {
        // Arrange
        var content = new Content("Hello");

        // Act & Assert
        Assert.True(content.Contains("".AsSpan(), StringComparison.Ordinal));
        Assert.Equal(0, content.IndexOf("".AsSpan(), StringComparison.Ordinal));
    }

    [Fact]
    public void Contains_Substring_SingleValue_Exists_ReturnsTrue()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.True(content.Contains("Hello".AsSpan(), StringComparison.Ordinal));
        Assert.Equal(0, content.IndexOf("Hello".AsSpan(), StringComparison.Ordinal));

        Assert.True(content.Contains("World".AsSpan(), StringComparison.Ordinal));
        Assert.Equal(6, content.IndexOf("World".AsSpan(), StringComparison.Ordinal));

        Assert.True(content.Contains("lo Wo".AsSpan(), StringComparison.Ordinal));
        Assert.Equal(3, content.IndexOf("lo Wo".AsSpan(), StringComparison.Ordinal));
    }

    [Fact]
    public void Contains_Substring_SingleValue_NotExists_ReturnsFalse()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.False(content.Contains("xyz".AsSpan(), StringComparison.Ordinal));
        Assert.Equal(-1, content.IndexOf("xyz".AsSpan(), StringComparison.Ordinal));
    }

    [Fact]
    public void Contains_Substring_MultiPart_SpansMultipleParts_ReturnsTrue()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act & Assert
        Assert.True(content.Contains("lo Wo".AsSpan(), StringComparison.Ordinal));
        Assert.Equal(3, content.IndexOf("lo Wo".AsSpan(), StringComparison.Ordinal));

        Assert.True(content.Contains("Hello World".AsSpan(), StringComparison.Ordinal));
        Assert.Equal(0, content.IndexOf("Hello World".AsSpan(), StringComparison.Ordinal));
    }

    [Fact]
    public void Contains_Substring_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.True(content.Contains("hello".AsSpan(), StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, content.IndexOf("hello".AsSpan(), StringComparison.OrdinalIgnoreCase));

        Assert.True(content.Contains("WORLD".AsSpan(), StringComparison.OrdinalIgnoreCase));
        Assert.Equal(6, content.IndexOf("WORLD".AsSpan(), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Contains_Substring_LongerThanContent_ReturnsFalse()
    {
        // Arrange
        var content = new Content("Hi");

        // Act & Assert
        Assert.False(content.Contains("Hello".AsSpan(), StringComparison.Ordinal));
        Assert.Equal(-1, content.IndexOf("Hello".AsSpan(), StringComparison.Ordinal));
    }

    [Fact]
    public void ContainsAny_TwoChars_EmptyContent_ReturnsFalse()
    {
        // Arrange
        var content = Content.Empty;

        // Act & Assert
        Assert.False(content.ContainsAny('a', 'b'));
        Assert.Equal(-1, content.IndexOfAny('a', 'b'));
    }

    [Fact]
    public void ContainsAny_TwoChars_SingleValue_Found_ReturnsTrue()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.True(content.ContainsAny('H', 'x'));
        Assert.Equal(0, content.IndexOfAny('H', 'x'));

        Assert.True(content.ContainsAny('x', 'd'));
        Assert.Equal(10, content.IndexOfAny('x', 'd'));

        Assert.True(content.ContainsAny('o', 'x'));
        Assert.Equal(4, content.IndexOfAny('o', 'x'));
    }

    [Fact]
    public void ContainsAny_TwoChars_SingleValue_NotFound_ReturnsFalse()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.False(content.ContainsAny('x', 'z'));
        Assert.Equal(-1, content.IndexOfAny('x', 'z'));
    }

    [Fact]
    public void ContainsAny_ThreeChars_SingleValue_Found_ReturnsTrue()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.True(content.ContainsAny('H', 'x', 'y'));
        Assert.Equal(0, content.IndexOfAny('H', 'x', 'y'));

        Assert.True(content.ContainsAny('x', 'y', 'd'));
        Assert.Equal(10, content.IndexOfAny('x', 'y', 'd'));
    }

    [Fact]
    public void ContainsAny_ThreeChars_SingleValue_NotFound_ReturnsFalse()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.False(content.ContainsAny('x', 'y', 'z'));
        Assert.Equal(-1, content.IndexOfAny('x', 'y', 'z'));
    }

    [Fact]
    public void ContainsAny_Span_SingleValue_Found_ReturnsTrue()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.True(content.ContainsAny("Hxyz".AsSpan()));
        Assert.Equal(0, content.IndexOfAny("Hxyz".AsSpan()));

        Assert.True(content.ContainsAny("xyz ".AsSpan()));
        Assert.Equal(5, content.IndexOfAny("xyz ".AsSpan()));
    }

    [Fact]
    public void ContainsAny_Span_SingleValue_NotFound_ReturnsFalse()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.False(content.ContainsAny("xyz".AsSpan()));
        Assert.Equal(-1, content.IndexOfAny("xyz".AsSpan()));
    }

    [Fact]
    public void ContainsAny_Span_EmptySpan_ReturnsFalse()
    {
        // Arrange
        var content = new Content("Hello");

        // Act & Assert
        Assert.False(content.ContainsAny([]));
        Assert.Equal(-1, content.IndexOfAny([]));
    }

    [Fact]
    public void ContainsAny_MultiPart_Found_ReturnsTrue()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act & Assert
        Assert.True(content.ContainsAny('H', 'x'));
        Assert.Equal(0, content.IndexOfAny('H', 'x'));

        Assert.True(content.ContainsAny('x', 'W', 'y'));
        Assert.Equal(6, content.IndexOfAny('x', 'W', 'y'));

        Assert.True(content.ContainsAny("Hxyz".AsSpan()));
        Assert.Equal(0, content.IndexOfAny("Hxyz".AsSpan()));
    }

    [Fact]
    public void ContainsAny_NestedContent_Found_ReturnsTrue()
    {
        // Arrange
        var inner1 = new Content(["A", "B"]);
        var inner2 = new Content(["C", "D"]);
        var content = new Content([inner1, inner2]);

        // Act & Assert
        Assert.True(content.ContainsAny('A', 'x'));
        Assert.Equal(0, content.IndexOfAny('A', 'x'));

        Assert.True(content.ContainsAny('x', 'y', 'D'));
        Assert.Equal(3, content.IndexOfAny('x', 'y', 'D'));
    }

    [Fact]
    public void ContainsAnyExcept_SingleChar_EmptyContent_ReturnsFalse()
    {
        // Arrange
        var content = Content.Empty;

        // Act & Assert
        Assert.False(content.ContainsAnyExcept('a'));
        Assert.Equal(-1, content.IndexOfAnyExcept('a'));
    }

    [Fact]
    public void ContainsAnyExcept_SingleChar_OnlyContainsExcluded_ReturnsFalse()
    {
        // Arrange
        var content = new Content("aaaa");

        // Act & Assert
        Assert.False(content.ContainsAnyExcept('a'));
        Assert.Equal(-1, content.IndexOfAnyExcept('a'));
    }

    [Fact]
    public void ContainsAnyExcept_SingleChar_ContainsOther_ReturnsTrue()
    {
        // Arrange
        var content = new Content("aaabaa");

        // Act & Assert
        Assert.True(content.ContainsAnyExcept('a'));
        Assert.Equal(3, content.IndexOfAnyExcept('a'));
    }

    [Fact]
    public void ContainsAnyExcept_TwoChars_OnlyContainsExcluded_ReturnsFalse()
    {
        // Arrange
        var content = new Content("ababab");

        // Act & Assert
        Assert.False(content.ContainsAnyExcept('a', 'b'));
        Assert.Equal(-1, content.IndexOfAnyExcept('a', 'b'));
    }

    [Fact]
    public void ContainsAnyExcept_TwoChars_ContainsOther_ReturnsTrue()
    {
        // Arrange
        var content = new Content("ababcab");

        // Act & Assert
        Assert.True(content.ContainsAnyExcept('a', 'b'));
        Assert.Equal(4, content.IndexOfAnyExcept('a', 'b'));
    }

    [Fact]
    public void ContainsAnyExcept_ThreeChars_OnlyContainsExcluded_ReturnsFalse()
    {
        // Arrange
        var content = new Content("abcabc");

        // Act & Assert
        Assert.False(content.ContainsAnyExcept('a', 'b', 'c'));
        Assert.Equal(-1, content.IndexOfAnyExcept('a', 'b', 'c'));
    }

    [Fact]
    public void ContainsAnyExcept_ThreeChars_ContainsOther_ReturnsTrue()
    {
        // Arrange
        var content = new Content("abcdabc");

        // Act & Assert
        Assert.True(content.ContainsAnyExcept('a', 'b', 'c'));
        Assert.Equal(3, content.IndexOfAnyExcept('a', 'b', 'c'));
    }

    [Fact]
    public void ContainsAnyExcept_Span_OnlyContainsExcluded_ReturnsFalse()
    {
        // Arrange
        var content = new Content("abcabc");

        // Act & Assert
        Assert.False(content.ContainsAnyExcept("abc".AsSpan()));
        Assert.Equal(-1, content.IndexOfAnyExcept("abc".AsSpan()));
    }

    [Fact]
    public void ContainsAnyExcept_Span_ContainsOther_ReturnsTrue()
    {
        // Arrange
        var content = new Content("abcdabc");

        // Act & Assert
        Assert.True(content.ContainsAnyExcept("abc".AsSpan()));
        Assert.Equal(3, content.IndexOfAnyExcept("abc".AsSpan()));
    }

    [Fact]
    public void ContainsAnyExcept_Span_EmptySpan_ReturnsTrue()
    {
        // Arrange
        var content = new Content("Hello");

        // Act & Assert
        Assert.True(content.ContainsAnyExcept([]));
        Assert.Equal(0, content.IndexOfAnyExcept([]));
    }

    [Fact]
    public void ContainsAnyExcept_MultiPart_OnlyContainsExcluded_ReturnsFalse()
    {
        // Arrange
        var content = new Content(["aaa", "bbb", "ccc"]);

        // Act & Assert
        Assert.False(content.ContainsAnyExcept("abc".AsSpan()));
        Assert.Equal(-1, content.IndexOfAnyExcept("abc".AsSpan()));
    }

    [Fact]
    public void ContainsAnyExcept_MultiPart_ContainsOther_ReturnsTrue()
    {
        // Arrange
        var content = new Content(["aaa", "bbb", "ddd"]);

        // Act & Assert
        Assert.True(content.ContainsAnyExcept("abc".AsSpan()));
        Assert.Equal(6, content.IndexOfAnyExcept("abc".AsSpan()));
    }

    [Fact]
    public void ContainsAnyExcept_Whitespace_ReturnsCorrectly()
    {
        // Arrange
        var content1 = new Content("   \t\n");
        var content2 = new Content("  a  ");

        // Act & Assert
        Assert.False(content1.ContainsAnyExcept(' ', '\t', '\n'));
        Assert.Equal(-1, content1.IndexOfAnyExcept(' ', '\t', '\n'));

        Assert.True(content2.ContainsAnyExcept(' ', '\t', '\n'));
        Assert.Equal(2, content2.IndexOfAnyExcept(' ', '\t', '\n'));
    }

    [Fact]
    public void ContainsAnyExcept_NestedContent_ReturnsCorrectly()
    {
        // Arrange
        var inner1 = new Content(["aaa", "bbb"]);
        var inner2 = new Content(["ccc", "ddd"]);
        var content = new Content([inner1, inner2]);

        // Act & Assert
        Assert.False(content.ContainsAnyExcept("abcd".AsSpan()));
        Assert.Equal(-1, content.IndexOfAnyExcept("abcd".AsSpan()));
        
        var content2 = new Content([inner1, new Content("xyz")]);
        Assert.True(content2.ContainsAnyExcept("ab".AsSpan()));
        Assert.Equal(6, content2.IndexOfAnyExcept("ab".AsSpan()));
    }

    [Fact]
    public void ToString_EmptyContent_ReturnsEmptyString()
    {
        // Arrange
        var content = Content.Empty;

        // Act
        var result = content.ToString();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToString_EmptyStringContent_ReturnsEmptyString()
    {
        // Arrange
        var content = new Content(string.Empty);

        // Act
        var result = content.ToString();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToString_SingleValue_ReturnsString()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content.ToString();

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void ToString_SingleValue_WithMemory_ReturnsString()
    {
        // Arrange
        var content = new Content("Test Data".AsMemory());

        // Act
        var result = content.ToString();

        // Assert
        Assert.Equal("Test Data", result);
    }

    [Fact]
    public void ToString_MultiPart_ReturnsConcat()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act
        var result = content.ToString();

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void ToString_MultiPart_WithMemory_ReturnsConcatenated()
    {
        // Arrange
        var content = new Content(["Hello".AsMemory(), " ".AsMemory(), "World".AsMemory()]);

        // Act
        var result = content.ToString();

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void ToString_NestedContent_ReturnsFlattened()
    {
        // Arrange
        var inner1 = new Content(["Hello", " "]);
        var inner2 = new Content(["World", "!"]);
        var content = new Content([inner1, inner2]);

        // Act
        var result = content.ToString();

        // Assert
        Assert.Equal("Hello World!", result);
    }

    [Fact]
    public void ToString_DeeplyNestedContent_ReturnsFlattened()
    {
        // Arrange
        var level1 = new Content(["A", "B"]);
        var level2 = new Content([level1, new Content("C")]);
        var level3 = new Content([level2, new Content("D")]);

        // Act
        var result = level3.ToString();

        // Assert
        Assert.Equal("ABCD", result);
    }

    [Fact]
    public void ToString_WithEmptyParts_SkipsEmptyParts()
    {
        // Arrange
        var content = new Content(["", "Hello", "", "World", ""]);

        // Act
        var result = content.ToString();

        // Assert
        Assert.Equal("HelloWorld", result);
    }

    [Fact]
    public void ToString_ComplexNestedStructure_ReturnsCorrectString()
    {
        // Arrange
        var branch1 = new Content([new Content("Hello"), new Content(" ")]);
        var branch2 = new Content([new Content(["Wor", "ld"]), new Content("!")]);
        var content = new Content([branch1, branch2]);

        // Act
        var result = content.ToString();

        // Assert
        Assert.Equal("Hello World!", result);
    }

    [Fact]
    public void ToString_WithUnicodeCharacters_PreservesUnicode()
    {
        // Arrange
        var content = new Content(["Hello ", "世界", "!"]);

        // Act
        var result = content.ToString();

        // Assert
        Assert.Equal("Hello 世界!", result);
    }

    [Fact]
    public void ToString_WithSpecialCharacters_PreservesSpecialChars()
    {
        // Arrange
        var content = new Content(["Line1\n", "Line2\r\n", "Tab:\t"]);

        // Act
        var result = content.ToString();

        // Assert
        Assert.Equal("Line1\nLine2\r\nTab:\t", result);
    }

    [Fact]
    public void ToString_SingleCharacterParts_Concatenates()
    {
        // Arrange
        var content = new Content(["H", "e", "l", "l", "o"]);

        // Act
        var result = content.ToString();

        // Assert
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void ToString_MixedContentTypes_ReturnsConcatenated()
    {
        // Arrange
        var memoryPart = new Content("Memory".AsMemory());
        var stringPart = new Content(" String");
        var nestedPart = new Content([" ", "Nested"]);
        var content = new Content([memoryPart, stringPart, nestedPart]);

        // Act
        var result = content.ToString();

        // Assert
        Assert.Equal("Memory String Nested", result);
    }

    [Fact]
    public void ToString_LongContent_HandlesLargeStrings()
    {
        // Arrange
        var parts = new List<string>();
        for (var i = 0; i < 100; i++)
        {
            parts.Add($"Part{i}");
        }
        var content = new Content(parts.ToImmutableArray());

        // Act
        var result = content.ToString();

        // Assert
        var expected = string.Concat(parts);
        Assert.Equal(expected, result);
        Assert.Equal(content.Length, result.Length);
    }

    [Fact]
    public void ToString_CalledMultipleTimes_ReturnsSameResult()
    {
        // Arrange
        var content = new Content(["Test", " ", "Data"]);

        // Act
        var result1 = content.ToString();
        var result2 = content.ToString();
        var result3 = content.ToString();

        // Assert
        Assert.Equal("Test Data", result1);
        Assert.Equal(result1, result2);
        Assert.Equal(result2, result3);
    }

    [Fact]
    public void ToString_MatchesCharEnumerator()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World", "!"]);

        // Act
        var toStringResult = content.ToString();
        
        var chars = new List<char>();
        foreach (var ch in content)
        {
            chars.Add(ch);
        }
        var enumeratorResult = new string([.. chars]);

        // Assert
        Assert.Equal(toStringResult, enumeratorResult);
    }

    [Fact]
    public void ToString_DifferentPartBoundaries_SameContent_ReturnsSameString()
    {
        // Arrange
        var content1 = new Content(["Hello", " ", "World"]);
        var content2 = new Content(["Hello ", "World"]);
        var content3 = new Content("Hello World");

        // Act
        var result1 = content1.ToString();
        var result2 = content2.ToString();
        var result3 = content3.ToString();

        // Assert
        Assert.Equal("Hello World", result1);
        Assert.Equal(result1, result2);
        Assert.Equal(result2, result3);
    }

    [Fact]
    public void ToString_WithWhitespace_PreservesWhitespace()
    {
        // Arrange
        var content = new Content(["  ", "Hello", "  ", "World", "  "]);

        // Act
        var result = content.ToString();

        // Assert
        Assert.Equal("  Hello  World  ", result);
    }

    [Fact]
    public void ToString_NestedEmptyContent_SkipsEmpty()
    {
        // Arrange
        var empty1 = new Content(string.Empty);
        var nonEmpty = new Content("Test");
        var empty2 = new Content(ImmutableArray<string>.Empty);
        var content = new Content([empty1, nonEmpty, empty2]);

        // Act
        var result = content.ToString();

        // Assert
        Assert.Equal("Test", result);
    }

    [Fact]
    public void Slice_WithStart_EmptyContent_ReturnsEmpty()
    {
        // Arrange
        var content = Content.Empty;

        // Act
        var result = content[..];

        // Assert
        Assert.True(result.IsEmpty);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void Slice_WithStart_SingleValue_FromBeginning_ReturnsSlice()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content[6..];

        // Assert
        Assert.Equal("World", result.ToString());
        Assert.Equal(5, result.Length);
        Assert.True(result.IsSingleValue);
    }

    [Fact]
    public void Slice_WithStart_SingleValue_FromMiddle_ReturnsSlice()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content[3..];

        // Assert
        Assert.Equal("lo World", result.ToString());
        Assert.Equal(8, result.Length);
    }

    [Fact]
    public void Slice_WithStart_SingleValue_AtEnd_ReturnsEmpty()
    {
        // Arrange
        var content = new Content("Hello");

        // Act
        var result = content[5..];

        // Assert
        Assert.True(result.IsEmpty);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void Slice_WithStart_SingleValue_InvalidStart_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var content = new Content("Hello");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => content[-1..]);
        Assert.Throws<ArgumentOutOfRangeException>(() => content[6..]);
    }

    [Fact]
    public void Slice_WithStartAndLength_SingleValue_ReturnsSlice()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content.Slice(6, 5);

        // Assert
        Assert.Equal("World", result.ToString());
        Assert.Equal(5, result.Length);
        Assert.True(result.IsSingleValue);
    }

    [Fact]
    public void Slice_WithStartAndLength_SingleValue_MiddleSlice_ReturnsSlice()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content.Slice(3, 5);

        // Assert
        Assert.Equal("lo Wo", result.ToString());
        Assert.Equal(5, result.Length);
    }

    [Fact]
    public void Slice_WithStartAndLength_SingleValue_ZeroLength_ReturnsEmpty()
    {
        // Arrange
        var content = new Content("Hello");

        // Act
        var result = content.Slice(2, 0);

        // Assert
        Assert.True(result.IsEmpty);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void Slice_WithStartAndLength_SingleValue_EntireContent_ReturnsSameContent()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content[..11];

        // Assert
        Assert.Equal(content, result);
        Assert.Equal("Hello World", result.ToString());
    }

    [Fact]
    public void Slice_WithStartAndLength_SingleValue_InvalidArguments_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var content = new Content("Hello");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => content.Slice(-1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => content[..-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => content[..6]);
        Assert.Throws<ArgumentOutOfRangeException>(() => content.Slice(3, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => content.Slice(6, 0));
    }

    [Fact]
    public void Slice_MultiPart_FromBeginning_ReturnsSlice()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act
        var result = content[6..];

        // Assert
        Assert.Equal("World", result.ToString());
        Assert.Equal(5, result.Length);
    }

    [Fact]
    public void Slice_MultiPart_FromMiddle_ReturnsSlice()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act
        var result = content[3..];

        // Assert
        Assert.Equal("lo World", result.ToString());
        Assert.Equal(8, result.Length);
    }

    [Fact]
    public void Slice_MultiPart_WithinSinglePart_ReturnsSlice()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act
        var result = content.Slice(1, 3);

        // Assert
        Assert.Equal("ell", result.ToString());
        Assert.Equal(3, result.Length);
        Assert.True(result.IsSingleValue);
    }

    [Fact]
    public void Slice_MultiPart_SpanningMultipleParts_ReturnsSlice()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act
        var result = content.Slice(3, 5);

        // Assert
        Assert.Equal("lo Wo", result.ToString());
        Assert.Equal(5, result.Length);
    }

    [Fact]
    public void Slice_MultiPart_EntireContent_ReturnsSameContent()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act
        var result = content[..11]  ;

        // Assert
        Assert.Equal(content, result);
        Assert.Equal("Hello World", result.ToString());
    }

    [Fact]
    public void Slice_MultiPart_ExactlyOnePart_ReturnsSlice()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act
        var result = content.Slice(6, 5);

        // Assert
        Assert.Equal("World", result.ToString());
        Assert.Equal(5, result.Length);
        Assert.True(result.IsSingleValue);
    }

    [Fact]
    public void Slice_MultiPart_SkipFirstPart_ReturnsSlice()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act
        var result = content.Slice(5, 6);

        // Assert
        Assert.Equal(" World", result.ToString());
        Assert.Equal(6, result.Length);
    }

    [Fact]
    public void Slice_MultiPart_SkipLastPart_ReturnsSlice()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act
        var result = content[..6];

        // Assert
        Assert.Equal("Hello ", result.ToString());
        Assert.Equal(6, result.Length);
    }

    [Fact]
    public void Slice_NestedContent_ReturnsSlice()
    {
        // Arrange
        var inner1 = new Content(["Hello", " "]);
        var inner2 = new Content(["World", "!"]);
        var content = new Content([inner1, inner2]);

        // Act
        var result = content.Slice(6, 5);

        // Assert
        Assert.Equal("World", result.ToString());
        Assert.Equal(5, result.Length);
    }

    [Fact]
    public void Slice_NestedContent_SpanningMultipleParts_ReturnsSlice()
    {
        // Arrange
        var inner1 = new Content(["A", "B", "C"]);
        var inner2 = new Content(["D", "E", "F"]);
        var content = new Content([inner1, inner2]);

        // Act
        var result = content.Slice(1, 4);

        // Assert
        Assert.Equal("BCDE", result.ToString());
        Assert.Equal(4, result.Length);
    }

    [Fact]
    public void Slice_WithMemoryParts_ReturnsSlice()
    {
        // Arrange
        var content = new Content(["Hello".AsMemory(), " ".AsMemory(), "World".AsMemory()]);

        // Act
        var result = content.Slice(3, 5);

        // Assert
        Assert.Equal("lo Wo", result.ToString());
        Assert.Equal(5, result.Length);
    }

    [Fact]
    public void Slice_ComplexNestedStructure_ReturnsSlice()
    {
        // Arrange
        var level1 = new Content(["A", "B"]);
        var level2 = new Content([level1, new Content("C")]);
        var level3 = new Content([level2, new Content("D")]);

        // Act
        var result = level3.Slice(1, 2);

        // Assert
        Assert.Equal("BC", result.ToString());
        Assert.Equal(2, result.Length);
    }

    [Fact]
    public void Slice_WithEmptyParts_SkipsEmptyParts()
    {
        // Arrange
        var content = new Content(["", "Hello", "", " ", "World", ""]);

        // Act
        var result = content.Slice(3, 5);

        // Assert
        Assert.Equal("lo Wo", result.ToString());
        Assert.Equal(5, result.Length);
    }

    [Fact]
    public void Slice_MultipleTimes_ReturnsCorrectSlices()
    {
        // Arrange
        var content = new Content("Hello World!");

        // Act
        var slice1 = content[..5];
        var slice2 = content.Slice(6, 5);
        var slice3 = content.Slice(11, 1);

        // Assert
        Assert.Equal("Hello", slice1.ToString());
        Assert.Equal("World", slice2.ToString());
        Assert.Equal("!", slice3.ToString());
    }

    [Fact]
    public void Slice_ChainedSlicing_ReturnsCorrectSlice()
    {
        // Arrange
        var content = new Content("Hello World!");

        // Act
        var slice1 = content[..11];
        var slice2 = slice1.Slice(6, 5);

        // Assert
        Assert.Equal("World", slice2.ToString());
        Assert.Equal(5, slice2.Length);
    }

    [Fact]
    public void Slice_MultiPart_PartialFirstAndLastPart_ReturnsSlice()
    {
        // Arrange
        var content = new Content(["ABCDE", "FGHIJ", "KLMNO"]);

        // Act
        var result = content.Slice(2, 9);

        // Assert
        Assert.Equal("CDEFGHIJK", result.ToString());
        Assert.Equal(9, result.Length);
    }

    [Fact]
    public void Slice_UnicodeCharacters_PreservesUnicode()
    {
        // Arrange
        var content = new Content(["Hello ", "世界", "!"]);

        // Act
        var result = content.Slice(6, 2);

        // Assert
        Assert.Equal("世界", result.ToString());
        Assert.Equal(2, result.Length);
    }

    [Fact]
    public void Slice_WithSpecialCharacters_PreservesSpecialChars()
    {
        // Arrange
        var content = new Content(["Line1\n", "Line2\r\n", "Tab:\t"]);

        // Act
        var result = content.Slice(5, 7);

        // Assert
        Assert.Equal("\nLine2\r", result.ToString());
        Assert.Equal(7, result.Length);
    }

    [Fact]
    public void Slice_SingleCharacter_ReturnsSlice()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content.Slice(4, 1);

        // Assert
        Assert.Equal("o", result.ToString());
        Assert.Equal(1, result.Length);
        Assert.True(result.IsSingleValue);
    }

    [Fact]
    public void Slice_ResultEquality_WorksCorrectly()
    {
        // Arrange
        var content1 = new Content("Hello World");
        var content2 = new Content(["Hello", " ", "World"]);

        // Act
        var slice1 = content1.Slice(6, 5);
        var slice2 = content2.Slice(6, 5);
        var slice3 = new Content("World");

        // Assert
        Assert.Equal(slice1, slice2);
        Assert.Equal(slice2, slice3);
        Assert.Equal(slice1, slice3);
    }

    [Fact]
    public void Slice_ResultHashCode_ConsistentWithEquality()
    {
        // Arrange
        var content1 = new Content("Hello World");
        var content2 = new Content(["Hello", " ", "World"]);

        // Act
        var slice1 = content1.Slice(6, 5);
        var slice2 = content2.Slice(6, 5);

        // Assert
        Assert.Equal(slice1.GetHashCode(), slice2.GetHashCode());
    }

    [Fact]
    public void Slice_PreservesOriginalContent()
    {
        // Arrange
        var original = new Content(["Hello", " ", "World"]);
        var originalString = original.ToString();

        // Act
        var _ = original.Slice(3, 5);

        // Assert - original should be unchanged
        Assert.Equal(originalString, original.ToString());
        Assert.Equal(11, original.Length);
    }

    [Fact]
    public void Slice_EdgeCase_SliceAtPartBoundary()
    {
        // Arrange
        var content = new Content(["AAA", "BBB", "CCC"]);

        // Act
        var result1 = content[..3];
        var result2 = content.Slice(3, 3);
        var result3 = content.Slice(6, 3);

        // Assert
        Assert.Equal("AAA", result1.ToString());
        Assert.Equal("BBB", result2.ToString());
        Assert.Equal("CCC", result3.ToString());
    }

    [Fact]
    public void Slice_EdgeCase_CrossPartBoundary()
    {
        // Arrange
        var content = new Content(["AAA", "BBB", "CCC"]);

        // Act
        var result1 = content.Slice(2, 2);
        var result2 = content.Slice(5, 2);

        // Assert
        Assert.Equal("AB", result1.ToString());
        Assert.Equal("BC", result2.ToString());
    }

    [Fact]
    public void Slice_MultiPart_AllButFirst_ReturnsSlice()
    {
        // Arrange
        var content = new Content(["A", "B", "C", "D"]);

        // Act
        var result = content[1..];

        // Assert
        Assert.Equal("BCD", result.ToString());
        Assert.Equal(3, result.Length);
    }

    [Fact]
    public void Slice_MultiPart_AllButLast_ReturnsSlice()
    {
        // Arrange
        var content = new Content(["A", "B", "C", "D"]);

        // Act
        var result = content[..3];

        // Assert
        Assert.Equal("ABC", result.ToString());
        Assert.Equal(3, result.Length);
    }

    [Fact]
    public void Insert_EmptyContent_InsertsValue()
    {
        // Arrange
        var content = Content.Empty;

        // Act
        var result = content.Insert(0, "Test");

        // Assert
        Assert.Equal("Test", result.ToString());
        Assert.Equal(4, result.Length);
    }

    [Fact]
    public void Insert_SingleValue_AtBeginning_InsertsCorrectly()
    {
        // Arrange
        var content = new Content("World");

        // Act
        var result = content.Insert(0, "Hello ");

        // Assert
        Assert.Equal("Hello World", result.ToString());
        Assert.True(result.IsMultiPart);
    }

    [Fact]
    public void Insert_SingleValue_AtEnd_InsertsCorrectly()
    {
        // Arrange
        var content = new Content("Hello");

        // Act
        var result = content.Insert(5, " World");

        // Assert
        Assert.Equal("Hello World", result.ToString());
        Assert.True(result.IsMultiPart);
    }

    [Fact]
    public void Insert_SingleValue_InMiddle_InsertsCorrectly()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content.Insert(6, "Cruel ");

        // Assert
        Assert.Equal("Hello Cruel World", result.ToString());
        Assert.True(result.IsMultiPart);
        Assert.Equal(17, result.Length);
    }

    [Fact]
    public void Insert_MultiPart_InMiddle_InsertsCorrectly()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act
        var result = content.Insert(6, "Cruel ");

        // Assert
        Assert.Equal("Hello Cruel World", result.ToString());
        Assert.Equal(17, result.Length);
    }

    [Fact]
    public void Insert_MultiPart_AtPartBoundary_InsertsCorrectly()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act
        var result = content.Insert(5, "!");

        // Assert
        Assert.Equal("Hello! World", result.ToString());
    }

    [Fact]
    public void Insert_WithMemory_InsertsCorrectly()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content.Insert(6, "Beautiful ".AsMemory());

        // Assert
        Assert.Equal("Hello Beautiful World", result.ToString());
    }

    [Fact]
    public void Insert_EmptyValue_ReturnsOriginal()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content.Insert(5, "");

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public void Insert_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var content = new Content("Hello");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => content.Insert(-1, "Test"));
        Assert.Throws<ArgumentOutOfRangeException>(() => content.Insert(6, "Test"));
    }

    [Fact]
    public void Insert_NestedContent_InsertsCorrectly()
    {
        // Arrange
        var inner1 = new Content(["Hello", " "]);
        var inner2 = new Content(["World"]);
        var content = new Content([inner1, inner2]);

        // Act
        var result = content.Insert(6, "Beautiful ");

        // Assert
        Assert.Equal("Hello Beautiful World", result.ToString());
    }

    [Fact]
    public void Remove_EmptyContent_ReturnsEmpty()
    {
        // Arrange
        var content = Content.Empty;

        // Act
        var result = content.Remove(0, 0);

        // Assert
        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void Remove_SingleValue_FromBeginning_RemovesCorrectly()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content.Remove(0, 6);

        // Assert
        Assert.Equal("World", result.ToString());
        Assert.True(result.IsSingleValue);
    }

    [Fact]
    public void Remove_SingleValue_FromEnd_RemovesCorrectly()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content.Remove(5, 6);

        // Assert
        Assert.Equal("Hello", result.ToString());
        Assert.True(result.IsSingleValue);
    }

    [Fact]
    public void Remove_SingleValue_FromMiddle_RemovesCorrectly()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content.Remove(5, 1);

        // Assert
        Assert.Equal("HelloWorld", result.ToString());
        Assert.True(result.IsMultiPart);
    }

    [Fact]
    public void Remove_SingleValue_Everything_ReturnsEmpty()
    {
        // Arrange
        var content = new Content("Hello");

        // Act
        var result = content.Remove(0, 5);

        // Assert
        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void Remove_MultiPart_FromMiddle_RemovesCorrectly()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act
        var result = content.Remove(5, 1);

        // Assert
        Assert.Equal("HelloWorld", result.ToString());
    }

    [Fact]
    public void Remove_MultiPart_SpanningParts_RemovesCorrectly()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act
        var result = content.Remove(3, 5);

        // Assert
        Assert.Equal("Helrld", result.ToString());
    }

    [Fact]
    public void Remove_MultiPart_EntirePart_RemovesCorrectly()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act
        var result = content.Remove(5, 1);

        // Assert
        Assert.Equal("HelloWorld", result.ToString());
    }

    [Fact]
    public void Remove_ZeroCount_ReturnsOriginal()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content.Remove(5, 0);

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public void Remove_InvalidArguments_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var content = new Content("Hello");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => content.Remove(-1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => content.Remove(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => content.Remove(3, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => content.Remove(6, 0));
    }

    [Fact]
    public void Remove_NestedContent_RemovesCorrectly()
    {
        // Arrange
        var inner1 = new Content(["Hello", " "]);
        var inner2 = new Content(["World", "!"]);
        var content = new Content([inner1, inner2]);

        // Act
        var result = content.Remove(5, 6);

        // Assert
        Assert.Equal("Hello!", result.ToString());
    }

    [Fact]
    public void Replace_EmptyContent_ReturnsOriginal()
    {
        // Arrange
        var content = Content.Empty;

        // Act
        var result = content.Replace("test", "new");

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public void Replace_SingleValue_NoMatch_ReturnsOriginal()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content.Replace("xyz", "abc");

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public void Replace_SingleValue_SingleMatch_ReplacesCorrectly()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content.Replace("World", "Universe");

        // Assert
        Assert.Equal("Hello Universe", result.ToString());
    }

    [Fact]
    public void Replace_SingleValue_MultipleMatches_ReplacesAll()
    {
        // Arrange
        var content = new Content("Hello World World");

        // Act
        var result = content.Replace("World", "Universe");

        // Assert
        Assert.Equal("Hello Universe Universe", result.ToString());
    }

    [Fact]
    public void Replace_SingleValue_WithEmpty_RemovesMatches()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content.Replace(" World", "");

        // Assert
        Assert.Equal("Hello", result.ToString());
    }

    [Fact]
    public void Replace_MultiPart_WithinPart_ReplacesCorrectly()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act
        var result = content.Replace("World", "Universe");

        // Assert
        Assert.Equal("Hello Universe", result.ToString());
    }

    [Fact]
    public void Replace_MultiPart_SpanningParts_ReplacesCorrectly()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act
        var result = content.Replace(" W", "_W");

        // Assert
        Assert.Equal("Hello_World", result.ToString());
    }

    [Fact]
    public void Replace_MultiPart_MultipleMatches_ReplacesAll()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World", " ", "Hello"]);

        // Act
        var result = content.Replace("Hello", "Hi");

        // Assert
        Assert.Equal("Hi World Hi", result.ToString());
    }

    [Fact]
    public void Replace_CaseInsensitive_ReplacesCorrectly()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content.Replace("hello", "Hi", StringComparison.OrdinalIgnoreCase);

        // Assert
        Assert.Equal("Hi World", result.ToString());
    }

    [Fact]
    public void Replace_WithMemory_ReplacesCorrectly()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content.Replace("World".AsSpan(), "Universe".AsMemory());

        // Assert
        Assert.Equal("Hello Universe", result.ToString());
    }

    [Fact]
    public void Replace_EmptyOldValue_ThrowsArgumentException()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => content.Replace("", "test"));
        Assert.Throws<ArgumentException>(() => content.Replace("".AsSpan(), "test".AsMemory()));
    }

    [Fact]
    public void Replace_NestedContent_ReplacesCorrectly()
    {
        // Arrange
        var inner1 = new Content(["Hello", " "]);
        var inner2 = new Content(["World", "!"]);
        var content = new Content([inner1, inner2]);

        // Act
        var result = content.Replace("World", "Universe");

        // Assert
        Assert.Equal("Hello Universe!", result.ToString());
    }

    [Fact]
    public void Replace_OverlappingMatches_ReplacesNonOverlapping()
    {
        // Arrange
        var content = new Content("aaa");

        // Act
        var result = content.Replace("aa", "b");

        // Assert
        Assert.Equal("ba", result.ToString());
    }

    [Fact]
    public void Replace_MultiPart_SpanningMultipleParts_ReplacesCorrectly()
    {
        // Arrange
        var content = new Content(["AB", "CD", "EF"]);

        // Act
        var result = content.Replace("CDE", "XYZ");

        // Assert
        Assert.Equal("ABXYZF", result.ToString());
    }

    [Fact]
    public void Replace_MultiPart_Performance_ChecksEachPositionOnlyOnce()
    {
        // This test verifies that the optimized implementation doesn't walk
        // through all parts repeatedly for every position checked.
        // It creates a scenario where the old O(n×m) approach would be very slow.

        // Arrange - Create content with many parts
        var parts = new string[100];
        for (var i = 0; i < 100; i++)
        {
            parts[i] = $"Part{i:D3} ";  // "Part000 ", "Part001 ", etc.
        }

        // The string to find only appears once, near the end
        parts[95] = "Part095 Target ";  // Insert "Target" in part 95
        var contentWithTarget = new Content([.. parts]);

        // Act - Replace "Target" with "Replaced"
        var result = contentWithTarget.Replace("Target", "NewValue");

        // Assert
        Assert.Equal(contentWithTarget.ToString().Replace("Target", "NewValue"), result.ToString());

        // Verify the replacement happened
        Assert.Contains("NewValue", result.ToString());
        Assert.DoesNotContain("Target", result.ToString());
    }

    [Fact]
    public void Replace_MultiPart_SpanningParts_WithinAndAcrossBoundaries()
    {
        // This test specifically verifies that matches are found both
        // within parts and spanning across part boundaries

        // Arrange
        var content = new Content(["AB", "CD", "EF", "GH"]);

        // Act
        var result1 = content.Replace("CD", "XY");      // Match within single part
        var result2 = content.Replace("BC", "XY");      // Match across boundary AB|CD
        var result3 = content.Replace("DEF", "XYZ");    // Match across multiple boundaries CD|EF
        var result4 = content.Replace("CDEFG", "XYZ");  // Match across 3 part boundaries

        // Assert
        Assert.Equal("ABXYEFGH", result1.ToString());
        Assert.Equal("AXYDEFGH", result2.ToString());
        Assert.Equal("ABCXYZGH", result3.ToString());
        Assert.Equal("ABXYZH", result4.ToString());
    }

    [Fact]
    public void Replace_MultiPart_MatchAtEveryPartBoundary()
    {
        // Arrange - Pattern appears at every boundary between parts
        var content = new Content(["AXB", "CXD", "EXF"]);

        // Act - Replace all "X" occurrences
        var result = content.Replace("X", "Y");

        // Assert
        Assert.Equal("AYBCYDEYF", result.ToString());

        // Verify all three replacements happened
        var resultParts = new List<string>();
        foreach (var part in result.Parts)
        {
            resultParts.Add(part.ToString());
        }

        // The exact part structure may vary, but the content should be correct
        Assert.Equal(3, resultParts.Count(p => p.Contains('Y')));
    }

    [Fact]
    public void Replace_MultiPart_NoMatchesFound_ReturnsOriginal()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act
        var result = content.Replace("NotFound", "Replacement");

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public void Replace_MultiPart_EmptyParts_HandledCorrectly()
    {
        // Arrange - Include empty parts which shouldn't affect matching
        var content = new Content(["", "AB", "", "CD", "EF", ""]);

        // Act
        var result = content.Replace("BC", "XY");

        // Assert
        Assert.Equal("AXYDEF", result.ToString());
    }

    [Fact]
    public void Replace_MultiPart_OverlappingPotentialMatches()
    {
        // Arrange - Test that we handle overlapping patterns correctly
        var content = new Content(["AAA", "AAA"]);

        // Act - Should only match non-overlapping instances
        var result = content.Replace("AAA", "B");

        // Assert
        Assert.Equal("BB", result.ToString());
    }

    [Fact]
    public void Replace_MultiPart_MatchAtVeryEndOfContent()
    {
        // Arrange
        var content = new Content(["Start", " ", "Middle", " ", "End"]);

        // Act
        var result = content.Replace("End", "Finish");

        // Assert
        Assert.Equal("Start Middle Finish", result.ToString());
        Assert.EndsWith("Finish", result.ToString());
    }

    [Fact]
    public void Replace_MultiPart_MatchSpanningManyParts()
    {
        // Arrange - 10 single-character parts forming "ABCDEFGHIJ"
        var content = new Content(["A", "B", "C", "D", "E", "F", "G", "H", "I", "J"]);

        // Act - Replace a pattern spanning 5 parts
        var result = content.Replace("CDEFG", "XYZ");

        // Assert
        Assert.Equal("ABXYZHIJ", result.ToString());
    }

    [Fact]
    public void Replace_MultiPart_CaseInsensitive_SpanningBoundaries()
    {
        // Arrange
        var content = new Content(["hel", "LO", " ", "wor", "LD"]);

        // Act
        var result = content.Replace("hello", "hi", StringComparison.OrdinalIgnoreCase);

        // Assert
        Assert.Equal("hi worLD", result.ToString());
    }
    [Fact]
    public void Mutations_CanBeChained()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content
            .Insert(6, "Beautiful ")
            .Replace("Beautiful", "Amazing")
            .Remove(0, 6);

        // Assert
        Assert.Equal("Amazing World", result.ToString());
    }

    [Fact]
    public void Mutations_PreserveOriginal()
    {
        // Arrange
        var original = new Content("Hello World");

        // Act
        var inserted = original.Insert(6, "Cruel ");
        var removed = original.Remove(6, 5);
        var replaced = original.Replace("World", "Universe");

        // Assert
        Assert.Equal("Hello World", original.ToString());
        Assert.Equal("Hello Cruel World", inserted.ToString());
        Assert.Equal("Hello ", removed.ToString());
        Assert.Equal("Hello Universe", replaced.ToString());
    }

    [Fact]
    public void Insert_PreservesMemorySlicing()
    {
        // Arrange
        var original = "Hello World";
        var content = new Content(original);

        // Act
        var result = content.Insert(5, "!");

        // Assert
        Assert.True(result.IsMultiPart);
        // Verify no string allocation happened for the original content
        foreach (var part in result.Parts)
        {
            Assert.NotEqual("Hello!", part.ToString());
            Assert.NotEqual("! World", part.ToString());
        }
    }

    [Fact]
    public void Remove_PreservesMemorySlicing()
    {
        // Arrange
        var original = "Hello World";
        var content = new Content(original);

        // Act
        var result = content.Remove(5, 1);

        // Assert
        Assert.True(result.IsMultiPart);
        // Verify no new string allocation for the parts
        Assert.Equal(2, result.Parts.Count);
    }

    [Fact]
    public void Replace_PreservesMemorySlicing()
    {
        // Arrange
        var original = "Hello World World";
        var content = new Content(original);

        // Act
        var result = content.Replace(" ", "_");

        // Assert
        Assert.True(result.IsMultiPart);
        // The original memory should be sliced, not copied
        var parts = new List<string>();
        foreach (var part in result.Parts)
        {
            parts.Add(part.ToString());
        }
        Assert.Contains("Hello", parts);
        Assert.Contains("_", parts);
    }

    [Fact]
    public void Replace_AlternateInsideAndAcrossParts()
    {
        // Arrange
        var content = new Content(["Hello", "World", "Hel", "loWor", "ldHello", "World", "HelloWor", "ld"]);

        // Act
        var result = content.Replace("World", "_");

        // Assert
        Assert.Equal("Hello_Hello_Hello_Hello_", result.ToString());
    }

    [Fact]
    public void Replace_SingleChar_EmptyContent_ReturnsOriginal()
    {
        // Arrange
        var content = Content.Empty;

        // Act
        var result = content.Replace('a', 'b');

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public void Replace_SingleChar_NoMatch_ReturnsOriginal()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content.Replace('x', 'y');

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public void Replace_SingleChar_SingleMatch_ReplacesCorrectly()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content.Replace('o', 'a');

        // Assert
        Assert.Equal("Hella Warld", result.ToString());
    }

    [Fact]
    public void Replace_SingleChar_MultipleMatches_ReplacesAll()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content.Replace('l', 'x');

        // Assert
        Assert.Equal("Hexxo Worxd", result.ToString());
    }

    [Fact]
    public void Replace_SingleChar_SameChar_ReturnsOriginal()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content.Replace('o', 'o');

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public void Replace_SingleChar_MultiPart_ReplacesCorrectly()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World", "!"]);

        // Act
        var result = content.Replace('l', 'x');

        // Assert
        Assert.Equal("Hexxo Worxd!", result.ToString());
    }

    [Fact]
    public void Replace_SingleChar_NestedContent_ReplacesCorrectly()
    {
        // Arrange
        var inner1 = new Content(["Hell", "o"]);
        var inner2 = new Content([" Wor", "ld"]);
        var content = new Content([inner1, inner2]);

        // Act
        var result = content.Replace('l', 'x');

        // Assert
        Assert.Equal("Hexxo Worxd", result.ToString());
    }

    [Fact]
    public void Replace_WithContentValue_EmptyOldValue_ThrowsArgumentException()
    {
        // Arrange
        var content = new Content("Hello World");
        var newValue = new Content("replacement");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => content.Replace("".AsSpan(), newValue));
    }

    [Fact]
    public void Replace_WithContentValue_SingleMatch_ReplacesCorrectly()
    {
        // Arrange
        var content = new Content("Hello World");
        var newValue = new Content("Beautiful");

        // Act
        var result = content.Replace("World", newValue);

        // Assert
        Assert.Equal("Hello Beautiful", result.ToString());
    }

    [Fact]
    public void Replace_WithContentValue_MultipartReplacement_ReplacesCorrectly()
    {
        // Arrange
        var content = new Content("Hello World");
        var newValue = new Content(["Beau", "ti", "ful"]);

        // Act
        var result = content.Replace("World", newValue);

        // Assert
        Assert.Equal("Hello Beautiful", result.ToString());
    }

    [Fact]
    public void Replace_WithContentValue_EmptyReplacement_RemovesMatches()
    {
        // Arrange
        var content = new Content("Hello World World");
        var newValue = Content.Empty;

        // Act
        var result = content.Replace("World ", newValue);

        // Assert
        Assert.Equal("Hello World", result.ToString());
    }

    [Fact]
    public void Replace_EdgeCase_OldValueLongerThanContent_ReturnsOriginal()
    {
        // Arrange
        var content = new Content("Hi");

        // Act
        var result = content.Replace("Hello", "Goodbye");

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public void Replace_EdgeCase_ReplacementAtVeryStart_ReplacesCorrectly()
    {
        // Arrange
        var content = new Content(["Start", "Middle", "End"]);

        // Act
        var result = content.Replace("Start", "Begin");

        // Assert
        Assert.Equal("BeginMiddleEnd", result.ToString());
    }

    [Fact]
    public void Replace_EdgeCase_ReplacementAtVeryEnd_ReplacesCorrectly()
    {
        // Arrange
        var content = new Content(["Start", "Middle", "End"]);

        // Act
        var result = content.Replace("End", "Finish");

        // Assert
        Assert.Equal("StartMiddleFinish", result.ToString());
    }

    [Fact]
    public void Replace_EdgeCase_ExactMatch_ReplacesEntireContent()
    {
        // Arrange
        var content = new Content("Hello");

        // Act
        var result = content.Replace("Hello", "Goodbye");

        // Assert
        Assert.Equal("Goodbye", result.ToString());
    }

    [Fact]
    public void Replace_ComplexCrossPartMatch_MultipleOccurrences()
    {
        // Arrange - Multiple cross-part matches
        var content = new Content(["AB", "CD", "EF", "AB", "CD", "EF", "GH"]);

        // Act
        var result = content.Replace("BC", "XY");

        // Assert
        Assert.Equal("AXYDEFAXYDEFGH", result.ToString());
    }

    [Fact]
    public void Replace_NestedReplacements_WithinSamePart()
    {
        // Arrange
        var content = new Content("abcabcabc");

        // Act
        var result = content.Replace("abc", "x");

        // Assert
        Assert.Equal("xxx", result.ToString());
    }

    [Fact]
    public void Replace_VeryLongMatch_SpanningManyParts()
    {
        // Arrange - Create a pattern that spans 8 parts
        var content = new Content(["A", "B", "C", "D", "E", "F", "G", "H", "I", "J"]);

        // Act
        var result = content.Replace("ABCDEFGH", "REPLACED");

        // Assert
        Assert.Equal("REPLACEDIJ", result.ToString());
    }

    [Fact]
    public void Replace_RepeatingPattern_CrossPart()
    {
        // Arrange
        var content = new Content(["aba", "bab", "aba", "bab"]);

        // Act
        var result = content.Replace("ab", "X");

        // Assert
        Assert.Equal("XXXXXX", result.ToString());
    }

    [Fact]
    public void Replace_WithUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var content = new Content(["Hello ", "世界", "! How are you?"]);

        // Act
        var result = content.Replace("世界", "World");

        // Assert
        Assert.Equal("Hello World! How are you?", result.ToString());
    }

    [Fact]
    public void Replace_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var content = new Content(["Line1\n", "Line2\r\n", "Line3\t"]);

        // Act
        var result = content.Replace("\n", "_NEWLINE_");

        // Assert
        Assert.Equal("Line1_NEWLINE_Line2\r_NEWLINE_Line3\t", result.ToString());
    }

    [Fact]
    public void Replace_CaseSensitive_DoesNotMatchDifferentCase()
    {
        // Arrange
        var content = new Content(["Hello", " ", "WORLD"]);

        // Act
        var result = content.Replace("world", "universe");

        // Assert
        Assert.Equal("Hello WORLD", result.ToString());
    }

    [Fact]
    public void Replace_CaseInsensitive_MatchesDifferentCase()
    {
        // Arrange
        var content = new Content(["Hello", " ", "WORLD"]);

        // Act
        var result = content.Replace("world", "universe", StringComparison.OrdinalIgnoreCase);

        // Assert
        Assert.Equal("Hello universe", result.ToString());
    }

    [Fact]
    public void Replace_CaseInsensitive_CrossPartMatch()
    {
        // Arrange
        var content = new Content(["hel", "LO", " ", "wor", "LD"]);

        // Act
        var result = content.Replace("HELLO WORLD", "hi there", StringComparison.OrdinalIgnoreCase);

        // Assert
        Assert.Equal("hi there", result.ToString());
    }

    [Fact]
    public void Replace_WithEmptyParts_IgnoresEmptyParts()
    {
        // Arrange
        var content = new Content(["", "A", "", "B", "", "C", "", "D", ""]);

        // Act
        var result = content.Replace("BC", "XY");

        // Assert
        Assert.Equal("AXYD", result.ToString());
    }

    [Fact]
    public void Replace_SingleCharacterParts_HandlesCorrectly()
    {
        // Arrange
        var content = new Content(["H", "e", "l", "l", "o", " ", "W", "o", "r", "l", "d"]);

        // Act
        var result = content.Replace("llo W", "y ");

        // Assert
        Assert.Equal("Hey orld", result.ToString());
    }

    [Fact]
    public void Replace_OverlappingPatterns_ReplacesNonOverlapping()
    {
        // Arrange
        var content = new Content("aaaa");

        // Act
        var result = content.Replace("aaa", "b");

        // Assert
        Assert.Equal("ba", result.ToString());
    }

    [Fact]
    public void Replace_AdjacentMatches_ReplacesAll()
    {
        // Arrange
        var content = new Content("abcabc");

        // Act
        var result = content.Replace("abc", "X");

        // Assert
        Assert.Equal("XX", result.ToString());
    }

    [Fact]
    public void Replace_PartialMatchAtEnd_DoesNotReplace()
    {
        // Arrange
        var content = new Content(["Hello", "Wor"]);

        // Act
        var result = content.Replace("World", "Universe");

        // Assert
        Assert.Equal("HelloWor", result.ToString());
    }

    [Fact]
    public void Replace_LongerReplacement_IncreasesLength()
    {
        // Arrange
        var content = new Content("Hi");

        // Act
        var result = content.Replace("Hi", "Hello there");

        // Assert
        Assert.Equal("Hello there", result.ToString());
        Assert.Equal(11, result.Length);
    }

    [Fact]
    public void Replace_ShorterReplacement_DecreasesLength()
    {
        // Arrange
        var content = new Content("Hello there");

        // Act
        var result = content.Replace("Hello there", "Hi");

        // Assert
        Assert.Equal("Hi", result.ToString());
        Assert.Equal(2, result.Length);
    }

    [Fact]
    public void Replace_ChainedReplacements_WorkCorrectly()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act
        var result = content
            .Replace("Hello", "Hi")
            .Replace("World", "Universe")
            .Replace(" ", "_");

        // Assert
        Assert.Equal("Hi_Universe", result.ToString());
    }

    [Fact]
    public void Replace_DeeplyNestedContent_HandlesCorrectly()
    {
        // Arrange
        var level1 = new Content(["AB", "CD"]);
        var level2 = new Content([level1, new Content("EF")]);
        var level3 = new Content([level2, new Content(["GH", "IJ"])]);

        // Act
        var result = level3.Replace("DE", "XY");

        // Assert
        Assert.Equal("ABCXYFGHIJ", result.ToString());
    }

    [Fact]
    public void Replace_ManySmallParts_Performance()
    {
        // Arrange - Create content with many small parts
        var parts = new string[1000];
        for (var i = 0; i < 1000; i++)
        {
            parts[i] = i % 2 == 0 ? "A" : "B";
        }

        var content = new Content(parts.ToImmutableArray());

        // Act
        var result = content.Replace("AB", "X");

        // Assert
        // Should replace every adjacent A,B pair
        var expected = string.Concat(Enumerable.Repeat("X", 500));
        Assert.Equal(expected, result.ToString());
    }

    [Fact]
    public void Replace_VeryLongSinglePart_HandlesCorrectly()
    {
        // Arrange
        var longString = new string('A', 10000) + "TARGET" + new string('B', 10000);
        var content = new Content(longString);

        // Act
        var result = content.Replace("TARGET", "REPLACED");

        // Assert
        Assert.Equal(new string('A', 10000) + "REPLACED" + new string('B', 10000), result.ToString());
        Assert.Equal(20008, result.Length); // 10000 + 8 + 10000
    }

    [Fact]
    public void Replace_MultipleInstancesOfSameChar_InDifferentParts()
    {
        // Arrange
        var content = new Content(["aaa", "bbb", "aaa", "ccc", "aaa"]);

        // Act
        var result = content.Replace('a', 'x');

        // Assert
        Assert.Equal("xxxbbbxxxcccxxx", result.ToString());
    }

    [Fact]
    public void Replace_AlternatingReplacementPattern()
    {
        // Arrange
        var content = new Content(["X", "Y", "X", "Y", "X", "Y"]);

        // Act
        var result = content.Replace("XY", "Z");

        // Assert
        Assert.Equal("ZZZ", result.ToString());
    }

    [Fact]
    public void Replace_PatternLongerThanIndividualParts()
    {
        // Arrange - Pattern is longer than any single part
        var content = new Content(["AB", "CD", "EF", "GH"]);

        // Act
        var result = content.Replace("ABCDEFGH", "REPLACED");

        // Assert
        Assert.Equal("REPLACED", result.ToString());
    }

    [Fact]
    public void Replace_MatchStartsInLastPart()
    {
        // Arrange
        var content = new Content(["Hello ", "World", "!"]);

        // Act
        var result = content.Replace("World!", "Universe.");

        // Assert
        Assert.Equal("Hello Universe.", result.ToString());
    }

    [Fact]
    public void Replace_MultipleNonOverlappingCrossPartMatches()
    {
        // Arrange
        var content = new Content(["AB", "CD", "EF", "AB", "CD", "EF"]);

        // Act
        var result = content.Replace("DE", "XY");

        // Assert
        Assert.Equal("ABCXYF" + "ABCXYF", result.ToString());
    }

    [Fact]
    public void Replace_PatternAtExactPartBoundaries()
    {
        // Arrange - Pattern exactly spans part boundaries
        var content = new Content(["ABC", "DEF", "GHI"]);

        // Act
        var result1 = content.Replace("CDE", "XYZ");
        var result2 = content.Replace("FGH", "XYZ");

        // Assert
        Assert.Equal("ABXYZFGHI", result1.ToString());
        Assert.Equal("ABCDEXYZ" + "I", result2.ToString());
    }

    [Fact]
    public void Replace_WithWhitespaceOnly_HandlesCorrectly()
    {
        // Arrange
        var content = new Content(["   ", "   ", "  "]);

        // Act
        var result = content.Replace("  ", "_");

        // Assert
        Assert.Equal("____", result.ToString()); // Should replace overlapping spaces
    }

    [Fact]
    public void Replace_EmptyStringReplacement_RemovesMatches()
    {
        // Arrange
        var content = new Content(["Remove", "This", "Remove", "This"]);

        // Act
        var result = content.Replace("Remove", "");

        // Assert
        Assert.Equal("ThisThis", result.ToString());
    }

    [Fact]
    public void Replace_ReplacementCreatesNewMatchOpportunity()
    {
        // Arrange
        var content = new Content("abc");

        // Act - This should not cause infinite recursion
        var result = content.Replace("b", "bb");

        // Assert
        Assert.Equal("abbc", result.ToString());
    }

    [Fact]
    public void Replace_ConsecutiveIdenticalParts_HandlesCorrectly()
    {
        // Arrange
        var content = new Content(["ABC", "ABC", "ABC"]);

        // Act
        var result = content.Replace("ABC", "X");

        // Assert
        Assert.Equal("XXX", result.ToString());
    }

    [Fact]
    public void Replace_ResultPreservesEqualityContract()
    {
        // Arrange
        var content1 = new Content(["Hello", " ", "World"]);
        var content2 = new Content("Hello World");

        // Act
        var result1 = content1.Replace("World", "Universe");
        var result2 = content2.Replace("World", "Universe");

        // Assert
        Assert.Equal(result1, result2);
        Assert.Equal(result1.GetHashCode(), result2.GetHashCode());
    }

    [Fact]
    public void Replace_WithContentReplacement_PreservesStructure()
    {
        // Arrange
        var content = new Content("Hello World");
        var replacement = new Content(["Beautiful", " ", "Universe"]);

        // Act
        var result = content.Replace("World", replacement);

        // Assert
        Assert.Equal("Hello Beautiful Universe", result.ToString());
        Assert.True(result.IsMultiPart);
    }

    [Fact]
    public void Replace_StressTest_ManyReplacements()
    {
        // Arrange - Create content with many potential matches
        var parts = new List<string>();
        for (var i = 0; i < 100; i++)
        {
            parts.Add(i % 3 == 0 ? "TARGET" : $"Part{i}");
        }

        var content = new Content(parts.ToImmutableArray());

        // Act
        var result = content.Replace("TARGET", "REPLACED");

        // Assert
        var expectedReplacements = parts.Count(p => p == "TARGET");
        var actualReplacements = result.ToString().Split(["REPLACED"], StringSplitOptions.None).Length - 1;
        Assert.Equal(expectedReplacements, actualReplacements);
    }

    [Fact]
    public void Replace_CrossPartMatch_WithVaryingPartSizes()
    {
        // Arrange - Parts of different sizes
        var content = new Content(["A", "BB", "CCC", "DDDD", "EEEEE"]);

        // Act
        var result = content.Replace("BCCCD", "X");

        // Assert
        Assert.Equal("ABXDDD" + "EEEEE", result.ToString());
    }

    [Fact]
    public void Replace_PatternAppearsMultipleTimesInSamePart()
    {
        // Arrange
        var content = new Content(["ABCABCABC", "DEFDEF"]);

        // Act
        var result = content.Replace("ABC", "X");

        // Assert
        Assert.Equal("XXXDEFDEF", result.ToString());
    }

    [Fact]
    public void Replace_PartialMatchFollowedByFullMatch()
    {
        // Arrange
        var content = new Content(["ABCDE", "FGHABCDE"]);

        // Act
        var result = content.Replace("ABCDE", "X");

        // Assert
        Assert.Equal("XFGHX", result.ToString());
    }

    [Fact]
    public void Builder_Constructor_WithInitialCapacity_CreatesBuilder()
    {
        // Arrange & Act
        using var builder = new Content.Builder(10);

        // Assert
        var result = builder.ToContent();
        Assert.True(result.IsEmpty);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void Builder_Constructor_WithFlattenTrue_CreatesBuilder()
    {
        // Arrange & Act
        using var builder = new Content.Builder(10, flatten: true);

        // Assert
        var result = builder.ToContent();
        Assert.True(result.IsEmpty);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void Builder_Constructor_WithDefaultValues_CreatesBuilder()
    {
        // Arrange & Act
        using var builder = new Content.Builder(5);

        // Assert
        var result = builder.ToContent();
        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void Builder_AddContent_SingleContent_AddsCorrectly()
    {
        // Arrange
        using var builder = new Content.Builder(10);
        var content = new Content("Hello World");

        // Act
        builder.Add(content);
        var result = builder.ToContent();

        // Assert
        Assert.False(result.IsEmpty);
        Assert.Equal("Hello World", result.ToString());
        Assert.Equal(11, result.Length);
    }

    [Fact]
    public void Builder_AddContent_EmptyContent_IgnoresEmpty()
    {
        // Arrange
        using var builder = new Content.Builder(10);
        var emptyContent = Content.Empty;
        var validContent = new Content("Test");

        // Act
        builder.Add(emptyContent);
        builder.Add(validContent);
        var result = builder.ToContent();

        // Assert
        Assert.Equal("Test", result.ToString());
        Assert.Equal(4, result.Length);
    }

    [Fact]
    public void Builder_AddContent_MultipleContent_CreatesMultiPart()
    {
        // Arrange
        using var builder = new Content.Builder(10);
        var content1 = new Content("Hello");
        var content2 = new Content(" ");
        var content3 = new Content("World");

        // Act
        builder.Add(content1);
        builder.Add(content2);
        builder.Add(content3);
        var result = builder.ToContent();

        // Assert
        Assert.True(result.IsMultiPart);
        Assert.Equal("Hello World", result.ToString());
        Assert.Equal(11, result.Length);
    }

    [Fact]
    public void Builder_AddContent_WithFlattenTrue_FlattensNestedContent()
    {
        // Arrange
        using var builder = new Content.Builder(10, flatten: true);
        var nestedContent = new Content(["A", "B", "C"]);
        var singleContent = new Content("D");

        // Act
        builder.Add(nestedContent);
        builder.Add(singleContent);
        var result = builder.ToContent();

        // Assert
        Assert.Equal("ABCD", result.ToString());
        Assert.Equal(4, result.Length);
        Assert.True(result.IsMultiPart);
    }

    [Fact]
    public void Builder_AddContent_WithFlattenFalse_PreservesNesting()
    {
        // Arrange
        using var builder = new Content.Builder(10, flatten: false);
        var nestedContent = new Content(["A", "B", "C"]);
        var singleContent = new Content("D");

        // Act
        builder.Add(nestedContent);
        builder.Add(singleContent);
        var result = builder.ToContent();

        // Assert
        Assert.Equal("ABCD", result.ToString());
        Assert.Equal(4, result.Length);
        Assert.True(result.IsMultiPart);
    }

    [Fact]
    public void Builder_AddContent_WithFlattenTrue_SkipsEmptyParts()
    {
        // Arrange
        using var builder = new Content.Builder(10, flatten: true);
        var contentWithEmpty = new Content(["", "Hello", "", "World", ""]);

        // Act
        builder.Add(contentWithEmpty);
        var result = builder.ToContent();

        // Assert
        Assert.Equal("HelloWorld", result.ToString());
        Assert.Equal(10, result.Length);
    }

    [Fact]
    public void Builder_AddReadOnlyMemory_ValidValue_AddsCorrectly()
    {
        // Arrange
        using var builder = new Content.Builder(10);
        var memory = "Hello World".AsMemory();

        // Act
        builder.Add(memory);
        var result = builder.ToContent();

        // Assert
        Assert.Equal("Hello World", result.ToString());
        Assert.Equal(11, result.Length);
    }

    [Fact]
    public void Builder_AddReadOnlyMemory_EmptyValue_IgnoresEmpty()
    {
        // Arrange
        using var builder = new Content.Builder(10);
        var emptyMemory = ReadOnlyMemory<char>.Empty;
        var validMemory = "Test".AsMemory();

        // Act
        builder.Add(emptyMemory);
        builder.Add(validMemory);
        var result = builder.ToContent();

        // Assert
        Assert.Equal("Test", result.ToString());
        Assert.Equal(4, result.Length);
    }

    [Fact]
    public void Builder_AddReadOnlyMemory_MultipleValues_CreatesMultiPart()
    {
        // Arrange
        using var builder = new Content.Builder(10);
        var memory1 = "Hello".AsMemory();
        var memory2 = " ".AsMemory();
        var memory3 = "World".AsMemory();

        // Act
        builder.Add(memory1);
        builder.Add(memory2);
        builder.Add(memory3);
        var result = builder.ToContent();

        // Assert
        Assert.True(result.IsMultiPart);
        Assert.Equal("Hello World", result.ToString());
        Assert.Equal(11, result.Length);
    }

    [Fact]
    public void Builder_AddString_ValidValue_AddsCorrectly()
    {
        // Arrange
        using var builder = new Content.Builder(10);
        var text = "Hello World";

        // Act
        builder.Add(text);
        var result = builder.ToContent();

        // Assert
        Assert.Equal("Hello World", result.ToString());
        Assert.Equal(11, result.Length);
    }

    [Fact]
    public void Builder_AddString_NullValue_IgnoresNull()
    {
        // Arrange
        using var builder = new Content.Builder(10);
        string nullString = null!;
        var validString = "Test";

        // Act
        builder.Add(nullString);
        builder.Add(validString);
        var result = builder.ToContent();

        // Assert
        Assert.Equal("Test", result.ToString());
        Assert.Equal(4, result.Length);
    }

    [Fact]
    public void Builder_AddString_EmptyValue_IgnoresEmpty()
    {
        // Arrange
        using var builder = new Content.Builder(10);
        var emptyString = "";
        var validString = "Test";

        // Act
        builder.Add(emptyString);
        builder.Add(validString);
        var result = builder.ToContent();

        // Assert
        Assert.Equal("Test", result.ToString());
        Assert.Equal(4, result.Length);
    }

    [Fact]
    public void Builder_AddString_MultipleValues_CreatesMultiPart()
    {
        // Arrange
        using var builder = new Content.Builder(10);

        // Act
        builder.Add("Hello");
        builder.Add(" ");
        builder.Add("World");
        var result = builder.ToContent();

        // Assert
        Assert.True(result.IsMultiPart);
        Assert.Equal("Hello World", result.ToString());
        Assert.Equal(11, result.Length);
    }

    [Fact]
    public void Builder_MixedAdd_ContentMemoryString_CreatesMultiPart()
    {
        // Arrange
        using var builder = new Content.Builder(10);
        var content = new Content("Hello");
        var memory = " ".AsMemory();
        var text = "World";

        // Act
        builder.Add(content);
        builder.Add(memory);
        builder.Add(text);
        var result = builder.ToContent();

        // Assert
        Assert.True(result.IsMultiPart);
        Assert.Equal("Hello World", result.ToString());
        Assert.Equal(11, result.Length);
    }

    [Fact]
    public void Builder_ToContent_EmptyBuilder_ReturnsEmpty()
    {
        // Arrange
        using var builder = new Content.Builder(10);

        // Act
        var result = builder.ToContent();

        // Assert
        Assert.True(result.IsEmpty);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void Builder_ToContent_SingleItem_ReturnsSingleValue()
    {
        // Arrange
        using var builder = new Content.Builder(10);
        builder.Add("Hello World");

        // Act
        var result = builder.ToContent();

        // Assert
        Assert.True(result.IsSingleValue);
        Assert.False(result.IsMultiPart);
        Assert.Equal("Hello World", result.ToString());
    }

    [Fact]
    public void Builder_ToContent_MultipleItems_ReturnsMultiPart()
    {
        // Arrange
        using var builder = new Content.Builder(10);
        builder.Add("Hello");
        builder.Add(" World");

        // Act
        var result = builder.ToContent();

        // Assert
        Assert.False(result.IsSingleValue);
        Assert.True(result.IsMultiPart);
        Assert.Equal("Hello World", result.ToString());
    }

    [Fact]
    public void Builder_ToContent_CalledMultipleTimes_ReturnsSameResult()
    {
        // Arrange
        using var builder = new Content.Builder(10);
        builder.Add("Hello");
        builder.Add(" World");

        // Act
        var result1 = builder.ToContent();
        var result2 = builder.ToContent();

        // Assert
        Assert.Equal(result1, result2);
        Assert.Equal(result1.ToString(), result2.ToString());
    }

    [Fact]
    public void Builder_Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var builder = new Content.Builder(10);
        builder.Add("Test");

        // Act & Assert - Should not throw
        builder.Dispose();
        builder.Dispose();
        builder.Dispose();
    }

    [Fact]
    public void Builder_UsingStatement_DisposesCorrectly()
    {
        // Arrange & Act
        Content result;
        using (var builder = new Content.Builder(10))
        {
            builder.Add("Hello");
            builder.Add(" World");
            result = builder.ToContent();
        }

        // Assert
        Assert.Equal("Hello World", result.ToString());
    }

    [Fact]
    public void Builder_WithLargeCapacity_HandlesCorrectly()
    {
        // Arrange
        using var builder = new Content.Builder(1000);

        // Act
        for (var i = 0; i < 100; i++)
        {
            builder.Add($"Part{i}");
        }
        var result = builder.ToContent();

        // Assert
        Assert.True(result.IsMultiPart);
        var expected = string.Concat(Enumerable.Range(0, 100).Select(i => $"Part{i}"));
        Assert.Equal(expected, result.ToString());
    }

    [Fact]
    public void Builder_FlattenTrue_WithDeeplyNestedContent_FlattensCompletely()
    {
        // Arrange
        using var builder = new Content.Builder(10, flatten: true);
        var level1 = new Content(["A", "B"]);
        var level2 = new Content([level1, new Content("C")]);
        var level3 = new Content([level2, new Content("D")]);

        // Act
        builder.Add(level3);
        var result = builder.ToContent();

        // Assert
        Assert.Equal("ABCD", result.ToString());
        Assert.Equal(4, result.Length);
    }

    [Fact]
    public void Builder_FlattenTrue_WithEmptyAndNonEmptyContent_FiltersEmpty()
    {
        // Arrange
        using var builder = new Content.Builder(10, flatten: true);
        var contentWithEmpty = new Content([Content.Empty, new Content("Hello"), Content.Empty]);
        var anotherContent = new Content(["", "World", ""]);

        // Act
        builder.Add(contentWithEmpty);
        builder.Add(anotherContent);
        var result = builder.ToContent();

        // Assert
        Assert.Equal("HelloWorld", result.ToString());
    }

    [Fact]
    public void Builder_FlattenFalse_WithNestedContent_PreservesStructure()
    {
        // Arrange
        using var builder = new Content.Builder(10, flatten: false);
        var nestedContent = new Content(["A", "B", "C"]);

        // Act
        builder.Add(nestedContent);
        var result = builder.ToContent();

        // Assert
        Assert.Equal("ABC", result.ToString());
        Assert.Equal(3, result.Length);
    }

    [Fact]
    public void Builder_AddUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        using var builder = new Content.Builder(10);

        // Act
        builder.Add("Hello ");
        builder.Add("世界");
        builder.Add("!");
        var result = builder.ToContent();

        // Assert
        Assert.Equal("Hello 世界!", result.ToString());
        Assert.Equal(9, result.Length);
    }

    [Fact]
    public void Builder_AddSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        using var builder = new Content.Builder(10);

        // Act
        builder.Add("Tab:\t");
        builder.Add("NewLine:\n");
        builder.Add("Return:\r");
        var result = builder.ToContent();

        // Assert
        Assert.Contains('\t', result.ToString());
        Assert.Contains('\n', result.ToString());
        Assert.Contains('\r', result.ToString());
    }

    [Fact]
    public void Builder_AddWhitespaceOnly_IncludesWhitespace()
    {
        // Arrange
        using var builder = new Content.Builder(10);

        // Act
        builder.Add("   ");
        builder.Add("\t\t");
        builder.Add("\n\n");
        var result = builder.ToContent();

        // Assert
        Assert.Equal("   \t\t\n\n", result.ToString());
        Assert.Equal(7, result.Length);
    }

    [Fact]
    public void Builder_MixedEmptyAndValid_IgnoresOnlyEmpty()
    {
        // Arrange
        using var builder = new Content.Builder(10);

        // Act
        builder.Add(Content.Empty);
        builder.Add("");
        builder.Add(ReadOnlyMemory<char>.Empty);
        builder.Add("Valid");
        builder.Add(Content.Empty);
        var result = builder.ToContent();

        // Assert
        Assert.Equal("Valid", result.ToString());
        Assert.Equal(5, result.Length);
        Assert.True(result.IsSingleValue);
    }

    [Fact]
    public void Builder_SingleCharacterParts_HandlesCorrectly()
    {
        // Arrange
        using var builder = new Content.Builder(10);

        // Act
        builder.Add("H");
        builder.Add("e");
        builder.Add("l");
        builder.Add("l");
        builder.Add("o");
        var result = builder.ToContent();

        // Assert
        Assert.Equal("Hello", result.ToString());
        Assert.Equal(5, result.Length);
        Assert.True(result.IsMultiPart);
    }

    [Fact]
    public void Builder_PerformanceTest_ManySmallAdditions()
    {
        // Arrange
        using var builder = new Content.Builder(1000);

        // Act
        for (var i = 0; i < 500; i++)
        {
            builder.Add(i % 2 == 0 ? "A" : "B");
        }
        var result = builder.ToContent();

        // Assert
        Assert.Equal(500, result.Length);
        Assert.True(result.IsMultiPart);
        var expected = string.Concat(Enumerable.Range(0, 500).Select(i => i % 2 == 0 ? "A" : "B"));
        Assert.Equal(expected, result.ToString());
    }

    [Fact]
    public void Builder_ZeroInitialCapacity_WorksCorrectly()
    {
        // Arrange
        using var builder = new Content.Builder(0);

        // Act
        builder.Add("Hello");
        builder.Add(" World");
        var result = builder.ToContent();

        // Assert
        Assert.Equal("Hello World", result.ToString());
        Assert.True(result.IsMultiPart);
    }

    [Fact]
    public void Builder_FlattenTrue_WithComplexNesting_ProducesCorrectResult()
    {
        // Arrange
        using var builder = new Content.Builder(10, flatten: true);
        var branch1 = new Content([new Content("A"), new Content("B")]);
        var branch2 = new Content([new Content(["C", "D"]), new Content("E")]);
        var complexContent = new Content([branch1, new Content("F"), branch2]);

        // Act
        builder.Add(complexContent);
        var result = builder.ToContent();

        // Assert
        Assert.Equal("ABFCDE", result.ToString());
        Assert.Equal(6, result.Length);
    }

    [Fact]
    public void Builder_ResultEquality_WithSameContent()
    {
        // Arrange
        using var builder1 = new Content.Builder(10);
        using var builder2 = new Content.Builder(10);

        // Act
        builder1.Add("Hello");
        builder1.Add(" World");

        builder2.Add("Hello World");

        var result1 = builder1.ToContent();
        var result2 = builder2.ToContent();

        // Assert
        Assert.Equal(result1, result2);
        Assert.Equal(result1.GetHashCode(), result2.GetHashCode());
    }

    [Fact]
    public void Builder_GrowthBehavior_ExceedsInitialCapacity()
    {
        // Arrange
        using var builder = new Content.Builder(2); // Small initial capacity

        // Act
        for (var i = 0; i < 10; i++)
        {
            builder.Add($"Item{i}");
        }
        var result = builder.ToContent();

        // Assert
        Assert.True(result.IsMultiPart);
        var expected = string.Concat(Enumerable.Range(0, 10).Select(i => $"Item{i}"));
        Assert.Equal(expected, result.ToString());
    }

    [Fact]
    public void Builder_FlattenTrue_OnlyNonEmptyParts_AreAdded()
    {
        // Arrange
        using var builder = new Content.Builder(10, flatten: true);
        var contentWithManyEmpty = new Content([
            "", "A", "", "", "B", "", "C", "", "", ""
        ]);

        // Act
        builder.Add(contentWithManyEmpty);
        var result = builder.ToContent();

        // Assert
        Assert.Equal("ABC", result.ToString());
        Assert.Equal(3, result.Length);
    }

    [Fact]
    public void Join_WithContentArray_EmptyValues_ReturnsEmpty()
    {
        // Arrange
        var separator = new Content(", ");
        ReadOnlySpan<Content> values = [];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.True(result.IsEmpty);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void Join_WithContentArray_SingleValue_ReturnsSingleValue()
    {
        // Arrange
        var separator = new Content(", ");
        ReadOnlySpan<Content> values = [new Content("Hello")];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("Hello", result.ToString());
        Assert.Equal(5, result.Length);
    }

    [Fact]
    public void Join_WithContentArray_MultipleValues_JoinsCorrectly()
    {
        // Arrange
        var separator = new Content(", ");
        ReadOnlySpan<Content> values = [
            new Content("Hello"),
            new Content("World"),
            new Content("Test")
        ];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("Hello, World, Test", result.ToString());
        Assert.Equal(18, result.Length);
    }

    [Fact]
    public void Join_WithContentArray_EmptySeparator_JoinsWithoutSeparator()
    {
        // Arrange
        var separator = Content.Empty;
        ReadOnlySpan<Content> values = [
            new Content("Hello"),
            new Content("World")
        ];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("HelloWorld", result.ToString());
        Assert.Equal(10, result.Length);
    }

    [Fact]
    public void Join_WithContentArray_SkipsEmptyValues()
    {
        // Arrange
        var separator = new Content(", ");
        ReadOnlySpan<Content> values = [
            new Content("Hello"),
            Content.Empty,
            new Content("World"),
            Content.Empty,
            new Content("Test")
        ];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("Hello, World, Test", result.ToString());
        Assert.Equal(18, result.Length);
    }

    [Fact]
    public void Join_WithContentArray_AllEmptyValues_ReturnsEmpty()
    {
        // Arrange
        var separator = new Content(", ");
        ReadOnlySpan<Content> values = [Content.Empty, Content.Empty, Content.Empty];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.True(result.IsEmpty);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void Join_WithImmutableArrayContent_JoinsCorrectly()
    {
        // Arrange
        var separator = new Content(" | ");
        ImmutableArray<Content> values = [
            new Content("A"),
            new Content("B"),
            new Content("C")
        ];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("A | B | C", result.ToString());
        Assert.Equal(9, result.Length);
    }

    [Fact]
    public void Join_WithMemoryArray_EmptyValues_ReturnsEmpty()
    {
        // Arrange
        var separator = new Content(", ");
        ReadOnlySpan<ReadOnlyMemory<char>> values = [];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.True(result.IsEmpty);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void Join_WithMemoryArray_SingleValue_ReturnsSingleValue()
    {
        // Arrange
        var separator = new Content(", ");
        ReadOnlySpan<ReadOnlyMemory<char>> values = ["Hello".AsMemory()];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("Hello", result.ToString());
        Assert.Equal(5, result.Length);
    }

    [Fact]
    public void Join_WithMemoryArray_MultipleValues_JoinsCorrectly()
    {
        // Arrange
        var separator = new Content(" - ");
        ReadOnlySpan<ReadOnlyMemory<char>> values = [
            "Alpha".AsMemory(),
            "Beta".AsMemory(),
            "Gamma".AsMemory()
        ];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("Alpha - Beta - Gamma", result.ToString());
        Assert.Equal(20, result.Length);
    }

    [Fact]
    public void Join_WithMemoryArray_SkipsEmptyValues()
    {
        // Arrange
        var separator = new Content(", ");
        ReadOnlySpan<ReadOnlyMemory<char>> values = [
            "Hello".AsMemory(),
            ReadOnlyMemory<char>.Empty,
            "World".AsMemory()
        ];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("Hello, World", result.ToString());
        Assert.Equal(12, result.Length);
    }

    [Fact]
    public void Join_WithImmutableArrayMemory_JoinsCorrectly()
    {
        // Arrange
        var separator = new Content(" & ");
        ImmutableArray<ReadOnlyMemory<char>> values = [
            "First".AsMemory(),
            "Second".AsMemory(),
            "Third".AsMemory()
        ];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("First & Second & Third", result.ToString());
        Assert.Equal(22, result.Length);
    }

    [Fact]
    public void Join_WithStringArray_EmptyValues_ReturnsEmpty()
    {
        // Arrange
        var separator = new Content(", ");
        ReadOnlySpan<string> values = [];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.True(result.IsEmpty);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void Join_WithStringArray_SingleValue_ReturnsSingleValue()
    {
        // Arrange
        var separator = new Content(", ");
        ReadOnlySpan<string> values = ["Single"];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("Single", result.ToString());
        Assert.Equal(6, result.Length);
    }

    [Fact]
    public void Join_WithStringArray_MultipleValues_JoinsCorrectly()
    {
        // Arrange
        var separator = new Content(" + ");
        ReadOnlySpan<string> values = ["One", "Two", "Three"];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("One + Two + Three", result.ToString());
        Assert.Equal(17, result.Length);
    }

    [Fact]
    public void Join_WithStringArray_SkipsNullAndEmptyValues()
    {
        // Arrange
        var separator = new Content(", ");
        ReadOnlySpan<string> values = ["Hello", null!, "", "World", null!];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("Hello, World", result.ToString());
        Assert.Equal(12, result.Length);
    }

    [Fact]
    public void Join_WithImmutableArrayString_JoinsCorrectly()
    {
        // Arrange
        var separator = new Content(" -> ");
        ImmutableArray<string> values = ["Start", "Middle", "End"];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("Start -> Middle -> End", result.ToString());
        Assert.Equal(22, result.Length);
    }

    [Fact]
    public void Join_WithEnumerableContent_EmptyEnumerable_ReturnsEmpty()
    {
        // Arrange
        var separator = new Content(", ");
        IEnumerable<Content> values = [];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.True(result.IsEmpty);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void Join_WithEnumerableContent_SingleValue_ReturnsSingleValue()
    {
        // Arrange
        var separator = new Content(", ");
        IEnumerable<Content> values = [new Content("OnlyOne")];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("OnlyOne", result.ToString());
        Assert.Equal(7, result.Length);
    }

    [Fact]
    public void Join_WithEnumerableContent_MultipleValues_JoinsCorrectly()
    {
        // Arrange
        var separator = new Content(" | ");
        IEnumerable<Content> values = ["Item1", "Item2", "Item3" ];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("Item1 | Item2 | Item3", result.ToString());
        Assert.Equal(21, result.Length);
    }

    [Fact]
    public void Join_WithEnumerableContent_SkipsEmptyValues()
    {
        // Arrange
        var separator = new Content(", ");
        IEnumerable<Content> values = ["First", Content.Empty, "Second", Content.Empty];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("First, Second", result.ToString());
        Assert.Equal(13, result.Length);
    }

    [Fact]
    public void Join_WithEnumerableMemory_EmptyEnumerable_ReturnsEmpty()
    {
        // Arrange
        var separator = new Content(", ");
        IEnumerable<ReadOnlyMemory<char>> values = [];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.True(result.IsEmpty);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void Join_WithEnumerableMemory_SingleValue_ReturnsSingleValue()
    {
        // Arrange
        var separator = new Content(", ");
        IEnumerable<ReadOnlyMemory<char>> values = ["Solo".AsMemory()];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("Solo", result.ToString());
        Assert.Equal(4, result.Length);
    }

    [Fact]
    public void Join_WithEnumerableMemory_MultipleValues_JoinsCorrectly()
    {
        // Arrange
        var separator = new Content(" :: ");
        IEnumerable<ReadOnlyMemory<char>> values = [
            "Alpha".AsMemory(),
            "Bravo".AsMemory(),
            "Charlie".AsMemory()
        ];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("Alpha :: Bravo :: Charlie", result.ToString());
        Assert.Equal(25, result.Length);
    }

    [Fact]
    public void Join_WithEnumerableMemory_SkipsEmptyValues()
    {
        // Arrange
        var separator = new Content(" - ");
        IEnumerable<ReadOnlyMemory<char>> values = [
            "Valid".AsMemory(),
            ReadOnlyMemory<char>.Empty,
            "Content".AsMemory()
        ];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("Valid - Content", result.ToString());
        Assert.Equal(15, result.Length);
    }

    [Fact]
    public void Join_WithEnumerableString_EmptyEnumerable_ReturnsEmpty()
    {
        // Arrange
        var separator = new Content(", ");
        IEnumerable<string> values = [];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.True(result.IsEmpty);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void Join_WithEnumerableString_SingleValue_ReturnsSingleValue()
    {
        // Arrange
        var separator = new Content(", ");
        IEnumerable<string> values = ["Unique"];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("Unique", result.ToString());
        Assert.Equal(6, result.Length);
    }

    [Fact]
    public void Join_WithEnumerableString_MultipleValues_JoinsCorrectly()
    {
        // Arrange
        var separator = new Content(" <-> ");
        IEnumerable<string> values = ["Left", "Center", "Right"];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("Left <-> Center <-> Right", result.ToString());
        Assert.Equal(25, result.Length);
    }

    [Fact]
    public void Join_WithEnumerableString_SkipsNullAndEmptyValues()
    {
        // Arrange
        var separator = new Content(", ");
        IEnumerable<string> values = ["Valid", null!, "", "Content"];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("Valid, Content", result.ToString());
        Assert.Equal(14, result.Length);
    }

    [Fact]
    public void Join_WithComplexSeparator_JoinsCorrectly()
    {
        // Arrange
        var separator = new Content([" >>> ", "SEPARATOR", " <<< "]);
        ReadOnlySpan<Content> values = [
            new Content("First"),
            new Content("Second")
        ];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("First >>> SEPARATOR <<< Second", result.ToString());
    }

    [Fact]
    public void Join_WithNestedContentValues_JoinsCorrectly()
    {
        // Arrange
        var separator = new Content(" | ");
        var nestedContent1 = new Content(["Nested", "1"]);
        var nestedContent2 = new Content(["Nested", "2"]);
        ReadOnlySpan<Content> values = [nestedContent1, nestedContent2];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("Nested1 | Nested2", result.ToString());
    }

    [Fact]
    public void Join_WithUnicodeCharacters_JoinsCorrectly()
    {
        // Arrange
        var separator = new Content(" 🔗 ");
        ReadOnlySpan<Content> values = [
            new Content("Hello"),
            new Content("世界"),
            new Content("🌍")
        ];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("Hello 🔗 世界 🔗 🌍", result.ToString());
    }

    [Fact]
    public void Join_WithSpecialCharacters_JoinsCorrectly()
    {
        // Arrange
        var separator = new Content("\n");
        ReadOnlySpan<string> values = ["Line1", "Line2", "Line3"];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("Line1\nLine2\nLine3", result.ToString());
        Assert.Contains('\n', result.ToString());
    }

    [Fact]
    public void Join_WithWhitespaceValues_JoinsCorrectly()
    {
        // Arrange
        var separator = new Content("|");
        ReadOnlySpan<string> values = ["   ", "\t", " \n "];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("   |\t| \n ", result.ToString());
    }

    [Fact]
    public void Join_LargeNumberOfValues_JoinsCorrectly()
    {
        // Arrange
        var separator = new Content(",");
        var values = new Content[100];
        for (var i = 0; i < 100; i++)
        {
            values[i] = new Content($"Item{i}");
        }

        // Act
        var result = Content.Join(separator, values.AsSpan());

        // Assert
        Assert.True(result.IsMultiPart);
        var expected = string.Join(",", values.Select(v => v.ToString()));
        Assert.Equal(expected, result.ToString());
    }

    [Fact]
    public void Join_WithSingleCharacterSeparator_JoinsCorrectly()
    {
        // Arrange
        var separator = new Content(",");
        ReadOnlySpan<string> values = ["A", "B", "C", "D"];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("A,B,C,D", result.ToString());
        Assert.Equal(7, result.Length);
    }

    [Fact]
    public void Join_WithEmptyStringsInArray_SkipsEmpty()
    {
        // Arrange
        var separator = new Content(" | ");
        ReadOnlySpan<string> values = ["", "Valid", "", "Content", ""];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("Valid | Content", result.ToString());
    }

    [Fact]
    public void Join_ResultEquality_WorksCorrectly()
    {
        // Arrange
        var separator = new Content(", ");
        ReadOnlySpan<Content> values1 = [new Content("A"), new Content("B")];
        ReadOnlySpan<string> values2 = ["A", "B"];

        // Act
        var result1 = Content.Join(separator, values1);
        var result2 = Content.Join(separator, values2);

        // Assert
        Assert.Equal(result1, result2);
        Assert.Equal(result1.GetHashCode(), result2.GetHashCode());
    }

    [Fact]
    public void Join_ResultHashCode_ConsistentWithEquality()
    {
        // Arrange
        var separator = new Content(" - ");
        ReadOnlySpan<Content> values = [new Content("X"), new Content("Y")];

        // Act
        var result1 = Content.Join(separator, values);
        var result2 = Content.Join(separator, values);

        // Assert
        Assert.Equal(result1, result2);
        Assert.Equal(result1.GetHashCode(), result2.GetHashCode());
    }

    [Fact]
    public void Join_PreservesOriginalValues()
    {
        // Arrange
        var separator = new Content(", ");
        var value1 = new Content("Hello");
        var value2 = new Content("World");
        ReadOnlySpan<Content> values = [value1, value2];

        // Act
        var _ = Content.Join(separator, values);

        // Assert - Original values should be unchanged
        Assert.Equal("Hello", value1.ToString());
        Assert.Equal("World", value2.ToString());
        Assert.Equal(", ", separator.ToString());
    }

    [Fact]
    public void Join_WithVeryLongSeparator_JoinsCorrectly()
    {
        // Arrange
        var longSeparator = new string('=', 50);
        var separator = new Content(longSeparator);
        ReadOnlySpan<string> values = ["Start", "End"];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        var expected = $"Start{longSeparator}End";
        Assert.Equal(expected, result.ToString());
        Assert.Equal(expected.Length, result.Length);
    }

    [Fact]
    public void Join_WithVeryLongValues_JoinsCorrectly()
    {
        // Arrange
        var separator = new Content(" | ");
        var longValue1 = new string('A', 1000);
        var longValue2 = new string('B', 1000);
        ReadOnlySpan<string> values = [longValue1, longValue2];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        var expected = $"{longValue1} | {longValue2}";
        Assert.Equal(expected, result.ToString());
        Assert.Equal(expected.Length, result.Length);
    }

    [Fact]
    public void Join_MixedEnumerableTypes_ProduceSameResults()
    {
        // Arrange
        var separator = new Content(" & ");

        // Different ways to represent the same values
        ReadOnlySpan<Content> contentValues = [new Content("X"), new Content("Y"), new Content("Z")];
        ReadOnlySpan<string> stringValues = ["X", "Y", "Z"];
        ReadOnlySpan<ReadOnlyMemory<char>> memoryValues = ["X".AsMemory(), "Y".AsMemory(), "Z".AsMemory()];

        // Act
        var result1 = Content.Join(separator, contentValues);
        var result2 = Content.Join(separator, stringValues);
        var result3 = Content.Join(separator, memoryValues);

        // Assert
        Assert.Equal(result1, result2);
        Assert.Equal(result2, result3);
        Assert.Equal("X & Y & Z", result1.ToString());
    }

    [Fact]
    public void Join_EnumerableVsSpan_ProduceSameResults()
    {
        // Arrange
        var separator = new Content(" -> ");
        var values = new[] { "First", "Second", "Third" };

        // Act
        var spanResult = Content.Join(separator, values.AsSpan());
        var enumerableResult = Content.Join(separator, (IEnumerable<string>)values);

        // Assert
        Assert.Equal(spanResult, enumerableResult);
        Assert.Equal("First -> Second -> Third", spanResult.ToString());
        Assert.Equal("First -> Second -> Third", enumerableResult.ToString());
    }

    [Fact]
    public void Join_PerformanceTest_ManyValues()
    {
        // Arrange
        var separator = new Content(",");
        var values = new string[1000];
        for (var i = 0; i < 1000; i++)
        {
            values[i] = $"Value{i}";
        }

        // Act
        var result = Content.Join(separator, values.AsSpan());

        // Assert
        Assert.True(result.IsMultiPart);
        var expected = string.Join(",", values);
        Assert.Equal(expected, result.ToString());
    }

    [Fact]
    public void Join_EdgeCase_OnlyEmptyValuesWithSeparator()
    {
        // Arrange
        var separator = new Content(" SEPARATOR ");
        ReadOnlySpan<Content> values = [Content.Empty, Content.Empty, Content.Empty];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.True(result.IsEmpty);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void Join_EdgeCase_SingleNonEmptyAmongEmpty()
    {
        // Arrange
        var separator = new Content(" | ");
        ReadOnlySpan<Content> values = [Content.Empty, new Content("OnlyOne"), Content.Empty];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("OnlyOne", result.ToString());
        Assert.Equal(7, result.Length);
    }

    [Fact]
    public void Join_EdgeCase_EmptyValuesAtBeginningAndEnd()
    {
        // Arrange
        var separator = new Content(", ");
        ReadOnlySpan<string> values = ["", "Middle1", "Middle2", ""];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("Middle1, Middle2", result.ToString());
    }

    [Fact]
    public void Join_EdgeCase_AlternatingEmptyAndNonEmpty()
    {
        // Arrange
        var separator = new Content(" | ");
        ReadOnlySpan<string> values = ["A", "", "B", "", "C"];

        // Act
        var result = Content.Join(separator, values);

        // Assert
        Assert.Equal("A | B | C", result.ToString());
    }
}
