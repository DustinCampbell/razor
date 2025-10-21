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
    }

    [Fact]
    public void Contains_SingleValue_CharacterExists_ReturnsTrue()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.True(content.Contains('H'));
        Assert.True(content.Contains('o'));
        Assert.True(content.Contains(' '));
        Assert.True(content.Contains('d'));
    }

    [Fact]
    public void Contains_SingleValue_CharacterNotExists_ReturnsFalse()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.False(content.Contains('x'));
        Assert.False(content.Contains('Z'));
    }

    [Fact]
    public void Contains_MultiPart_CharacterExists_ReturnsTrue()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act & Assert
        Assert.True(content.Contains('H'));
        Assert.True(content.Contains(' '));
        Assert.True(content.Contains('W'));
    }

    [Fact]
    public void Contains_MultiPart_CharacterNotExists_ReturnsFalse()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act & Assert
        Assert.False(content.Contains('x'));
    }

    [Fact]
    public void Contains_Substring_EmptyValue_ReturnsTrue()
    {
        // Arrange
        var content = new Content("Hello");

        // Act & Assert
        Assert.True(content.Contains("".AsSpan(), StringComparison.Ordinal));
    }

    [Fact]
    public void Contains_Substring_SingleValue_Exists_ReturnsTrue()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.True(content.Contains("Hello".AsSpan(), StringComparison.Ordinal));
        Assert.True(content.Contains("World".AsSpan(), StringComparison.Ordinal));
        Assert.True(content.Contains("lo Wo".AsSpan(), StringComparison.Ordinal));
    }

    [Fact]
    public void Contains_Substring_SingleValue_NotExists_ReturnsFalse()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.False(content.Contains("xyz".AsSpan(), StringComparison.Ordinal));
    }

    [Fact]
    public void Contains_Substring_MultiPart_SpansMultipleParts_ReturnsTrue()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act & Assert
        Assert.True(content.Contains("lo Wo".AsSpan(), StringComparison.Ordinal));
        Assert.True(content.Contains("Hello World".AsSpan(), StringComparison.Ordinal));
    }

    [Fact]
    public void Contains_Substring_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.True(content.Contains("hello".AsSpan(), StringComparison.OrdinalIgnoreCase));
        Assert.True(content.Contains("WORLD".AsSpan(), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Contains_Substring_LongerThanContent_ReturnsFalse()
    {
        // Arrange
        var content = new Content("Hi");

        // Act & Assert
        Assert.False(content.Contains("Hello".AsSpan(), StringComparison.Ordinal));
    }

    [Fact]
    public void ContainsAny_TwoChars_EmptyContent_ReturnsFalse()
    {
        // Arrange
        var content = Content.Empty;

        // Act & Assert
        Assert.False(content.ContainsAny('a', 'b'));
    }

    [Fact]
    public void ContainsAny_TwoChars_SingleValue_Found_ReturnsTrue()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.True(content.ContainsAny('H', 'x'));
        Assert.True(content.ContainsAny('x', 'd'));
        Assert.True(content.ContainsAny('o', 'x'));
    }

    [Fact]
    public void ContainsAny_TwoChars_SingleValue_NotFound_ReturnsFalse()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.False(content.ContainsAny('x', 'z'));
    }

    [Fact]
    public void ContainsAny_ThreeChars_SingleValue_Found_ReturnsTrue()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.True(content.ContainsAny('H', 'x', 'y'));
        Assert.True(content.ContainsAny('x', 'y', 'd'));
    }

    [Fact]
    public void ContainsAny_ThreeChars_SingleValue_NotFound_ReturnsFalse()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.False(content.ContainsAny('x', 'y', 'z'));
    }

    [Fact]
    public void ContainsAny_Span_SingleValue_Found_ReturnsTrue()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.True(content.ContainsAny("Hxyz".AsSpan()));
        Assert.True(content.ContainsAny("xyz ".AsSpan()));
    }

    [Fact]
    public void ContainsAny_Span_SingleValue_NotFound_ReturnsFalse()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.False(content.ContainsAny("xyz".AsSpan()));
    }

    [Fact]
    public void ContainsAny_Span_EmptySpan_ReturnsFalse()
    {
        // Arrange
        var content = new Content("Hello");

        // Act & Assert
        Assert.False(content.ContainsAny([]));
    }

    [Fact]
    public void ContainsAny_MultiPart_Found_ReturnsTrue()
    {
        // Arrange
        var content = new Content(["Hello", " ", "World"]);

        // Act & Assert
        Assert.True(content.ContainsAny('H', 'x'));
        Assert.True(content.ContainsAny('x', 'W', 'y'));
        Assert.True(content.ContainsAny("Hxyz".AsSpan()));
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
        Assert.True(content.ContainsAny('x', 'y', 'D'));
    }

    [Fact]
    public void ContainsAnyExcept_SingleChar_EmptyContent_ReturnsFalse()
    {
        // Arrange
        var content = Content.Empty;

        // Act & Assert
        Assert.False(content.ContainsAnyExcept('a'));
    }

    [Fact]
    public void ContainsAnyExcept_SingleChar_OnlyContainsExcluded_ReturnsFalse()
    {
        // Arrange
        var content = new Content("aaaa");

        // Act & Assert
        Assert.False(content.ContainsAnyExcept('a'));
    }

    [Fact]
    public void ContainsAnyExcept_SingleChar_ContainsOther_ReturnsTrue()
    {
        // Arrange
        var content = new Content("aaabaa");

        // Act & Assert
        Assert.True(content.ContainsAnyExcept('a'));
    }

    [Fact]
    public void ContainsAnyExcept_TwoChars_OnlyContainsExcluded_ReturnsFalse()
    {
        // Arrange
        var content = new Content("ababab");

        // Act & Assert
        Assert.False(content.ContainsAnyExcept('a', 'b'));
    }

    [Fact]
    public void ContainsAnyExcept_TwoChars_ContainsOther_ReturnsTrue()
    {
        // Arrange
        var content = new Content("ababcab");

        // Act & Assert
        Assert.True(content.ContainsAnyExcept('a', 'b'));
    }

    [Fact]
    public void ContainsAnyExcept_ThreeChars_OnlyContainsExcluded_ReturnsFalse()
    {
        // Arrange
        var content = new Content("abcabc");

        // Act & Assert
        Assert.False(content.ContainsAnyExcept('a', 'b', 'c'));
    }

    [Fact]
    public void ContainsAnyExcept_ThreeChars_ContainsOther_ReturnsTrue()
    {
        // Arrange
        var content = new Content("abcdabc");

        // Act & Assert
        Assert.True(content.ContainsAnyExcept('a', 'b', 'c'));
    }

    [Fact]
    public void ContainsAnyExcept_Span_OnlyContainsExcluded_ReturnsFalse()
    {
        // Arrange
        var content = new Content("abcabc");

        // Act & Assert
        Assert.False(content.ContainsAnyExcept("abc".AsSpan()));
    }

    [Fact]
    public void ContainsAnyExcept_Span_ContainsOther_ReturnsTrue()
    {
        // Arrange
        var content = new Content("abcdabc");

        // Act & Assert
        Assert.True(content.ContainsAnyExcept("abc".AsSpan()));
    }

    [Fact]
    public void ContainsAnyExcept_Span_EmptySpan_ReturnsTrue()
    {
        // Arrange
        var content = new Content("Hello");

        // Act & Assert
        Assert.True(content.ContainsAnyExcept([]));
    }

    [Fact]
    public void ContainsAnyExcept_MultiPart_OnlyContainsExcluded_ReturnsFalse()
    {
        // Arrange
        var content = new Content(["aaa", "bbb", "ccc"]);

        // Act & Assert
        Assert.False(content.ContainsAnyExcept("abc".AsSpan()));
    }

    [Fact]
    public void ContainsAnyExcept_MultiPart_ContainsOther_ReturnsTrue()
    {
        // Arrange
        var content = new Content(["aaa", "bbb", "ddd"]);

        // Act & Assert
        Assert.True(content.ContainsAnyExcept("abc".AsSpan()));
    }

    [Fact]
    public void ContainsAnyExcept_Whitespace_ReturnsCorrectly()
    {
        // Arrange
        var content1 = new Content("   \t\n");
        var content2 = new Content("  a  ");

        // Act & Assert
        Assert.False(content1.ContainsAnyExcept(' ', '\t', '\n'));
        Assert.True(content2.ContainsAnyExcept(' ', '\t', '\n'));
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
        
        var content2 = new Content([inner1, new Content("xyz")]);
        Assert.True(content2.ContainsAnyExcept("ab".AsSpan()));
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
}
