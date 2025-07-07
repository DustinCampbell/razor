// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public class ContentTests
{
    private static string GetWrittenString(Content content)
    {
        using var writer = new CodeWriter();
        content.WriteTo(writer);

        return writer.GetText().ToString();
    }

    [Fact]
    public void EmptyContent_IsEmpty()
    {
        var content = Content.Empty;

        Assert.True(content.IsEmpty);
        Assert.Equal(string.Empty, GetWrittenString(content));
    }

    [Fact]
    public void Content_FromString_WritesCorrectly()
    {
        var content = new Content("hello");

        Assert.False(content.IsEmpty);
        Assert.Equal("hello", GetWrittenString(content));
    }

    [Fact]
    public void Content_FromReadOnlyMemory_WritesCorrectly()
    {
        var memory = "world".AsMemory();
        var content = new Content(memory);

        Assert.False(content.IsEmpty);
        Assert.Equal("world", GetWrittenString(content));
    }

    [Fact]
    public void Content_FromImmutableArrayOfContent_WritesAllParts()
    {
        ImmutableArray<Content> parts = [new Content("a"), new Content("b"), new Content("c")];
        var content = new Content(parts);

        Assert.False(content.IsEmpty);
        Assert.Equal("abc", GetWrittenString(content));
    }

    [Fact]
    public void Content_FromImmutableArrayOfContent_WithMultipleTypes_WritesAllParts()
    {
        ImmutableArray<Content> parts = [
            new Content("A"),
            new Content("B".AsMemory()),
            new Content(["C"]),
            new Content(["D", "E", "F"]),
            new Content(["G".AsMemory()]),
            new Content(["H".AsMemory(), "I".AsMemory(), "J".AsMemory()])
        ];

        var content = new Content(parts);

        Assert.Equal("ABCDEFGHIJ", GetWrittenString(content));
    }

    [Fact]
    public void Content_FromImmutableArrayOfReadOnlyMemory_WritesAllParts()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts = ["x".AsMemory(), "y".AsMemory(), "z".AsMemory()];
        var content = new Content(parts);

        Assert.False(content.IsEmpty);
        Assert.Equal("xyz", GetWrittenString(content));
    }

    [Fact]
    public void Content_FromImmutableArrayOfString_WritesAllParts()
    {
        ImmutableArray<string> parts = ["foo", "bar", "baz"];
        var content = new Content(parts);

        Assert.False(content.IsEmpty);
        Assert.Equal("foobarbaz", GetWrittenString(content));
    }

    [Fact]
    public void Content_ImplicitConversion_String()
    {
        Content content = "implicit";

        Assert.Equal("implicit", GetWrittenString(content));
    }

    [Fact]
    public void Content_ImplicitConversion_ReadOnlyMemory()
    {
        Content content = "memory".AsMemory();

        Assert.Equal("memory", GetWrittenString(content));
    }

    [Fact]
    public void Content_ImplicitConversion_ImmutableArrayOfContent()
    {
        ImmutableArray<Content> array = [new Content("1"), new Content("2")];
        Content content = array;

        Assert.Equal("12", GetWrittenString(content));
    }

    [Fact]
    public void Content_ImplicitConversion_ImmutableArrayOfReadOnlyMemory()
    {
        ImmutableArray<ReadOnlyMemory<char>> array = ["a".AsMemory(), "b".AsMemory()];
        Content content = array;

        Assert.Equal("ab", GetWrittenString(content));
    }

    [Fact]
    public void Content_ImplicitConversion_ImmutableArrayOfString()
    {
        ImmutableArray<string> array = ["x", "y"];
        Content content = array;

        Assert.Equal("xy", GetWrittenString(content));
    }

    [Fact]
    public void ContentInterpolatedStringHandler_AppendsLiteralsAndFormatted()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts = ["Hello".AsMemory(), "World".AsMemory()];
        var extraContent = new Content("abc");

        var content = new Content($"foo{"bar"} {parts}! {extraContent}");

        Assert.Equal("foobar HelloWorld! abc", GetWrittenString(content));
    }

    [Fact]
    public void ContentInterpolatedStringHandler_AppendsNullFormatted_Ignored()
    {
        var content = new Content($"{(string)null!}");

        Assert.Equal(string.Empty, GetWrittenString(content));
    }

    [Fact]
    public void Content_IsEmpty_WhenPartsEmpty()
    {
        var array = ImmutableArray<Content>.Empty;
        var content = new Content(array);

        Assert.True(content.IsEmpty);
        Assert.Equal(string.Empty, GetWrittenString(content));
    }

    [Fact]
    public void Enumerator_SingleString_EnumeratesAllChars()
    {
        var content = new Content("abc");
        var chars = new List<char>();

        foreach (var ch in content)
        {
            chars.Add(ch);
        }

        Assert.Equal("abc".ToCharArray(), chars);
    }

    [Fact]
    public void Enumerator_SingleReadOnlyMemory_EnumeratesAllChars()
    {
        var content = new Content("xyz".AsMemory());

        var chars = new List<char>();
        foreach (var ch in content)
        {
            chars.Add(ch);
        }

        Assert.Equal("xyz".ToCharArray(), chars);
    }

    [Fact]
    public void Enumerator_ImmutableArrayOfString_EnumeratesAllChars()
    {
        ImmutableArray<string> parts = ["foo", "bar"];
        var content = new Content(parts);
        var chars = new List<char>();

        foreach (var ch in content)
        {
            chars.Add(ch);
        }

        Assert.Equal("foobar".ToCharArray(), chars);
    }

    [Fact]
    public void Enumerator_ImmutableArrayOfReadOnlyMemory_EnumeratesAllChars()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts = ["hi".AsMemory(), "bye".AsMemory()];
        var content = new Content(parts);
        var chars = new List<char>();

        foreach (var ch in content)
        {
            chars.Add(ch);
        }

        Assert.Equal("hibye".ToCharArray(), chars);
    }

    [Fact]
    public void Enumerator_ImmutableArrayOfContent_EnumeratesAllChars()
    {
        ImmutableArray<Content> parts = [new Content("1"), new Content("2"), new Content("3")];
        var content = new Content(parts);
        var chars = new List<char>();

        foreach (var ch in content)
        {
            chars.Add(ch);
        }

        Assert.Equal("123".ToCharArray(), chars);
    }

    [Fact]
    public void Enumerator_NestedContent_EnumeratesAllChars()
    {
        // Nested: [ "A", ["B", ["C", "D"], ["E", "F"], "G", ["H", ["I"], "J", "K" ]
        var nested = new Content([
            new Content("A"),
            new Content([
                new Content("B"),
                new Content([
                    new Content("C"),
                    new Content("D")
                ]),
                new Content([
                    new Content("E"),
                    new Content("F")
                ])
            ]),
            new Content("G"),
            new Content([
                new Content("H"),
                new Content([
                    new Content("I")
                ]),
                new Content("J"),
                new Content("K")
            ])
        ]);

        var chars = new List<char>();

        foreach (var ch in nested)
        {
            chars.Add(ch);
        }

        Assert.Equal("ABCDEFGHIJK".ToCharArray(), chars);
    }

    [Fact]
    public void Enumerator_DeeplyNestedContent_EnumeratesAllChars()
    {
        // Deeply nested: [ [ [ [ [ [ [ [ [ [ [ [ "X" ] ] ] ] ] ] ] ] ] ] ] ]
        var deep = new Content([
            new Content([
                new Content([
                    new Content([
                        new Content([
                            new Content([
                                new Content([
                                    new Content([
                                        new Content([
                                            new Content([
                                                new Content([
                                                    new Content("X")
                                                    ])
                                            ])
                                        ])
                                    ])
                                ])
                            ])
                        ])
                    ])
                ])
            ])
        ]);

        var chars = new List<char>();

        foreach (var ch in deep)
        {
            chars.Add(ch);
        }

        Assert.Equal("X".ToCharArray(), chars);
    }

    [Fact]
    public void Enumerator_EmptyContent_YieldsNoChars()
    {
        var content = Content.Empty;
        var chars = new List<char>();

        foreach (var ch in content)
        {
            chars.Add(ch);
        }

        Assert.Empty(chars);
    }

    [Fact]
    public void Enumerator_EmptyPartsArray_YieldsNoChars()
    {
        var content = new Content(ImmutableArray<Content>.Empty);
        var chars = new List<char>();

        foreach (var ch in content)
        {
            chars.Add(ch);
        }

        Assert.Empty(chars);
    }

    [Fact]
    public void AllParts_SingleString_ReturnsSinglePart()
    {
        var content = new Content("abc");
        var parts = new List<ReadOnlyMemory<char>>();

        foreach (var part in content.AllParts)
        {
            parts.Add(part);
        }

        Assert.Single(parts);
        Assert.Equal("abc", parts[0].ToString());
    }

    [Fact]
    public void AllParts_SingleReadOnlyMemory_ReturnsSinglePart()
    {
        var content = new Content("xyz".AsMemory());
        var parts = new List<ReadOnlyMemory<char>>();

        foreach (var part in content.AllParts)
        {
            parts.Add(part);
        }

        Assert.Single(parts);
        Assert.Equal("xyz", parts[0].ToString());
    }

    [Fact]
    public void AllParts_ImmutableArrayOfString_ReturnsAllParts()
    {
        ImmutableArray<string> arr = ["foo", "bar"];
        var content = new Content(arr);
        var parts = new List<ReadOnlyMemory<char>>();

        foreach (var part in content.AllParts)
        {
            parts.Add(part);
        }

        Assert.Equal(2, parts.Count);
        Assert.Equal("foo", parts[0].ToString());
        Assert.Equal("bar", parts[1].ToString());
    }

    [Fact]
    public void AllParts_ImmutableArrayOfReadOnlyMemory_ReturnsAllParts()
    {
        ImmutableArray<ReadOnlyMemory<char>> arr = ["hi".AsMemory(), "bye".AsMemory()];
        var content = new Content(arr);
        var parts = new List<ReadOnlyMemory<char>>();

        foreach (var part in content.AllParts)
        {
            parts.Add(part);
        }

        Assert.Equal(2, parts.Count);
        Assert.Equal("hi", parts[0].ToString());
        Assert.Equal("bye", parts[1].ToString());
    }

    [Fact]
    public void AllParts_ImmutableArrayOfContent_ReturnsAllPartsFlattened()
    {
        ImmutableArray<Content> arr = [
            new Content("A"),
            new Content("B".AsMemory()),
            new Content(["C", "D"]),
            new Content(ImmutableArray<string>.Empty)
        ];

        var content = new Content(arr);
        var parts = new List<ReadOnlyMemory<char>>();

        foreach (var part in content.AllParts)
        {
            parts.Add(part);
        }

        Assert.Equal(4, parts.Count);
        Assert.Equal("A", parts[0].ToString());
        Assert.Equal("B", parts[1].ToString());
        Assert.Equal("C", parts[2].ToString());
        Assert.Equal("D", parts[3].ToString());
    }

    [Fact]
    public void AllParts_NestedContent_ReturnsAllLeafParts()
    {
        var nested = new Content([
            new Content("A"),
            new Content([
                new Content("B"),
                new Content([
                    new Content("C"),
                    new Content("D")
                ])
            ]),
            new Content("E")
        ]);

        var parts = new List<ReadOnlyMemory<char>>();
        foreach (var part in nested.AllParts)
        {
            parts.Add(part);
        }

        Assert.Equal(5, parts.Count);
        Assert.Equal("A", parts[0].ToString());
        Assert.Equal("B", parts[1].ToString());
        Assert.Equal("C", parts[2].ToString());
        Assert.Equal("D", parts[3].ToString());
        Assert.Equal("E", parts[4].ToString());
    }

    [Fact]
    public void AllParts_EmptyContent_YieldsNoParts()
    {
        var content = Content.Empty;
        var parts = new List<ReadOnlyMemory<char>>();

        foreach (var part in content.AllParts)
        {
            parts.Add(part);
        }

        Assert.Empty(parts);
    }

    [Fact]
    public void AllParts_EmptyPartsArray_YieldsNoParts()
    {
        var content = new Content(ImmutableArray<Content>.Empty);
        var parts = new List<ReadOnlyMemory<char>>();

        foreach (var part in content.AllParts)
        {
            parts.Add(part);
        }

        Assert.Empty(parts);
    }

    [Fact]
    public void ToString_SingleString_ReturnsString()
    {
        var content = new Content("hello world");

        var result = content.ToString();

        Assert.Equal("hello world", result);
    }

    [Fact]
    public void ToString_SingleReadOnlyMemory_ReturnsString()
    {
        var content = new Content("memory test".AsMemory());

        var result = content.ToString();

        Assert.Equal("memory test", result);
    }

    [Fact]
    public void ToString_ImmutableArrayOfStrings_ReturnsConcatenatedString()
    {
        ImmutableArray<string> parts = ["foo", "bar", "baz"];
        var content = new Content(parts);

        var result = content.ToString();

        Assert.Equal("foobarbaz", result);
    }

    [Fact]
    public void ToString_ImmutableArrayOfReadOnlyMemory_ReturnsConcatenatedString()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts = ["abc".AsMemory(), "def".AsMemory(), "ghi".AsMemory()];
        var content = new Content(parts);

        var result = content.ToString();

        Assert.Equal("abcdefghi", result);
    }

    [Fact]
    public void ToString_ImmutableArrayOfContent_ReturnsConcatenatedString()
    {
        ImmutableArray<Content> parts = [
            new Content("A"),
            new Content("B".AsMemory()),
            new Content(["C", "D"]),
            new Content(["E".AsMemory(), "F".AsMemory()])
        ];

        var content = new Content(parts);

        var result = content.ToString();

        Assert.Equal("ABCDEF", result);
    }

    [Fact]
    public void ToString_EmptyContent_ReturnsEmptyString()
    {
        var content = Content.Empty;

        var result = content.ToString();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToString_EmptyStringArray_ReturnsEmptyString()
    {
        var content = new Content(ImmutableArray<string>.Empty);

        var result = content.ToString();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToString_EmptyMemoryArray_ReturnsEmptyString()
    {
        var content = new Content(ImmutableArray<ReadOnlyMemory<char>>.Empty);

        var result = content.ToString();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToString_EmptyContentArray_ReturnsEmptyString()
    {
        var content = new Content(ImmutableArray<Content>.Empty);

        var result = content.ToString();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToString_StringArrayWithNulls_SkipsNulls()
    {
        ImmutableArray<string> parts = ["start", null!, "middle", null!, "end"];
        var content = new Content(parts);

        var result = content.ToString();

        Assert.Equal("startmiddleend", result);
    }

    [Fact]
    public void ToString_StringArrayWithEmptyStrings_IncludesEmptyStrings()
    {
        ImmutableArray<string> parts = ["A", "", "B", "", "C"];
        var content = new Content(parts);

        var result = content.ToString();

        Assert.Equal("ABC", result);
    }

    [Fact]
    public void ToString_MemoryArrayWithEmptyMemories_SkipsEmptyMemories()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts = [
            "start".AsMemory(),
            ReadOnlyMemory<char>.Empty,
            "middle".AsMemory(),
            ReadOnlyMemory<char>.Empty,
            "end".AsMemory()
        ];
        var content = new Content(parts);

        var result = content.ToString();

        Assert.Equal("startmiddleend", result);
    }

    [Fact]
    public void ToString_DeeplyNestedContent_ReturnsFlattened()
    {
        var nested = new Content([
            new Content("1"),
            new Content([
                new Content("2"),
                new Content([
                    new Content("3"),
                    new Content([
                        new Content("4")
                    ])
                ])
            ]),
            new Content("5")
        ]);

        var result = nested.ToString();

        Assert.Equal("12345", result);
    }

    [Fact]
    public void ToString_MixedContentTypes_ReturnsCorrectResult()
    {
        ImmutableArray<Content> parts = [
            new Content("String"),
            new Content("Memory".AsMemory()),
            new Content(["Array", "Of", "Strings"]),
            new Content(["Array".AsMemory(), "Of".AsMemory(), "Memory".AsMemory()]),
            new Content([new Content("Nested"), new Content("Content")])
        ];

        var content = new Content(parts);

        var result = content.ToString();

        Assert.Equal("StringMemoryArrayOfStringsArrayOfMemoryNestedContent", result);
    }

    [Fact]
    public void ToString_LargeContent_HandlesCorrectly()
    {
        var largeParts = new string[1000];

        for (var i = 0; i < 1000; i++)
        {
            largeParts[i] = i.ToString();
        }

        var content = new Content(largeParts.ToImmutableArray());

        var result = content.ToString();

        var expected = string.Concat(largeParts);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToString_SpecialCharacters_PreservesCharacters()
    {
        ImmutableArray<string> parts = ["Hello\n", "World\t", "With\r\n", "Special\"Chars"];
        var content = new Content(parts);

        var result = content.ToString();

        Assert.Equal("Hello\nWorld\tWith\r\nSpecial\"Chars", result);
    }

    [Fact]
    public void Length_EmptyContent_ReturnsZero()
    {
        var content = Content.Empty;

        Assert.Equal(0, content.Length);
    }

    [Fact]
    public void Length_SingleString_ReturnsStringLength()
    {
        var content = new Content("hello");

        Assert.Equal(5, content.Length);
    }

    [Fact]
    public void Length_SingleReadOnlyMemory_ReturnsMemoryLength()
    {
        var content = new Content("world".AsMemory());

        Assert.Equal(5, content.Length);
    }

    [Fact]
    public void Length_EmptyString_ReturnsZero()
    {
        var content = new Content("");

        Assert.Equal(0, content.Length);
    }

    [Fact]
    public void Length_EmptyReadOnlyMemory_ReturnsZero()
    {
        var content = new Content(ReadOnlyMemory<char>.Empty);

        Assert.Equal(0, content.Length);
    }

    [Fact]
    public void Length_ImmutableArrayOfContent_ReturnsTotalLength()
    {
        ImmutableArray<Content> parts = [
            new Content("abc"),   // 3
            new Content("de"),    // 2
            new Content("fghi")   // 4
        ];

        var content = new Content(parts);

        Assert.Equal(9, content.Length);
    }

    [Fact]
    public void Length_ImmutableArrayOfReadOnlyMemory_ReturnsTotalLength()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts = [
            "foo".AsMemory(),     // 3
            "bar".AsMemory(),     // 3
            "baz".AsMemory()      // 3
        ];

        var content = new Content(parts);

        Assert.Equal(9, content.Length);
    }

    [Fact]
    public void Length_ImmutableArrayOfString_ReturnsTotalLength()
    {
        ImmutableArray<string> parts = ["hello", "world", "test"];
        var content = new Content(parts);

        Assert.Equal(14, content.Length); // 5 + 5 + 4 = 14
    }

    [Fact]
    public void Length_EmptyPartsArray_ReturnsZero()
    {
        var content = new Content(ImmutableArray<Content>.Empty);

        Assert.Equal(0, content.Length);
    }

    [Fact]
    public void Length_EmptyStringArray_ReturnsZero()
    {
        var content = new Content(ImmutableArray<string>.Empty);

        Assert.Equal(0, content.Length);
    }

    [Fact]
    public void Length_EmptyMemoryArray_ReturnsZero()
    {
        var content = new Content(ImmutableArray<ReadOnlyMemory<char>>.Empty);

        Assert.Equal(0, content.Length);
    }

    [Fact]
    public void Length_NestedContent_ReturnsTotalLength()
    {
        var nested = new Content([
            new Content("A"),                    // 1
            new Content([
                new Content("BC"),               // 2
                new Content([
                    new Content("D"),            // 1
                    new Content("EF")            // 2
                ])
            ]),
            new Content("G")                     // 1
        ]);

        Assert.Equal(7, nested.Length); // A + BC + D + EF + G = 1 + 2 + 1 + 2 + 1 = 7
    }

    [Fact]
    public void Length_DeeplyNestedContent_ReturnsTotalLength()
    {
        var deep = new Content([
            new Content([
                new Content([
                    new Content("Hello") // 5
                ])
            ])
        ]);

        Assert.Equal(5, deep.Length);
    }

    [Fact]
    public void Length_MixedContentTypes_ReturnsTotalLength()
    {
        ImmutableArray<Content> parts = [
            new Content("String"),                                                  // 6
            new Content("Memory".AsMemory()),                                       // 6
            new Content(["Array", "Of", "Strings"]),                                // 5 + 2 + 7 = 14
            new Content(["Array".AsMemory(), "Of".AsMemory(), "Memory".AsMemory()]) // 5 + 2 + 6 = 13
        ];

        var content = new Content(parts);

        Assert.Equal(39, content.Length); // 6 + 6 + 14 + 13 = 39
    }

    [Fact]
    public void Length_StringArrayWithNulls_SkipsNulls()
    {
        ImmutableArray<string> parts = ["start", null!, "middle", null!, "end"];
        var content = new Content(parts);

        Assert.Equal(14, content.Length); // "start" + "middle" + "end" = 5 + 6 + 3 = 14
    }

    [Fact]
    public void Length_StringArrayWithEmptyStrings_IncludesEmptyStrings()
    {
        ImmutableArray<string> parts = ["A", "", "B", "", "C"];
        var content = new Content(parts);

        Assert.Equal(3, content.Length); // "A" + "" + "B" + "" + "C" = 1 + 0 + 1 + 0 + 1 = 3
    }

    [Fact]
    public void Length_MemoryArrayWithEmptyMemories_IncludesEmptyMemories()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts = [
            "start".AsMemory(),              // 5
            ReadOnlyMemory<char>.Empty,      // 0
            "middle".AsMemory(),             // 6
            ReadOnlyMemory<char>.Empty,      // 0
            "end".AsMemory()                 // 3
        ];

        var content = new Content(parts);

        Assert.Equal(14, content.Length); // 5 + 0 + 6 + 0 + 3 = 14
    }

    [Fact]
    public void Length_SinglePartArray_OptimizesToSingleValue()
    {
        // When array has single part, it should optimize to store just the value
        ImmutableArray<string> parts = ["single"];
        var content = new Content(parts);

        Assert.Equal(6, content.Length);
        Assert.False(content.HasParts); // Should be optimized to single value
    }

    [Fact]
    public void Length_SingleContentPartArray_OptimizesToSingleValue()
    {
        ImmutableArray<Content> parts = [new Content("single")];
        var content = new Content(parts);

        Assert.Equal(6, content.Length);
        Assert.False(content.HasParts); // Should be optimized to single value
    }

    [Fact]
    public void Length_SingleMemoryPartArray_OptimizesToSingleValue()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts = ["single".AsMemory()];
        var content = new Content(parts);

        Assert.Equal(6, content.Length);
        Assert.False(content.HasParts); // Should be optimized to single value
    }

    [Fact]
    public void Length_LargeContent_HandlesCorrectly()
    {
        var largeParts = new string[1000];
        var expectedLength = 0;

        for (var i = 0; i < 1000; i++)
        {
            largeParts[i] = i.ToString();
            expectedLength += largeParts[i].Length;
        }

        var content = new Content(largeParts.ToImmutableArray());

        Assert.Equal(expectedLength, content.Length);
    }

    [Fact]
    public void Length_SpecialCharacters_CountsCorrectly()
    {
        ImmutableArray<string> parts = ["Hello\n", "World\t", "With\r\n", "Special\"Chars"];
        var content = new Content(parts);

        var expectedLength = "Hello\n".Length + "World\t".Length + "With\r\n".Length + "Special\"Chars".Length;
        Assert.Equal(expectedLength, content.Length);
        Assert.Equal(31, content.Length); // 6 + 6 + 6 + 13 = 31
    }

    [Fact]
    public void IsNullOrEmpty_NullContent_ReturnsTrue()
    {
        Content? content = null;

        var result = Content.IsNullOrEmpty(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrEmpty_EmptyContent_ReturnsTrue()
    {
        var content = Content.Empty;

        var result = Content.IsNullOrEmpty(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrEmpty_NonEmptyString_ReturnsFalse()
    {
        var content = new Content("hello");

        var result = Content.IsNullOrEmpty(content);

        Assert.False(result);
    }

    [Fact]
    public void IsNullOrEmpty_NonEmptyReadOnlyMemory_ReturnsFalse()
    {
        var content = new Content("world".AsMemory());

        var result = Content.IsNullOrEmpty(content);

        Assert.False(result);
    }

    [Fact]
    public void IsNullOrEmpty_EmptyString_ReturnsTrue()
    {
        var content = new Content("");

        var result = Content.IsNullOrEmpty(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrEmpty_EmptyReadOnlyMemory_ReturnsTrue()
    {
        var content = new Content(ReadOnlyMemory<char>.Empty);

        var result = Content.IsNullOrEmpty(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrEmpty_EmptyContentArray_ReturnsTrue()
    {
        var content = new Content(ImmutableArray<Content>.Empty);

        var result = Content.IsNullOrEmpty(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrEmpty_EmptyStringArray_ReturnsTrue()
    {
        var content = new Content(ImmutableArray<string>.Empty);

        var result = Content.IsNullOrEmpty(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrEmpty_EmptyMemoryArray_ReturnsTrue()
    {
        var content = new Content(ImmutableArray<ReadOnlyMemory<char>>.Empty);

        var result = Content.IsNullOrEmpty(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrEmpty_NonEmptyContentArray_ReturnsFalse()
    {
        ImmutableArray<Content> parts = [new Content("a"), new Content("b")];
        var content = new Content(parts);

        var result = Content.IsNullOrEmpty(content);

        Assert.False(result);
    }

    [Fact]
    public void IsNullOrEmpty_NonEmptyStringArray_ReturnsFalse()
    {
        ImmutableArray<string> parts = ["foo", "bar"];
        var content = new Content(parts);

        var result = Content.IsNullOrEmpty(content);

        Assert.False(result);
    }

    [Fact]
    public void IsNullOrEmpty_NonEmptyMemoryArray_ReturnsFalse()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts = ["x".AsMemory(), "y".AsMemory()];
        var content = new Content(parts);

        var result = Content.IsNullOrEmpty(content);

        Assert.False(result);
    }

    [Fact]
    public void IsNullOrEmpty_NestedEmptyContent_ReturnsTrue()
    {
        var nested = new Content([
            new Content(ImmutableArray<string>.Empty),
            new Content(ImmutableArray<Content>.Empty),
            new Content(ImmutableArray<ReadOnlyMemory<char>>.Empty)
        ]);

        var result = Content.IsNullOrEmpty(nested);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrEmpty_NestedNonEmptyContent_ReturnsFalse()
    {
        var nested = new Content([
            new Content(""),
            new Content([new Content("x")])
        ]);

        var result = Content.IsNullOrEmpty(nested);

        Assert.False(result);
    }

    [Fact]
    public void IsNullOrEmpty_ImplicitConversion_NullString_ReturnsTrue()
    {
        Content content = (string)null!;

        var result = Content.IsNullOrEmpty(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_NullContent_ReturnsTrue()
    {
        Content? content = null;

        var result = Content.IsNullOrWhiteSpace(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_EmptyContent_ReturnsTrue()
    {
        var content = Content.Empty;

        var result = Content.IsNullOrWhiteSpace(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_WhitespaceString_ReturnsTrue()
    {
        var content = new Content("   \t\r\n  ");

        var result = Content.IsNullOrWhiteSpace(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_WhitespaceReadOnlyMemory_ReturnsTrue()
    {
        var content = new Content("  \t  ".AsMemory());

        var result = Content.IsNullOrWhiteSpace(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_NonWhitespaceString_ReturnsFalse()
    {
        var content = new Content("hello");

        var result = Content.IsNullOrWhiteSpace(content);

        Assert.False(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_NonWhitespaceReadOnlyMemory_ReturnsFalse()
    {
        var content = new Content("world".AsMemory());

        var result = Content.IsNullOrWhiteSpace(content);

        Assert.False(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_EmptyString_ReturnsTrue()
    {
        var content = new Content("");

        var result = Content.IsNullOrWhiteSpace(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_EmptyReadOnlyMemory_ReturnsTrue()
    {
        var content = new Content(ReadOnlyMemory<char>.Empty);

        var result = Content.IsNullOrWhiteSpace(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_EmptyContentArray_ReturnsTrue()
    {
        var content = new Content(ImmutableArray<Content>.Empty);

        var result = Content.IsNullOrWhiteSpace(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_EmptyStringArray_ReturnsTrue()
    {
        var content = new Content(ImmutableArray<string>.Empty);

        var result = Content.IsNullOrWhiteSpace(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_EmptyMemoryArray_ReturnsTrue()
    {
        var content = new Content(ImmutableArray<ReadOnlyMemory<char>>.Empty);

        var result = Content.IsNullOrWhiteSpace(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_AllWhitespaceContentArray_ReturnsTrue()
    {
        ImmutableArray<Content> parts = [
            new Content("   "),
            new Content("\t\t"),
            new Content("\r\n")
        ];

        var content = new Content(parts);

        var result = Content.IsNullOrWhiteSpace(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_AllWhitespaceStringArray_ReturnsTrue()
    {
        ImmutableArray<string> parts = ["   ", "\t\t", "\r\n", ""];
        var content = new Content(parts);

        var result = Content.IsNullOrWhiteSpace(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_AllWhitespaceMemoryArray_ReturnsTrue()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts = [
            "   ".AsMemory(),
            "\t\t".AsMemory(),
            "\r\n".AsMemory(),
            ReadOnlyMemory<char>.Empty
        ];

        var content = new Content(parts);

        var result = Content.IsNullOrWhiteSpace(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_MixedWhitespaceAndNonWhitespaceContentArray_ReturnsFalse()
    {
        ImmutableArray<Content> parts = [
            new Content("   "),
            new Content("hello"),
            new Content("\t\t")
        ];

        var content = new Content(parts);

        var result = Content.IsNullOrWhiteSpace(content);

        Assert.False(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_MixedWhitespaceAndNonWhitespaceStringArray_ReturnsFalse()
    {
        ImmutableArray<string> parts = ["   ", "world", "\t\t"];
        var content = new Content(parts);

        var result = Content.IsNullOrWhiteSpace(content);

        Assert.False(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_MixedWhitespaceAndNonWhitespaceMemoryArray_ReturnsFalse()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts = [
            "   ".AsMemory(),
            "test".AsMemory(),
            "\t\t".AsMemory()
        ];

        var content = new Content(parts);

        var result = Content.IsNullOrWhiteSpace(content);

        Assert.False(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_NestedAllWhitespaceContent_ReturnsTrue()
    {
        var nested = new Content([
            new Content("   "),
            new Content([
                new Content("\t"),
                new Content([
                    new Content("\r\n"),
                    new Content("  ")
                ])
            ]),
            new Content("")
        ]);

        var result = Content.IsNullOrWhiteSpace(nested);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_NestedMixedContent_ReturnsFalse()
    {
        var nested = new Content([
            new Content("   "),
            new Content([
                new Content("\t"),
                new Content([
                    new Content("x"),
                    new Content("  ")
                ])
            ])
        ]);

        var result = Content.IsNullOrWhiteSpace(nested);

        Assert.False(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_StringWithVariousWhitespaceChars_ReturnsTrue()
    {
        var content = new Content(" \t\r\n\u00A0\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200A\u202F\u205F\u3000");

        var result = Content.IsNullOrWhiteSpace(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_StringWithNonBreakingSpace_ReturnsTrue()
    {
        var content = new Content("\u00A0\u00A0\u00A0");

        var result = Content.IsNullOrWhiteSpace(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_StringWithZeroWidthSpace_ReturnsFalse()
    {
        var content = new Content("\u200B"); // Zero-width space is not considered whitespace by char.IsWhiteSpace

        var result = Content.IsNullOrWhiteSpace(content);

        Assert.False(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_ImplicitConversion_NullString_ReturnsTrue()
    {
        Content content = (string)null!;

        var result = Content.IsNullOrWhiteSpace(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_ImplicitConversion_WhitespaceString_ReturnsTrue()
    {
        Content content = "   \t   ";

        var result = Content.IsNullOrWhiteSpace(content);

        Assert.True(result);
    }

    [Fact]
    public void IsNullOrWhiteSpace_ImplicitConversion_NonWhitespaceString_ReturnsFalse()
    {
        Content content = "hello world";

        var result = Content.IsNullOrWhiteSpace(content);

        Assert.False(result);
    }

    [Fact]
    public void IsWhiteSpace_EmptyContent_ReturnsTrue()
    {
        var content = Content.Empty;

        var result = content.IsWhiteSpace();

        Assert.True(result);
    }

    [Fact]
    public void IsWhiteSpace_EmptyString_ReturnsTrue()
    {
        var content = new Content("");

        var result = content.IsWhiteSpace();

        Assert.True(result);
    }

    [Fact]
    public void IsWhiteSpace_EmptyReadOnlyMemory_ReturnsTrue()
    {
        var content = new Content(ReadOnlyMemory<char>.Empty);

        var result = content.IsWhiteSpace();

        Assert.True(result);
    }

    [Fact]
    public void IsWhiteSpace_WhitespaceString_ReturnsTrue()
    {
        var content = new Content("   \t\r\n  ");

        var result = content.IsWhiteSpace();

        Assert.True(result);
    }

    [Fact]
    public void IsWhiteSpace_NonWhitespaceString_ReturnsFalse()
    {
        var content = new Content("hello");

        var result = content.IsWhiteSpace();

        Assert.False(result);
    }

    [Fact]
    public void IsWhiteSpace_WhitespaceReadOnlyMemory_ReturnsTrue()
    {
        var content = new Content("  \t  ".AsMemory());

        var result = content.IsWhiteSpace();

        Assert.True(result);
    }

    [Fact]
    public void IsWhiteSpace_NonWhitespaceReadOnlyMemory_ReturnsFalse()
    {
        var content = new Content("world".AsMemory());

        var result = content.IsWhiteSpace();

        Assert.False(result);
    }

    [Fact]
    public void IsWhiteSpace_AllWhitespaceContentArray_ReturnsTrue()
    {
        ImmutableArray<Content> parts = [
            new Content("   "),
            new Content("\t\t"),
            new Content("\r\n"),
            new Content("")
        ];

        var content = new Content(parts);

        var result = content.IsWhiteSpace();

        Assert.True(result);
    }

    [Fact]
    public void IsWhiteSpace_MixedWhitespaceAndNonWhitespaceContentArray_ReturnsFalse()
    {
        ImmutableArray<Content> parts = [
            new Content("   "),
            new Content("hello"),
            new Content("\t\t")
        ];

        var content = new Content(parts);

        var result = content.IsWhiteSpace();

        Assert.False(result);
    }

    [Fact]
    public void IsWhiteSpace_AllWhitespaceStringArray_ReturnsTrue()
    {
        ImmutableArray<string> parts = ["   ", "\t\t", "\r\n", ""];
        var content = new Content(parts);

        var result = content.IsWhiteSpace();

        Assert.True(result);
    }

    [Fact]
    public void IsWhiteSpace_MixedWhitespaceAndNonWhitespaceStringArray_ReturnsFalse()
    {
        ImmutableArray<string> parts = ["   ", "world", "\t\t"];
        var content = new Content(parts);

        var result = content.IsWhiteSpace();

        Assert.False(result);
    }

    [Fact]
    public void IsWhiteSpace_StringArrayWithNulls_IgnoresNulls()
    {
        ImmutableArray<string> parts = ["   ", null!, "\t\t", null!];
        var content = new Content(parts);

        var result = content.IsWhiteSpace();

        Assert.True(result);
    }

    [Fact]
    public void IsWhiteSpace_AllWhitespaceMemoryArray_ReturnsTrue()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts = [
            "   ".AsMemory(),
            "\t\t".AsMemory(),
            "\r\n".AsMemory(),
            ReadOnlyMemory<char>.Empty
        ];

        var content = new Content(parts);

        var result = content.IsWhiteSpace();

        Assert.True(result);
    }

    [Fact]
    public void IsWhiteSpace_MixedWhitespaceAndNonWhitespaceMemoryArray_ReturnsFalse()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts = [
            "   ".AsMemory(),
            "test".AsMemory(),
            "\t\t".AsMemory()
        ];

        var content = new Content(parts);

        var result = content.IsWhiteSpace();

        Assert.False(result);
    }

    [Fact]
    public void IsWhiteSpace_NestedAllWhitespaceContent_ReturnsTrue()
    {
        var nested = new Content([
            new Content("   "),
            new Content([
                new Content("\t"),
                new Content([
                    new Content("\r\n"),
                    new Content("  ")
                ])
            ]),
            new Content("")
        ]);

        var result = nested.IsWhiteSpace();

        Assert.True(result);
    }

    [Fact]
    public void IsWhiteSpace_NestedMixedContent_ReturnsFalse()
    {
        var nested = new Content([
            new Content("   "),
            new Content([
                new Content("\t"),
                new Content([
                    new Content("x"),
                    new Content("  ")
                ])
            ])
        ]);

        var result = nested.IsWhiteSpace();

        Assert.False(result);
    }

    [Fact]
    public void IsWhiteSpace_EmptyContentArray_ReturnsTrue()
    {
        var content = new Content(ImmutableArray<Content>.Empty);

        var result = content.IsWhiteSpace();

        Assert.True(result);
    }

    [Fact]
    public void IsWhiteSpace_EmptyStringArray_ReturnsTrue()
    {
        var content = new Content(ImmutableArray<string>.Empty);

        var result = content.IsWhiteSpace();

        Assert.True(result);
    }

    [Fact]
    public void IsWhiteSpace_EmptyMemoryArray_ReturnsTrue()
    {
        var content = new Content(ImmutableArray<ReadOnlyMemory<char>>.Empty);

        var result = content.IsWhiteSpace();

        Assert.True(result);
    }

    [Fact]
    public void IsWhiteSpace_StringWithVariousWhitespaceChars_ReturnsTrue()
    {
        var content = new Content(" \t\r\n\u00A0\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200A\u202F\u205F\u3000");

        var result = content.IsWhiteSpace();

        Assert.True(result);
    }

    [Fact]
    public void IsWhiteSpace_StringWithNonBreakingSpace_ReturnsTrue()
    {
        var content = new Content("\u00A0\u00A0\u00A0");

        var result = content.IsWhiteSpace();

        Assert.True(result);
    }

    [Fact]
    public void IsWhiteSpace_StringWithZeroWidthSpace_ReturnsFalse()
    {
        var content = new Content("\u200B"); // Zero-width space is not considered whitespace by char.IsWhiteSpace

        var result = content.IsWhiteSpace();

        Assert.False(result);
    }

    [Fact]
    public void IsWhiteSpace_StringWithMixedWhitespaceAndText_ReturnsFalse()
    {
        var content = new Content("  hello  ");

        var result = content.IsWhiteSpace();

        Assert.False(result);
    }

    [Fact]
    public void IsWhiteSpace_DeeplyNestedAllWhitespace_ReturnsTrue()
    {
        var deep = new Content([
            new Content([
                new Content([
                    new Content("   ")
                ])
            ])
        ]);

        var result = deep.IsWhiteSpace();

        Assert.True(result);
    }

    [Fact]
    public void IsWhiteSpace_DeeplyNestedWithNonWhitespace_ReturnsFalse()
    {
        var deep = new Content([
            new Content([
                new Content([
                    new Content("X")
                ])
            ])
        ]);

        var result = deep.IsWhiteSpace();

        Assert.False(result);
    }

    [Fact]
    public void IsWhiteSpace_MixedContentTypes_AllWhitespace_ReturnsTrue()
    {
        ImmutableArray<Content> parts = [
            new Content("   "),
            new Content("\t".AsMemory()),
            new Content([" ", "\r\n"]),
            new Content(["".AsMemory(), "  ".AsMemory()])
        ];

        var content = new Content(parts);

        var result = content.IsWhiteSpace();

        Assert.True(result);
    }

    [Fact]
    public void IsWhiteSpace_MixedContentTypes_WithNonWhitespace_ReturnsFalse()
    {
        ImmutableArray<Content> parts = [
            new Content("   "),
            new Content("\t".AsMemory()),
            new Content([" ", "text"]),
            new Content(["".AsMemory(), "  ".AsMemory()])
        ];

        var content = new Content(parts);

        var result = content.IsWhiteSpace();

        Assert.False(result);
    }

    [Fact]
    public void Equals_String_EmptyContentWithEmptyString_ReturnsTrue()
    {
        var content = Content.Empty;

        var result = content.Equals("");

        Assert.True(result);
    }

    [Fact]
    public void Equals_String_EmptyContentWithNonEmptyString_ReturnsFalse()
    {
        var content = Content.Empty;

        var result = content.Equals("hello");

        Assert.False(result);
    }

    [Fact]
    public void Equals_String_NonEmptyContentWithEmptyString_ReturnsFalse()
    {
        var content = new Content("hello");

        var result = content.Equals("");

        Assert.False(result);
    }

    [Fact]
    public void Equals_String_SingleStringContent_ExactMatch_ReturnsTrue()
    {
        var content = new Content("hello world");

        var result = content.Equals("hello world");

        Assert.True(result);
    }

    [Fact]
    public void Equals_String_SingleStringContent_NoMatch_ReturnsFalse()
    {
        var content = new Content("hello world");

        var result = content.Equals("hello there");

        Assert.False(result);
    }

    [Fact]
    public void Equals_String_SingleReadOnlyMemoryContent_ExactMatch_ReturnsTrue()
    {
        var content = new Content("test".AsMemory());

        var result = content.Equals("test");

        Assert.True(result);
    }

    [Fact]
    public void Equals_String_SingleReadOnlyMemoryContent_NoMatch_ReturnsFalse()
    {
        var content = new Content("test".AsMemory());

        var result = content.Equals("testing");

        Assert.False(result);
    }

    [Fact]
    public void Equals_String_ContentArray_ExactMatch_ReturnsTrue()
    {
        ImmutableArray<Content> parts = [new Content("foo"), new Content("bar"), new Content("baz")];
        var content = new Content(parts);

        var result = content.Equals("foobarbaz");

        Assert.True(result);
    }

    [Fact]
    public void Equals_String_ContentArray_NoMatch_ReturnsFalse()
    {
        ImmutableArray<Content> parts = [new Content("foo"), new Content("bar"), new Content("baz")];
        var content = new Content(parts);

        var result = content.Equals("foobar");

        Assert.False(result);
    }

    [Fact]
    public void Equals_String_StringArray_ExactMatch_ReturnsTrue()
    {
        ImmutableArray<string> parts = ["hello", "world"];
        var content = new Content(parts);

        var result = content.Equals("helloworld");

        Assert.True(result);
    }

    [Fact]
    public void Equals_String_StringArray_NoMatch_ReturnsFalse()
    {
        ImmutableArray<string> parts = ["hello", "world"];
        var content = new Content(parts);

        var result = content.Equals("hello world");

        Assert.False(result);
    }

    [Fact]
    public void Equals_String_ReadOnlyMemoryArray_ExactMatch_ReturnsTrue()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts = ["abc".AsMemory(), "def".AsMemory()];
        var content = new Content(parts);

        var result = content.Equals("abcdef");

        Assert.True(result);
    }

    [Fact]
    public void Equals_String_ReadOnlyMemoryArray_NoMatch_ReturnsFalse()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts = ["abc".AsMemory(), "def".AsMemory()];
        var content = new Content(parts);

        var result = content.Equals("abcde");

        Assert.False(result);
    }

    [Fact]
    public void Equals_String_NestedContent_ExactMatch_ReturnsTrue()
    {
        var nested = new Content([
            new Content("A"),
            new Content([
                new Content("B"),
                new Content([
                    new Content("C"),
                    new Content("D")
                ])
            ]),
            new Content("E")
        ]);

        var result = nested.Equals("ABCDE");

        Assert.True(result);
    }

    [Fact]
    public void Equals_String_NestedContent_NoMatch_ReturnsFalse()
    {
        var nested = new Content([
            new Content("A"),
            new Content([
                new Content("B"),
                new Content([
                    new Content("C"),
                    new Content("D")
                ])
            ]),
            new Content("E")
        ]);

        var result = nested.Equals("ABCD");

        Assert.False(result);
    }

    [Fact]
    public void Equals_String_MixedContentTypes_ExactMatch_ReturnsTrue()
    {
        ImmutableArray<Content> parts = [
            new Content("String"),
            new Content("Memory".AsMemory()),
            new Content(["Array", "Of", "Strings"]),
            new Content(["One".AsMemory(), "Two".AsMemory()])
        ];

        var content = new Content(parts);

        var result = content.Equals("StringMemoryArrayOfStringsOneTwo");

        Assert.True(result);
    }

    [Fact]
    public void Equals_String_MixedContentTypes_NoMatch_ReturnsFalse()
    {
        ImmutableArray<Content> parts = [
            new Content("String"),
            new Content("Memory".AsMemory()),
            new Content(["Array", "Of", "Strings"])
        ];

        var content = new Content(parts);

        var result = content.Equals("StringMemoryArrayOfString");

        Assert.False(result);
    }

    [Fact]
    public void Equals_String_StringArrayWithNulls_SkipsNulls_ReturnsTrue()
    {
        ImmutableArray<string> parts = ["start", null!, "end"];
        var content = new Content(parts);

        var result = content.Equals("startend");

        Assert.True(result);
    }

    [Fact]
    public void Equals_String_CaseSensitive_ReturnsFalse()
    {
        var content = new Content("Hello");

        var result = content.Equals("hello");

        Assert.False(result);
    }

    [Fact]
    public void Equals_String_SpecialCharacters_ReturnsTrue()
    {
        var content = new Content("Hello\nWorld\t!");

        var result = content.Equals("Hello\nWorld\t!");

        Assert.True(result);
    }

    [Fact]
    public void Equals_ReadOnlyMemory_EmptyContentWithEmptyMemory_ReturnsTrue()
    {
        var content = Content.Empty;

        var result = content.Equals(ReadOnlyMemory<char>.Empty);

        Assert.True(result);
    }

    [Fact]
    public void Equals_ReadOnlyMemory_EmptyContentWithNonEmptyMemory_ReturnsFalse()
    {
        var content = Content.Empty;

        var result = content.Equals("hello".AsMemory());

        Assert.False(result);
    }

    [Fact]
    public void Equals_ReadOnlyMemory_NonEmptyContentWithEmptyMemory_ReturnsFalse()
    {
        var content = new Content("hello");

        var result = content.Equals(ReadOnlyMemory<char>.Empty);

        Assert.False(result);
    }

    [Fact]
    public void Equals_ReadOnlyMemory_SingleStringContent_ExactMatch_ReturnsTrue()
    {
        var content = new Content("hello world");

        var result = content.Equals("hello world".AsMemory());

        Assert.True(result);
    }

    [Fact]
    public void Equals_ReadOnlyMemory_SingleStringContent_NoMatch_ReturnsFalse()
    {
        var content = new Content("hello world");

        var result = content.Equals("hello there".AsMemory());

        Assert.False(result);
    }

    [Fact]
    public void Equals_ReadOnlyMemory_SingleReadOnlyMemoryContent_ExactMatch_ReturnsTrue()
    {
        var content = new Content("test".AsMemory());

        var result = content.Equals("test".AsMemory());

        Assert.True(result);
    }

    [Fact]
    public void Equals_ReadOnlyMemory_SingleReadOnlyMemoryContent_NoMatch_ReturnsFalse()
    {
        var content = new Content("test".AsMemory());

        var result = content.Equals("testing".AsMemory());

        Assert.False(result);
    }

    [Fact]
    public void Equals_ReadOnlyMemory_ContentArray_ExactMatch_ReturnsTrue()
    {
        ImmutableArray<Content> parts = [new Content("foo"), new Content("bar"), new Content("baz")];
        var content = new Content(parts);

        var result = content.Equals("foobarbaz".AsMemory());

        Assert.True(result);
    }

    [Fact]
    public void Equals_ReadOnlyMemory_ContentArray_NoMatch_ReturnsFalse()
    {
        ImmutableArray<Content> parts = [new Content("foo"), new Content("bar"), new Content("baz")];
        var content = new Content(parts);

        var result = content.Equals("foobar".AsMemory());

        Assert.False(result);
    }

    [Fact]
    public void Equals_ReadOnlyMemory_StringArray_ExactMatch_ReturnsTrue()
    {
        ImmutableArray<string> parts = ["hello", "world"];
        var content = new Content(parts);

        var result = content.Equals("helloworld".AsMemory());

        Assert.True(result);
    }

    [Fact]
    public void Equals_ReadOnlyMemory_StringArray_NoMatch_ReturnsFalse()
    {
        ImmutableArray<string> parts = ["hello", "world"];
        var content = new Content(parts);

        var result = content.Equals("hello world".AsMemory());

        Assert.False(result);
    }

    [Fact]
    public void Equals_ReadOnlyMemory_ReadOnlyMemoryArray_ExactMatch_ReturnsTrue()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts = ["abc".AsMemory(), "def".AsMemory()];
        var content = new Content(parts);

        var result = content.Equals("abcdef".AsMemory());

        Assert.True(result);
    }

    [Fact]
    public void Equals_ReadOnlyMemory_ReadOnlyMemoryArray_NoMatch_ReturnsFalse()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts = ["abc".AsMemory(), "def".AsMemory()];
        var content = new Content(parts);

        var result = content.Equals("abcde".AsMemory());

        Assert.False(result);
    }

    [Fact]
    public void Equals_ReadOnlyMemory_NestedContent_ExactMatch_ReturnsTrue()
    {
        var nested = new Content([
            new Content("A"),
            new Content([
                new Content("B"),
                new Content([
                    new Content("C"),
                    new Content("D")
                ])
            ]),
            new Content("E")
        ]);

        var result = nested.Equals("ABCDE".AsMemory());

        Assert.True(result);
    }

    [Fact]
    public void Equals_ReadOnlyMemory_NestedContent_NoMatch_ReturnsFalse()
    {
        var nested = new Content([
            new Content("A"),
            new Content([
                new Content("B"),
                new Content([
                    new Content("C"),
                    new Content("D")
                ])
            ]),
            new Content("E")
        ]);

        var result = nested.Equals("ABCD".AsMemory());

        Assert.False(result);
    }

    [Fact]
    public void Equals_ReadOnlyMemory_MixedContentTypes_ExactMatch_ReturnsTrue()
    {
        ImmutableArray<Content> parts = [
            new Content("String"),
            new Content("Memory".AsMemory()),
            new Content(["Array", "Of", "Strings"]),
            new Content(["One".AsMemory(), "Two".AsMemory()])
        ];

        var content = new Content(parts);

        var result = content.Equals("StringMemoryArrayOfStringsOneTwo".AsMemory());

        Assert.True(result);
    }

    [Fact]
    public void Equals_ReadOnlyMemory_CaseSensitive_ReturnsFalse()
    {
        var content = new Content("Hello");

        var result = content.Equals("hello".AsMemory());

        Assert.False(result);
    }

    [Fact]
    public void Equals_ReadOnlyMemory_SpecialCharacters_ReturnsTrue()
    {
        var content = new Content("Hello\nWorld\t!");

        var result = content.Equals("Hello\nWorld\t!".AsMemory());

        Assert.True(result);
    }

    [Fact]
    public void Equals_ReadOnlyMemory_SubstringOfContent_ReturnsFalse()
    {
        var content = new Content("hello world");

        var result = content.Equals("hello".AsMemory());

        Assert.False(result);
    }

    [Fact]
    public void Equals_String_LongerStringThanContent_ReturnsFalse()
    {
        var content = new Content("hello");

        var result = content.Equals("hello world");

        Assert.False(result);
    }

    [Fact]
    public void Equals_ReadOnlyMemory_LongerMemoryThanContent_ReturnsFalse()
    {
        var content = new Content("hello");

        var result = content.Equals("hello world".AsMemory());

        Assert.False(result);
    }

    [Fact]
    public void Equals_Content_BothEmpty_ReturnsTrue()
    {
        var content1 = Content.Empty;
        var content2 = Content.Empty;

        var result = content1.Equals(content2);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Content_EmptyWithNonEmpty_ReturnsFalse()
    {
        var content1 = Content.Empty;
        var content2 = new Content("hello");

        var result = content1.Equals(content2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Content_NonEmptyWithEmpty_ReturnsFalse()
    {
        var content1 = new Content("hello");
        var content2 = Content.Empty;

        var result = content1.Equals(content2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Content_SingleStringValues_ExactMatch_ReturnsTrue()
    {
        var content1 = new Content("hello world");
        var content2 = new Content("hello world");

        var result = content1.Equals(content2);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Content_SingleStringValues_NoMatch_ReturnsFalse()
    {
        var content1 = new Content("hello world");
        var content2 = new Content("hello there");

        var result = content1.Equals(content2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Content_SingleMemoryValues_ExactMatch_ReturnsTrue()
    {
        var content1 = new Content("test".AsMemory());
        var content2 = new Content("test".AsMemory());

        var result = content1.Equals(content2);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Content_SingleMemoryValues_NoMatch_ReturnsFalse()
    {
        var content1 = new Content("test".AsMemory());
        var content2 = new Content("testing".AsMemory());

        var result = content1.Equals(content2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Content_StringVsMemory_ExactMatch_ReturnsTrue()
    {
        var content1 = new Content("hello");
        var content2 = new Content("hello".AsMemory());

        var result = content1.Equals(content2);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Content_StringVsMemory_NoMatch_ReturnsFalse()
    {
        var content1 = new Content("hello");
        var content2 = new Content("world".AsMemory());

        var result = content1.Equals(content2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Content_StringArrayVsString_ExactMatch_ReturnsTrue()
    {
        ImmutableArray<string> parts = ["foo", "bar", "baz"];
        var content1 = new Content(parts);
        var content2 = new Content("foobarbaz");

        var result = content1.Equals(content2);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Content_StringArrayVsString_NoMatch_ReturnsFalse()
    {
        ImmutableArray<string> parts = ["foo", "bar", "baz"];
        var content1 = new Content(parts);
        var content2 = new Content("foobar");

        var result = content1.Equals(content2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Content_MemoryArrayVsMemory_ExactMatch_ReturnsTrue()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts = ["abc".AsMemory(), "def".AsMemory()];
        var content1 = new Content(parts);
        var content2 = new Content("abcdef".AsMemory());

        var result = content1.Equals(content2);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Content_MemoryArrayVsMemory_NoMatch_ReturnsFalse()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts = ["abc".AsMemory(), "def".AsMemory()];
        var content1 = new Content(parts);
        var content2 = new Content("abcde".AsMemory());

        var result = content1.Equals(content2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Content_ContentArrayVsString_ExactMatch_ReturnsTrue()
    {
        ImmutableArray<Content> parts = [new Content("hello"), new Content("world")];
        var content1 = new Content(parts);
        var content2 = new Content("helloworld");

        var result = content1.Equals(content2);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Content_ContentArrayVsString_NoMatch_ReturnsFalse()
    {
        ImmutableArray<Content> parts = [new Content("hello"), new Content("world")];
        var content1 = new Content(parts);
        var content2 = new Content("hello world");

        var result = content1.Equals(content2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Content_TwoStringArrays_ExactMatch_ReturnsTrue()
    {
        ImmutableArray<string> parts1 = ["foo", "bar"];
        ImmutableArray<string> parts2 = ["foob", "ar"];
        var content1 = new Content(parts1);
        var content2 = new Content(parts2);

        var result = content1.Equals(content2);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Content_TwoStringArrays_NoMatch_ReturnsFalse()
    {
        ImmutableArray<string> parts1 = ["foo", "bar"];
        ImmutableArray<string> parts2 = ["foo", "baz"];
        var content1 = new Content(parts1);
        var content2 = new Content(parts2);

        var result = content1.Equals(content2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Content_TwoMemoryArrays_ExactMatch_ReturnsTrue()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts1 = ["ab".AsMemory(), "cd".AsMemory()];
        ImmutableArray<ReadOnlyMemory<char>> parts2 = ["abc".AsMemory(), "d".AsMemory()];
        var content1 = new Content(parts1);
        var content2 = new Content(parts2);

        var result = content1.Equals(content2);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Content_TwoMemoryArrays_NoMatch_ReturnsFalse()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts1 = ["ab".AsMemory(), "cd".AsMemory()];
        ImmutableArray<ReadOnlyMemory<char>> parts2 = ["ab".AsMemory(), "ce".AsMemory()];
        var content1 = new Content(parts1);
        var content2 = new Content(parts2);

        var result = content1.Equals(content2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Content_TwoContentArrays_ExactMatch_ReturnsTrue()
    {
        ImmutableArray<Content> parts1 = [new Content("A"), new Content("B")];
        ImmutableArray<Content> parts2 = [new Content("AB")];
        var content1 = new Content(parts1);
        var content2 = new Content(parts2);

        var result = content1.Equals(content2);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Content_TwoContentArrays_NoMatch_ReturnsFalse()
    {
        ImmutableArray<Content> parts1 = [new Content("A"), new Content("B")];
        ImmutableArray<Content> parts2 = [new Content("A"), new Content("C")];
        var content1 = new Content(parts1);
        var content2 = new Content(parts2);

        var result = content1.Equals(content2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Content_NestedContent_ExactMatch_ReturnsTrue()
    {
        var nested1 = new Content([
            new Content("A"),
            new Content([
                new Content("B"),
                new Content("C")
            ])
        ]);

        var nested2 = new Content([
            new Content("AB"),
            new Content("C")
        ]);

        var result = nested1.Equals(nested2);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Content_NestedContent_NoMatch_ReturnsFalse()
    {
        var nested1 = new Content([
            new Content("A"),
            new Content([
                new Content("B"),
                new Content("C")
            ])
        ]);

        var nested2 = new Content([
            new Content("A"),
            new Content("B"),
            new Content("D")
        ]);

        var result = nested1.Equals(nested2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Content_MixedContentTypes_ExactMatch_ReturnsTrue()
    {
        ImmutableArray<Content> parts1 = [
            new Content("String"),
            new Content("Memory".AsMemory()),
            new Content(["Array"])
        ];

        ImmutableArray<Content> parts2 = [
            new Content("StringMem"),
            new Content("oryArray")
        ];

        var content1 = new Content(parts1);
        var content2 = new Content(parts2);

        var result = content1.Equals(content2);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Content_MixedContentTypes_NoMatch_ReturnsFalse()
    {
        ImmutableArray<Content> parts1 = [
            new Content("String"),
            new Content("Memory".AsMemory())
        ];

        ImmutableArray<Content> parts2 = [
            new Content("String"),
            new Content("Array")
        ];

        var content1 = new Content(parts1);
        var content2 = new Content(parts2);

        var result = content1.Equals(content2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Content_WithEmptyParts_SkipsEmpty_ReturnsTrue()
    {
        ImmutableArray<string> parts1 = ["hello", "", "world"];
        ImmutableArray<string> parts2 = ["hellow", "orld"];
        var content1 = new Content(parts1);
        var content2 = new Content(parts2);

        var result = content1.Equals(content2);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Content_WithEmptyMemoryParts_SkipsEmpty_ReturnsTrue()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts1 = [
            "hello".AsMemory(),
            ReadOnlyMemory<char>.Empty,
            "world".AsMemory()
        ];

        ImmutableArray<ReadOnlyMemory<char>> parts2 = [
            "hellow".AsMemory(),
            "orld".AsMemory()
        ];

        var content1 = new Content(parts1);
        var content2 = new Content(parts2);

        var result = content1.Equals(content2);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Content_StringArrayWithNulls_SkipsNulls_ReturnsTrue()
    {
        ImmutableArray<string> parts1 = ["start", null!, "end"];
        var content1 = new Content(parts1);
        var content2 = new Content("startend");

        var result = content1.Equals(content2);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Content_DifferentLengths_ReturnsFalse()
    {
        var content1 = new Content("hello");
        var content2 = new Content("hello world");

        var result = content1.Equals(content2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Content_CaseSensitive_ReturnsFalse()
    {
        var content1 = new Content("Hello");
        var content2 = new Content("hello");

        var result = content1.Equals(content2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Content_SpecialCharacters_ReturnsTrue()
    {
        var content1 = new Content("Hello\nWorld\t!");
        var content2 = new Content("Hello\nWorld\t!");

        var result = content1.Equals(content2);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Content_DeeplyNestedContent_ExactMatch_ReturnsTrue()
    {
        var deep1 = new Content([
            new Content([
                new Content([
                    new Content("Deep")
                ])
            ])
        ]);

        var deep2 = new Content("Deep");

        var result = deep1.Equals(deep2);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Content_DeeplyNestedContent_NoMatch_ReturnsFalse()
    {
        var deep1 = new Content([
            new Content([
                new Content([
                    new Content("Deep1")
                ])
            ])
        ]);

        var deep2 = new Content("Deep2");

        var result = deep1.Equals(deep2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Content_EmptyArrays_ReturnsTrue()
    {
        var content1 = new Content(ImmutableArray<string>.Empty);
        var content2 = new Content(ImmutableArray<Content>.Empty);

        var result = content1.Equals(content2);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Content_OneEmptyArrayOneNonEmpty_ReturnsFalse()
    {
        var content1 = new Content(ImmutableArray<string>.Empty);
        var content2 = new Content("hello");

        var result = content1.Equals(content2);

        Assert.False(result);
    }

    [Fact]
    public void Equals_Content_SameReference_ReturnsTrue()
    {
        var content = new Content("hello world");

        var result = content.Equals(content);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Content_LargeContent_HandlesCorrectly()
    {
        var largeParts1 = new string[100];
        var largeParts2 = new string[100];

        for (var i = 0; i < 100; i++)
        {
            largeParts1[i] = i.ToString();
            largeParts2[i] = i.ToString();
        }

        var content1 = new Content(largeParts1.ToImmutableArray());
        var content2 = new Content(largeParts2.ToImmutableArray());

        var result = content1.Equals(content2);

        Assert.True(result);
    }

    [Fact]
    public void Equals_Content_PartialMatch_ReturnsFalse()
    {
        var content1 = new Content("hello world");
        var content2 = new Content("hello");

        var result = content1.Equals(content2);

        Assert.False(result);
    }

    [Fact]
    public void ContentExtensions_StringBuilder_Append_EmptyContent_DoesNotModifyBuilder()
    {
        var builder = new StringBuilder("initial");
        var content = Content.Empty;

        var result = builder.Append(content);

        Assert.Same(builder, result);
        Assert.Equal("initial", builder.ToString());
    }

    [Fact]
    public void ContentExtensions_StringBuilder_Append_SingleString_AppendsCorrectly()
    {
        var builder = new StringBuilder("prefix");
        var content = new Content("hello");

        var result = builder.Append(content);

        Assert.Same(builder, result);
        Assert.Equal("prefixhello", builder.ToString());
    }

    [Fact]
    public void ContentExtensions_StringBuilder_Append_SingleReadOnlyMemory_AppendsCorrectly()
    {
        var builder = new StringBuilder("start");
        var content = new Content("world".AsMemory());

        var result = builder.Append(content);

        Assert.Same(builder, result);
        Assert.Equal("startworld", builder.ToString());
    }

    [Fact]
    public void ContentExtensions_StringBuilder_Append_ContentArray_AppendsAllParts()
    {
        var builder = new StringBuilder();
        ImmutableArray<Content> parts = [new Content("foo"), new Content("bar"), new Content("baz")];
        var content = new Content(parts);

        var result = builder.Append(content);

        Assert.Same(builder, result);
        Assert.Equal("foobarbaz", builder.ToString());
    }

    [Fact]
    public void ContentExtensions_StringBuilder_Append_StringArray_AppendsAllParts()
    {
        var builder = new StringBuilder("prefix");
        ImmutableArray<string> parts = ["a", "b", "c"];
        var content = new Content(parts);

        var result = builder.Append(content);

        Assert.Same(builder, result);
        Assert.Equal("prefixabc", builder.ToString());
    }

    [Fact]
    public void ContentExtensions_StringBuilder_Append_ReadOnlyMemoryArray_AppendsAllParts()
    {
        var builder = new StringBuilder();
        ImmutableArray<ReadOnlyMemory<char>> parts = ["x".AsMemory(), "y".AsMemory(), "z".AsMemory()];
        var content = new Content(parts);

        var result = builder.Append(content);

        Assert.Same(builder, result);
        Assert.Equal("xyz", builder.ToString());
    }

    [Fact]
    public void ContentExtensions_StringBuilder_Append_NestedContent_AppendsAllParts()
    {
        var builder = new StringBuilder();
        var nested = new Content([
            new Content("A"),
            new Content([
                new Content("B"),
                new Content("C")
            ]),
            new Content("D")
        ]);

        var result = builder.Append(nested);

        Assert.Same(builder, result);
        Assert.Equal("ABCD", builder.ToString());
    }

    [Fact]
    public void ContentExtensions_StringBuilder_Append_MixedContentTypes_AppendsAllParts()
    {
        var builder = new StringBuilder();
        ImmutableArray<Content> parts = [
            new Content("String"),
            new Content("Memory".AsMemory()),
            new Content(["Array", "Parts"]),
            new Content(["Mem1".AsMemory(), "Mem2".AsMemory()])
        ];
        var content = new Content(parts);

        var result = builder.Append(content);

        Assert.Same(builder, result);
        Assert.Equal("StringMemoryArrayPartsMem1Mem2", builder.ToString());
    }

    [Fact]
    public void ContentExtensions_StringBuilder_Append_StringArrayWithNulls_SkipsNulls()
    {
        var builder = new StringBuilder();
        ImmutableArray<string> parts = ["start", null!, "middle", null!, "end"];
        var content = new Content(parts);

        var result = builder.Append(content);

        Assert.Same(builder, result);
        Assert.Equal("startmiddleend", builder.ToString());
    }

    [Fact]
    public void ContentExtensions_StringBuilder_Append_EmptyStringBuilder_WorksCorrectly()
    {
        var builder = new StringBuilder();
        var content = new Content("hello world");

        var result = builder.Append(content);

        Assert.Same(builder, result);
        Assert.Equal("hello world", builder.ToString());
    }

    [Fact]
    public void ContentExtensions_StringBuilder_Append_SpecialCharacters_PreservesCharacters()
    {
        var builder = new StringBuilder();
        var content = new Content("Hello\nWorld\t\r\n\"Special\"");

        var result = builder.Append(content);

        Assert.Same(builder, result);
        Assert.Equal("Hello\nWorld\t\r\n\"Special\"", builder.ToString());
    }

    [Fact]
    public void ContentExtensions_PooledArrayBuilder_ToContent_EmptyBuilder_ReturnsEmpty()
    {
        using var builder = new PooledArrayBuilder<Content>();

        var result = builder.ToContent();

        Assert.True(result.IsEmpty);
        Assert.Equal(Content.Empty, result);
    }

    [Fact]
    public void ContentExtensions_PooledArrayBuilder_ToContent_SingleItem_ReturnsSameItem()
    {
        using var builder = new PooledArrayBuilder<Content>();
        var content = new Content("hello");
        builder.Add(content);

        var result = builder.ToContent();

        Assert.Equal(content, result);
        Assert.Equal("hello", GetWrittenString(result));
    }

    [Fact]
    public void ContentExtensions_PooledArrayBuilder_ToContent_MultipleItems_ReturnsContentArray()
    {
        using var builder = new PooledArrayBuilder<Content>();
        builder.Add(new Content("foo"));
        builder.Add(new Content("bar"));
        builder.Add(new Content("baz"));

        var result = builder.ToContent();

        Assert.False(result.IsEmpty);
        Assert.Equal("foobarbaz", GetWrittenString(result));
    }

    [Fact]
    public void ContentExtensions_PooledArrayBuilder_ToContent_MixedContentTypes_WorksCorrectly()
    {
        using var builder = new PooledArrayBuilder<Content>();
        builder.Add(new Content("String"));
        builder.Add(new Content("Memory".AsMemory()));
        builder.Add(new Content(["Array", "Parts"]));

        var result = builder.ToContent();

        Assert.False(result.IsEmpty);
        Assert.Equal("StringMemoryArrayParts", GetWrittenString(result));
    }

    [Fact]
    public void ContentExtensions_PooledArrayBuilder_ToContent_EmptyContentItems_IncludesEmpty()
    {
        using var builder = new PooledArrayBuilder<Content>();
        builder.Add(new Content("start"));
        builder.Add(Content.Empty);
        builder.Add(new Content("end"));

        var result = builder.ToContent();

        Assert.False(result.IsEmpty);
        Assert.Equal("startend", GetWrittenString(result));
    }

    [Fact]
    public void ContentExtensions_PooledArrayBuilder_ToContent_LargeNumberOfItems_HandlesCorrectly()
    {
        using var builder = new PooledArrayBuilder<Content>();

        for (var i = 0; i < 100; i++)
        {
            builder.Add(new Content(i.ToString()));
        }

        var result = builder.ToContent();

        Assert.False(result.IsEmpty);

        var expected = string.Concat(Enumerable.Range(0, 100).Select(i => i.ToString()));
        Assert.Equal(expected, GetWrittenString(result));
    }

    [Fact]
    public void ContentExtensions_PooledArrayBuilder_ToContent_NestedContent_FlattensCorrectly()
    {
        using var builder = new PooledArrayBuilder<Content>();
        builder.Add(new Content("A"));
        builder.Add(new Content([new Content("B"), new Content("C")]));
        builder.Add(new Content("D"));

        var result = builder.ToContent();

        Assert.False(result.IsEmpty);
        Assert.Equal("ABCD", GetWrittenString(result));
    }

    [Fact]
    public void ContentExtensions_PooledArrayBuilder_ToContent_AfterClear_ReturnsEmpty()
    {
        using var builder = new PooledArrayBuilder<Content>();
        builder.Add(new Content("test"));
        builder.Clear();

        var result = builder.ToContent();

        Assert.True(result.IsEmpty);
        Assert.Equal(Content.Empty, result);
    }

    [Fact]
    public void ContentExtensions_PooledArrayBuilder_ToContent_WithCapacity_WorksCorrectly()
    {
        using var builder = new PooledArrayBuilder<Content>(capacity: 10);
        builder.Add(new Content("hello"));
        builder.Add(new Content("world"));

        var result = builder.ToContent();

        Assert.False(result.IsEmpty);
        Assert.Equal("helloworld", GetWrittenString(result));
    }

    [Fact]
    public void Concatenation_TwoStrings_ReturnsCorrectResult()
    {
        var left = new Content("hello");
        var right = new Content("world");

        var result = left + right;

        Assert.Equal("helloworld", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_StringAndMemory_ReturnsCorrectResult()
    {
        var left = new Content("hello");
        var right = new Content("world".AsMemory());

        var result = left + right;

        Assert.Equal("helloworld", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_TwoMemories_ReturnsCorrectResult()
    {
        var left = new Content("foo".AsMemory());
        var right = new Content("bar".AsMemory());

        var result = left + right;

        Assert.Equal("foobar", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_EmptyWithNonEmpty_ReturnsNonEmpty()
    {
        var left = Content.Empty;
        var right = new Content("hello");

        var result = left + right;

        Assert.Equal("hello", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_NonEmptyWithEmpty_ReturnsNonEmpty()
    {
        var left = new Content("hello");
        var right = Content.Empty;

        var result = left + right;

        Assert.Equal("hello", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_TwoEmptyContents_ReturnsEmpty()
    {
        var left = Content.Empty;
        var right = Content.Empty;

        var result = left + right;

        Assert.True(result.IsEmpty);
        Assert.Equal(string.Empty, GetWrittenString(result));
    }

    [Fact]
    public void Concatenation_StringArrayWithString_ReturnsCorrectResult()
    {
        ImmutableArray<string> parts = ["foo", "bar"];
        var left = new Content(parts);
        var right = new Content("baz");

        var result = left + right;

        Assert.Equal("foobarbaz", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_StringWithStringArray_ReturnsCorrectResult()
    {
        var left = new Content("foo");
        ImmutableArray<string> parts = ["bar", "baz"];
        var right = new Content(parts);

        var result = left + right;

        Assert.Equal("foobarbaz", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_TwoStringArrays_ReturnsCorrectResult()
    {
        ImmutableArray<string> leftParts = ["foo", "bar"];
        ImmutableArray<string> rightParts = ["baz", "qux"];
        var left = new Content(leftParts);
        var right = new Content(rightParts);

        var result = left + right;

        Assert.Equal("foobarbazqux", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_MemoryArrayWithMemory_ReturnsCorrectResult()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts = ["foo".AsMemory(), "bar".AsMemory()];
        var left = new Content(parts);
        var right = new Content("baz".AsMemory());

        var result = left + right;

        Assert.Equal("foobarbaz", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_ContentArrayWithContent_ReturnsCorrectResult()
    {
        ImmutableArray<Content> parts = [new Content("foo"), new Content("bar")];
        var left = new Content(parts);
        var right = new Content("baz");

        var result = left + right;

        Assert.Equal("foobarbaz", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_NestedContentWithString_ReturnsCorrectResult()
    {
        var nested = new Content([
            new Content("A"),
            new Content([
                new Content("B"),
                new Content("C")
            ])
        ]);
        var right = new Content("D");

        var result = nested + right;

        Assert.Equal("ABCD", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_TwoNestedContents_ReturnsCorrectResult()
    {
        var left = new Content([
            new Content("A"),
            new Content("B")
        ]);

        var right = new Content([
            new Content("C"),
            new Content("D")
        ]);

        var result = left + right;

        Assert.Equal("ABCD", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_MixedContentTypes_ReturnsCorrectResult()
    {
        var left = new Content("String");
        var right = new Content([
            new Content("Memory".AsMemory()),
            new Content(["Array", "Parts"])
        ]);

        var result = left + right;

        Assert.Equal("StringMemoryArrayParts", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_StringArrayWithNulls_SkipsNulls()
    {
        ImmutableArray<string> parts = ["start", null!, "middle"];
        var left = new Content(parts);
        var right = new Content("end");

        var result = left + right;

        Assert.Equal("startmiddleend", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_SpecialCharacters_PreservesCharacters()
    {
        var left = new Content("Hello\n");
        var right = new Content("World\t!");

        var result = left + right;

        Assert.Equal("Hello\nWorld\t!", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_ChainedOperations_ReturnsCorrectResult()
    {
        var a = new Content("A");
        var b = new Content("B");
        var c = new Content("C");
        var d = new Content("D");

        var result = a + b + c + d;

        Assert.Equal("ABCD", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_WithImplicitConversions_ReturnsCorrectResult()
    {
        Content left = "hello";
        Content right = "world".AsMemory();

        var result = left + right;

        Assert.Equal("helloworld", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_WithImplicitStringArrayConversion_ReturnsCorrectResult()
    {
        ImmutableArray<string> stringArray = ["foo", "bar"];
        Content left = stringArray;
        Content right = "baz";

        var result = left + right;

        Assert.Equal("foobarbaz", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_WithImplicitMemoryArrayConversion_ReturnsCorrectResult()
    {
        ImmutableArray<ReadOnlyMemory<char>> memoryArray = ["hello".AsMemory(), "world".AsMemory()];
        Content left = memoryArray;

        var result = left + "!";

        Assert.Equal("helloworld!", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_WithImplicitContentArrayConversion_ReturnsCorrectResult()
    {
        ImmutableArray<Content> contentArray = [new Content("A"), new Content("B")];
        Content left = contentArray;
        Content right = new Content("C");

        var result = left + right;

        Assert.Equal("ABC", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_MixedImplicitConversions_StringAndMemoryArray_ReturnsCorrectResult()
    {
        ImmutableArray<ReadOnlyMemory<char>> parts = ["suffix".AsMemory(), "end".AsMemory()];
        Content right = parts;

        var result = "prefix" + right;

        Assert.Equal("prefixsuffixend", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_MixedImplicitConversions_MemoryAndStringArray_ReturnsCorrectResult()
    {
        ImmutableArray<string> parts = ["middle", "end"];
        Content right = parts;

        var result = "start".AsMemory() + right;

        Assert.Equal("startmiddleend", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_MixedImplicitConversions_StringArrayAndContentArray_ReturnsCorrectResult()
    {
        ImmutableArray<string> stringParts = ["A", "B"];
        ImmutableArray<Content> contentParts = [new Content("C"), new Content("D")];
        Content left = stringParts;
        Content right = contentParts;

        var result = left + right;

        Assert.Equal("ABCD", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_MixedImplicitConversions_MemoryArrayAndContentArray_ReturnsCorrectResult()
    {
        ImmutableArray<ReadOnlyMemory<char>> memoryParts = ["X".AsMemory(), "Y".AsMemory()];
        ImmutableArray<Content> contentParts = [new Content("Z")];
        Content left = memoryParts;
        Content right = contentParts;

        var result = left + right;

        Assert.Equal("XYZ", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_ImplicitNullStringConversion_WithContent_ReturnsCorrectResult()
    {
        Content left = null;

        var result = left + "hello";

        Assert.Equal("hello", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_ContentWithImplicitNullStringConversion_ReturnsCorrectResult()
    {
        Content right = null;

        var result = "hello" + right;

        Assert.Equal("hello", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_LargeContent_HandlesCorrectly()
    {
        var leftParts = new string[50];
        var rightParts = new string[50];

        for (var i = 0; i < 50; i++)
        {
            leftParts[i] = i.ToString();
            rightParts[i] = (i + 50).ToString();
        }

        var left = new Content(leftParts.ToImmutableArray());
        var right = new Content(rightParts.ToImmutableArray());

        var result = left + right;

        var expected = string.Concat(leftParts) + string.Concat(rightParts);
        Assert.Equal(expected, GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_ResultHasParts_ReturnsTrue()
    {
        var left = new Content("hello");
        var right = new Content("world");

        var result = left + right;

        Assert.True(result.HasParts);
        Assert.False(result.HasValue);
    }

    [Fact]
    public void Concatenation_ResultLength_IsCorrect()
    {
        var left = new Content("hello"); // 5
        var right = new Content("world"); // 5

        var result = left + right;

        Assert.Equal(10, result.Length);
    }

    [Fact]
    public void Concatenation_EmptyStringContent_WithNonEmpty_ReturnsCorrectResult()
    {
        var left = new Content("");
        var right = new Content("hello");

        var result = left + right;

        Assert.Equal("hello", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_WithEmptyMemoryContent_ReturnsCorrectResult()
    {
        var left = new Content(ReadOnlyMemory<char>.Empty);
        var right = new Content("hello");

        var result = left + right;

        Assert.Equal("hello", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_WithEmptyArrayContent_ReturnsCorrectResult()
    {
        var left = new Content(ImmutableArray<string>.Empty);
        var right = new Content("hello");

        var result = left + right;

        Assert.Equal("hello", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Concatenation_ComplexMixedImplicitConversions_ReturnsCorrectResult()
    {
        // Mix all types of implicit conversions
        Content stringContent = "String";
        Content memoryContent = "Memory".AsMemory();
        ImmutableArray<string> stringArray = ["Array", "Of", "Strings"];
        ImmutableArray<ReadOnlyMemory<char>> memoryArray = ["Mem".AsMemory(), "Array".AsMemory()];
        ImmutableArray<Content> contentArray = [new Content("Content"), new Content("Array")];

        var result = stringContent + memoryContent + stringArray + memoryArray + contentArray;

        Assert.Equal("StringMemoryArrayOfStringsMemArrayContentArray", GetWrittenString(result));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void EqualityOperator_EmptyContent_ReturnsTrue()
    {
        // Arrange
        var content1 = Content.Empty;
        var content2 = new Content((string?)null);
        var content3 = new Content(ReadOnlyMemory<char>.Empty);

        // Act & Assert
        Assert.True(content1 == content2);
        Assert.True(content2 == content3);
        Assert.True(content1 == content3);
    }

    [Fact]
    public void EqualityOperator_SameStringContent_ReturnsTrue()
    {
        // Arrange
        var content1 = new Content("Hello World");
        var content2 = new Content("Hello World");

        // Act & Assert
        Assert.True(content1 == content2);
    }

    [Fact]
    public void EqualityOperator_DifferentStringContent_ReturnsFalse()
    {
        // Arrange
        var content1 = new Content("Hello");
        var content2 = new Content("World");

        // Act & Assert
        Assert.False(content1 == content2);
    }

    [Fact]
    public void EqualityOperator_SameMemoryContent_ReturnsTrue()
    {
        // Arrange
        var text = "Hello World".AsMemory();
        var content1 = new Content(text);
        var content2 = new Content(text);

        // Act & Assert
        Assert.True(content1 == content2);
    }

    [Fact]
    public void EqualityOperator_MultiPartContent_SameContent_ReturnsTrue()
    {
        // Arrange
        var content1 = new Content(ImmutableArray.Create("Hello", " ", "World"));
        var content2 = new Content(ImmutableArray.Create("Hello ", "World"));

        // Act & Assert
        Assert.True(content1 == content2);
    }

    [Fact]
    public void EqualityOperator_MultiPartContent_DifferentContent_ReturnsFalse()
    {
        // Arrange
        var content1 = new Content(ImmutableArray.Create("Hello", " ", "World"));
        var content2 = new Content(ImmutableArray.Create("Hello", " ", "Universe"));

        // Act & Assert
        Assert.False(content1 == content2);
    }

    [Fact]
    public void EqualityOperator_SingleValueVsMultiPart_SameContent_ReturnsTrue()
    {
        // Arrange
        var content1 = new Content("Hello World");
        var content2 = new Content(ImmutableArray.Create("Hello", " ", "World"));

        // Act & Assert
        Assert.True(content1 == content2);
    }

    [Fact]
    public void EqualityOperator_WithEmptyContent_ReturnsExpected()
    {
        // Arrange
        var content1 = new Content("Hello");
        var content2 = Content.Empty;

        // Act & Assert
        Assert.False(content1 == content2);
        Assert.True(content2 == Content.Empty);
    }

    [Fact]
    public void EqualityOperator_MixedMemoryTypes_SameContent_ReturnsTrue()
    {
        // Arrange
        var content1 = new Content(ImmutableArray.Create("Hello".AsMemory(), " World".AsMemory()));
        var content2 = new Content(ImmutableArray.Create("Hello ", "World"));

        // Act & Assert
        Assert.True(content1 == content2);
    }

    [Fact]
    public void EqualityOperator_EmptyContent_ImplicitConversions_ReturnsTrue()
    {
        // Arrange
        var content = Content.Empty;

        // Act & Assert
        Assert.True(content == null);
        Assert.True(content == ReadOnlyMemory<char>.Empty);
    }

    [Fact]
    public void EqualityOperator_SameStringContent_ImplicitConversions_ReturnsTrue()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.True(content == "Hello World");
    }

    [Fact]
    public void EqualityOperator_DifferentStringContent_ImplicitConversions_ReturnsFalse()
    {
        // Arrange
        var content = new Content("Hello");

        // Act & Assert
        Assert.False(content == "World");
    }

    [Fact]
    public void EqualityOperator_SameMemoryContent_ImplicitConversions_ReturnsTrue()
    {
        // Arrange
        var text = "Hello World".AsMemory();
        var content = new Content(text);

        // Act & Assert
        Assert.True(content == text);
    }

    [Fact]
    public void EqualityOperator_MultiPartContent_SameContent_ImplicitConversions_ReturnsTrue()
    {
        // Arrange
        var content = new Content(ImmutableArray.Create("Hello", " ", "World"));
        var array = ImmutableArray.Create("Hello ", "World");

        // Act & Assert
        Assert.True(content == array);
    }

    [Fact]
    public void EqualityOperator_MultiPartContent_DifferentContent_ImplicitConversions_ReturnsFalse()
    {
        // Arrange
        var content = new Content(ImmutableArray.Create("Hello", " ", "World"));
        var array = ImmutableArray.Create("Hello", " ", "Universe");

        // Act & Assert
        Assert.False(content == array);
    }

    [Fact]
    public void EqualityOperator_SingleValueVsMultiPart_SameContent_ImplicitConversions_ReturnsTrue()
    {
        // Arrange
        var content = new Content(ImmutableArray.Create("Hello", " ", "World"));

        // Act & Assert
        Assert.True("Hello World" == content);
    }

    [Fact]
    public void EqualityOperator_WithEmptyContent_ImplicitConversions_ReturnsExpected()
    {
        // Arrange
        var content = Content.Empty;

        // Act & Assert
        Assert.False("Hello" == content);
        Assert.True(null == Content.Empty);
    }

    [Fact]
    public void EqualityOperator_MixedMemoryTypes_SameContent_ImplicitConversions_ReturnsTrue()
    {
        // Arrange
        var content = new Content(ImmutableArray.Create("Hello".AsMemory(), " World".AsMemory()));
        var array = ImmutableArray.Create("Hello ", "World");

        // Act & Assert
        Assert.True(content == array);
    }

    [Fact]
    public void InequalityOperator_EmptyContent_ReturnsFalse()
    {
        // Arrange
        var content1 = Content.Empty;
        var content2 = new Content((string?)null);

        // Act & Assert
        Assert.False(content1 != content2);
    }

    [Fact]
    public void InequalityOperator_SameStringContent_ReturnsFalse()
    {
        // Arrange
        var content1 = new Content("Hello World");
        var content2 = new Content("Hello World");

        // Act & Assert
        Assert.False(content1 != content2);
    }

    [Fact]
    public void InequalityOperator_DifferentStringContent_ReturnsTrue()
    {
        // Arrange
        var content1 = new Content("Hello");
        var content2 = new Content("World");

        // Act & Assert
        Assert.True(content1 != content2);
    }

    [Fact]
    public void InequalityOperator_MultiPartContent_SameContent_ReturnsFalse()
    {
        // Arrange
        var content1 = new Content(ImmutableArray.Create("Hello", " ", "World"));
        var content2 = new Content(ImmutableArray.Create("Hello ", "World"));

        // Act & Assert
        Assert.False(content1 != content2);
    }

    [Fact]
    public void InequalityOperator_MultiPartContent_DifferentContent_ReturnsTrue()
    {
        // Arrange
        var content1 = new Content(ImmutableArray.Create("Hello", " ", "World"));
        var content2 = new Content(ImmutableArray.Create("Hello", " ", "Universe"));

        // Act & Assert
        Assert.True(content1 != content2);
    }

    [Fact]
    public void InequalityOperator_SingleValueVsMultiPart_SameContent_ReturnsFalse()
    {
        // Arrange
        var content1 = new Content("Hello World");
        var content2 = new Content(ImmutableArray.Create("Hello", " ", "World"));

        // Act & Assert
        Assert.False(content1 != content2);
    }

    [Fact]
    public void InequalityOperator_WithEmptyParts_ReturnsExpected()
    {
        // Arrange
        var content1 = new Content(ImmutableArray.Create("Hello", "", "World"));
        var content2 = new Content("HelloWorld");

        // Act & Assert
        Assert.False(content1 != content2);
    }

    [Fact]
    public void InequalityOperator_EmptyContent_ImplicitConversions_ReturnsFalse()
    {
        // Arrange
        var content = Content.Empty;

        // Act & Assert
        Assert.False(content != null);
    }

    [Fact]
    public void InequalityOperator_SameStringContent_ImplicitConversions_ReturnsFalse()
    {
        // Arrange
        var content = new Content("Hello World");

        // Act & Assert
        Assert.False(content != "Hello World");
    }

    [Fact]
    public void InequalityOperator_DifferentStringContent_ImplicitConversions_ReturnsTrue()
    {
        // Arrange
        var content = new Content("Hello");

        // Act & Assert
        Assert.True(content != "World");
    }

    [Fact]
    public void InequalityOperator_MultiPartContent_SameContent_ImplicitConversions_ReturnsFalse()
    {
        // Arrange
        var content = new Content(ImmutableArray.Create("Hello", " ", "World"));
        var array = ImmutableArray.Create("Hello ", "World");

        // Act & Assert
        Assert.False(content != array);
    }

    [Fact]
    public void InequalityOperator_MultiPartContent_DifferentContent_ImplicitConversions_ReturnsTrue()
    {
        // Arrange
        var content = new Content(ImmutableArray.Create("Hello", " ", "World"));
        var array = ImmutableArray.Create("Hello", " ", "Universe");

        // Act & Assert
        Assert.True(content != array);
    }

    [Fact]
    public void InequalityOperator_SingleValueVsMultiPart_SameContent_ImplicitConversions_ReturnsFalse()
    {
        // Arrange
        var content = new Content("Hello World");
        var array = ImmutableArray.Create("Hello", " ", "World");

        // Act & Assert
        Assert.False(content != array);
    }

    [Fact]
    public void InequalityOperator_WithEmptyParts_ImplicitConversions_ReturnsExpected()
    {
        // Arrange
        var content = new Content(ImmutableArray.Create("Hello", "", "World"));

        // Act & Assert
        Assert.False(content != "HelloWorld");
    }

    [Fact]
    public void Join_Content_EmptyArray_ReturnsEmpty()
    {
        var separator = new Content(", ");
        var values = ImmutableArray<Content>.Empty;

        var result = Content.Join(separator, values);

        Assert.True(result.IsEmpty);
        Assert.Equal(string.Empty, GetWrittenString(result));
    }

    [Fact]
    public void Join_Content_SingleElement_ReturnsSingleElement()
    {
        var separator = new Content(", ");
        var values = ImmutableArray.Create(new Content("hello"));

        var result = Content.Join(separator, values);

        Assert.Equal("hello", GetWrittenString(result));
    }

    [Fact]
    public void Join_Content_MultipleElements_WithSeparator_ReturnsJoinedContent()
    {
        var separator = new Content(", ");
        var values = ImmutableArray.Create(
            new Content("apple"),
            new Content("banana"),
            new Content("cherry")
        );

        var result = Content.Join(separator, values);

        Assert.Equal("apple, banana, cherry", GetWrittenString(result));
    }

    [Fact]
    public void Join_Content_MultipleElements_EmptySeparator_ReturnsConcatenatedContent()
    {
        var separator = Content.Empty;
        var values = ImmutableArray.Create(
            new Content("foo"),
            new Content("bar"),
            new Content("baz")
        );

        var result = Content.Join(separator, values);

        Assert.Equal("foobarbaz", GetWrittenString(result));
    }

    [Fact]
    public void Join_Content_WithEmptyElements_SkipsEmpty()
    {
        var separator = new Content("-");
        var values = ImmutableArray.Create(
            new Content("start"),
            Content.Empty,
            new Content("middle"),
            Content.Empty,
            new Content("end")
        );

        var result = Content.Join(separator, values);

        Assert.Equal("start-middle-end", GetWrittenString(result));
    }

    [Fact]
    public void Join_Content_AllEmptyElements_ReturnsEmpty()
    {
        var separator = new Content(", ");
        var values = ImmutableArray.Create(
            Content.Empty,
            Content.Empty,
            Content.Empty
        );

        var result = Content.Join(separator, values);

        Assert.True(result.IsEmpty);
        Assert.Equal(string.Empty, GetWrittenString(result));
    }

    [Fact]
    public void Join_Content_MixedContentTypes_ReturnsCorrectResult()
    {
        var separator = new Content(" | ");
        var values = ImmutableArray.Create(
            new Content("String"),
            new Content("Memory".AsMemory()),
            new Content(ImmutableArray.Create("Array", "Parts"))
        );

        var result = Content.Join(separator, values);

        Assert.Equal("String | Memory | ArrayParts", GetWrittenString(result));
    }

    [Fact]
    public void Join_Content_ReadOnlySpan_MultipleElements_ReturnsCorrectResult()
    {
        var separator = new Content(", ");
        ReadOnlySpan<Content> values = [
            new Content("first"),
            new Content("second"),
            new Content("third")
        ];

        var result = Content.Join(separator, values);

        Assert.Equal("first, second, third", GetWrittenString(result));
    }

    [Fact]
    public void Join_ReadOnlyMemory_EmptyArray_ReturnsEmpty()
    {
        var separator = new Content(", ");
        var values = ImmutableArray<ReadOnlyMemory<char>>.Empty;

        var result = Content.Join(separator, values);

        Assert.True(result.IsEmpty);
        Assert.Equal(string.Empty, GetWrittenString(result));
    }

    [Fact]
    public void Join_ReadOnlyMemory_SingleElement_ReturnsSingleElement()
    {
        var separator = new Content(", ");
        var values = ImmutableArray.Create("hello".AsMemory());

        var result = Content.Join(separator, values);

        Assert.Equal("hello", GetWrittenString(result));
    }

    [Fact]
    public void Join_ReadOnlyMemory_MultipleElements_WithSeparator_ReturnsJoinedContent()
    {
        var separator = new Content(" - ");
        var values = ImmutableArray.Create(
            "apple".AsMemory(),
            "banana".AsMemory(),
            "cherry".AsMemory()
        );

        var result = Content.Join(separator, values);

        Assert.Equal("apple - banana - cherry", GetWrittenString(result));
    }

    [Fact]
    public void Join_ReadOnlyMemory_WithEmptyMemories_SkipsEmpty()
    {
        var separator = new Content(":");
        var values = ImmutableArray.Create(
            "start".AsMemory(),
            ReadOnlyMemory<char>.Empty,
            "middle".AsMemory(),
            ReadOnlyMemory<char>.Empty,
            "end".AsMemory()
        );

        var result = Content.Join(separator, values);

        Assert.Equal("start:middle:end", GetWrittenString(result));
    }

    [Fact]
    public void Join_ReadOnlyMemory_ReadOnlySpan_ReturnsCorrectResult()
    {
        var separator = new Content(" & ");
        ReadOnlySpan<ReadOnlyMemory<char>> values = [
            "alpha".AsMemory(),
            "beta".AsMemory(),
            "gamma".AsMemory()
        ];

        var result = Content.Join(separator, values);

        Assert.Equal("alpha & beta & gamma", GetWrittenString(result));
    }

    [Fact]
    public void Join_String_EmptyArray_ReturnsEmpty()
    {
        var separator = new Content(", ");
        var values = ImmutableArray<string>.Empty;

        var result = Content.Join(separator, values);

        Assert.True(result.IsEmpty);
        Assert.Equal(string.Empty, GetWrittenString(result));
    }

    [Fact]
    public void Join_String_SingleElement_ReturnsSingleElement()
    {
        var separator = new Content(", ");
        var values = ImmutableArray.Create("hello");

        var result = Content.Join(separator, values);

        Assert.Equal("hello", GetWrittenString(result));
    }

    [Fact]
    public void Join_String_MultipleElements_WithSeparator_ReturnsJoinedContent()
    {
        var separator = new Content(" | ");
        var values = ImmutableArray.Create("red", "green", "blue");

        var result = Content.Join(separator, values);

        Assert.Equal("red | green | blue", GetWrittenString(result));
    }

    [Fact]
    public void Join_String_WithNullAndEmptyStrings_SkipsNullsAndEmpty()
    {
        var separator = new Content("-");
        var values = ImmutableArray.Create("start", null!, "", "middle", null!, "end");

        var result = Content.Join(separator, values);

        Assert.Equal("start-middle-end", GetWrittenString(result));
    }

    [Fact]
    public void Join_String_AllNullAndEmpty_ReturnsEmpty()
    {
        var separator = new Content(", ");
        var values = ImmutableArray.Create((string?)null!, "", (string?)null!);

        var result = Content.Join(separator, values);

        Assert.True(result.IsEmpty);
        Assert.Equal(string.Empty, GetWrittenString(result));
    }

    [Fact]
    public void Join_String_ReadOnlySpan_ReturnsCorrectResult()
    {
        var separator = new Content(" + ");
        ReadOnlySpan<string> values = ["one", "two", "three"];

        var result = Content.Join(separator, values);

        Assert.Equal("one + two + three", GetWrittenString(result));
    }

    [Fact]
    public void Join_IEnumerable_Content_EmptyEnumerable_ReturnsEmpty()
    {
        var separator = new Content(", ");
        var values = Enumerable.Empty<Content>();

        var result = Content.Join(separator, values);

        Assert.True(result.IsEmpty);
        Assert.Equal(string.Empty, GetWrittenString(result));
    }

    [Fact]
    public void Join_IEnumerable_Content_SingleElement_ReturnsSingleElement()
    {
        var separator = new Content(", ");
        var values = new[] { new Content("single") };

        var result = Content.Join(separator, values);

        Assert.Equal("single", GetWrittenString(result));
    }

    [Fact]
    public void Join_IEnumerable_Content_MultipleElements_ReturnsJoinedContent()
    {
        var separator = new Content(" <-> ");
        var values = new[]
        {
            new Content("first"),
            new Content("second"),
            new Content("third")
        };

        var result = Content.Join(separator, values);

        Assert.Equal("first <-> second <-> third", GetWrittenString(result));
    }

    [Fact]
    public void Join_IEnumerable_Content_WithEmptyElements_SkipsEmpty()
    {
        var separator = new Content(":");
        var values = new[]
        {
            new Content("start"),
            Content.Empty,
            new Content("middle"),
            Content.Empty,
            new Content("end")
        };

        var result = Content.Join(separator, values);

        Assert.Equal("start:middle:end", GetWrittenString(result));
    }

    [Fact]
    public void Join_IEnumerable_ReadOnlyMemory_EmptyEnumerable_ReturnsEmpty()
    {
        var separator = new Content(", ");
        var values = Enumerable.Empty<ReadOnlyMemory<char>>();

        var result = Content.Join(separator, values);

        Assert.True(result.IsEmpty);
        Assert.Equal(string.Empty, GetWrittenString(result));
    }

    [Fact]
    public void Join_IEnumerable_ReadOnlyMemory_SingleElement_ReturnsSingleElement()
    {
        var separator = new Content(", ");
        var values = new[] { "single".AsMemory() };

        var result = Content.Join(separator, values);

        Assert.Equal("single", GetWrittenString(result));
    }

    [Fact]
    public void Join_IEnumerable_ReadOnlyMemory_MultipleElements_ReturnsJoinedContent()
    {
        var separator = new Content(" -> ");
        var values = new[]
        {
            "alpha".AsMemory(),
            "beta".AsMemory(),
            "gamma".AsMemory()
        };

        var result = Content.Join(separator, values);

        Assert.Equal("alpha -> beta -> gamma", GetWrittenString(result));
    }

    [Fact]
    public void Join_IEnumerable_ReadOnlyMemory_WithEmptyMemories_SkipsEmpty()
    {
        var separator = new Content(".");
        var values = new[]
        {
            "a".AsMemory(),
            ReadOnlyMemory<char>.Empty,
            "b".AsMemory(),
            ReadOnlyMemory<char>.Empty,
            "c".AsMemory()
        };

        var result = Content.Join(separator, values);

        Assert.Equal("a.b.c", GetWrittenString(result));
    }

    [Fact]
    public void Join_IEnumerable_String_EmptyEnumerable_ReturnsEmpty()
    {
        var separator = new Content(", ");
        var values = Enumerable.Empty<string>();

        var result = Content.Join(separator, values);

        Assert.True(result.IsEmpty);
        Assert.Equal(string.Empty, GetWrittenString(result));
    }

    [Fact]
    public void Join_IEnumerable_String_SingleElement_ReturnsSingleElement()
    {
        var separator = new Content(", ");
        var values = new[] { "single" };

        var result = Content.Join(separator, values);

        Assert.Equal("single", GetWrittenString(result));
    }

    [Fact]
    public void Join_IEnumerable_String_MultipleElements_ReturnsJoinedContent()
    {
        var separator = new Content(" / ");
        var values = new[] { "path", "to", "file" };

        var result = Content.Join(separator, values);

        Assert.Equal("path / to / file", GetWrittenString(result));
    }

    [Fact]
    public void Join_IEnumerable_String_WithNullAndEmptyStrings_SkipsNullsAndEmpty()
    {
        var separator = new Content(",");
        var values = new[] { "a", null!, "", "b", null!, "c" };

        var result = Content.Join(separator, values);

        Assert.Equal("a,b,c", GetWrittenString(result));
    }

    [Fact]
    public void Join_IEnumerable_String_AllNullAndEmpty_ReturnsEmpty()
    {
        var separator = new Content(", ");
        var values = new[] { (string?)null!, "", (string?)null! };

        var result = Content.Join(separator, values);

        Assert.True(result.IsEmpty);
        Assert.Equal(string.Empty, GetWrittenString(result));
    }

    [Fact]
    public void Join_SpecialCharacters_InSeparator_PreservesCharacters()
    {
        var separator = new Content("\n\t");
        var values = ImmutableArray.Create("line1", "line2", "line3");

        var result = Content.Join(separator, values);

        Assert.Equal("line1\n\tline2\n\tline3", GetWrittenString(result));
    }

    [Fact]
    public void Join_SpecialCharacters_InValues_PreservesCharacters()
    {
        var separator = new Content(" | ");
        var values = ImmutableArray.Create(
            new Content("Hello\nWorld"),
            new Content("Tab\tSeparated"),
            new Content("Quote\"Mark")
        );

        var result = Content.Join(separator, values);

        Assert.Equal("Hello\nWorld | Tab\tSeparated | Quote\"Mark", GetWrittenString(result));
    }

    [Fact]
    public void Join_LargeNumberOfElements_HandlesCorrectly()
    {
        var separator = new Content(",");
        var values = new Content[100];

        for (var i = 0; i < 100; i++)
        {
            values[i] = new Content(i.ToString());
        }

        var result = Content.Join(separator, values.ToImmutableArray());

        var expected = string.Join(",", Enumerable.Range(0, 100).Select(i => i.ToString()));
        Assert.Equal(expected, GetWrittenString(result));
    }

    [Fact]
    public void Join_NestedContent_FlattensCorrectly()
    {
        var separator = new Content(" - ");
        var values = ImmutableArray.Create(
            new Content("Simple"),
            new Content(ImmutableArray.Create(
                new Content("Nested"),
                new Content("Content")
            )),
            new Content("Final")
        );

        var result = Content.Join(separator, values);

        Assert.Equal("Simple - NestedContent - Final", GetWrittenString(result));
    }

    [Fact]
    public void Join_MixedSeparatorTypes_StringSeparator_ReturnsCorrectResult()
    {
        var separator = new Content(" | ");
        var values = ImmutableArray.Create(
            new Content("String"),
            new Content("Memory".AsMemory()),
            new Content(ImmutableArray.Create("Array", "Parts"))
        );

        var result = Content.Join(separator, values);

        Assert.Equal("String | Memory | ArrayParts", GetWrittenString(result));
    }

    [Fact]
    public void Join_MixedSeparatorTypes_MemorySeparator_ReturnsCorrectResult()
    {
        var separator = new Content(" <> ".AsMemory());
        var values = ImmutableArray.Create("one", "two", "three");

        var result = Content.Join(separator, values);

        Assert.Equal("one <> two <> three", GetWrittenString(result));
    }

    [Fact]
    public void Join_MixedSeparatorTypes_ArraySeparator_ReturnsCorrectResult()
    {
        var separator = new Content(ImmutableArray.Create(" ", "<", "> "));
        var values = ImmutableArray.Create(
            "first".AsMemory(),
            "second".AsMemory(),
            "third".AsMemory()
        );

        var result = Content.Join(separator, values);

        Assert.Equal("first <> second <> third", GetWrittenString(result));
    }

    [Fact]
    public void Join_EmptyStringElements_IncludesEmptyStrings()
    {
        var separator = new Content(",");
        var values = ImmutableArray.Create("a", "", "b", "", "c");

        var result = Content.Join(separator, values);

        Assert.Equal("a,b,c", GetWrittenString(result)); // Empty strings are skipped
    }

    [Fact]
    public void Join_EmptyMemoryElements_SkipsEmptyMemories()
    {
        var separator = new Content(",");
        var values = ImmutableArray.Create(
            "a".AsMemory(),
            ReadOnlyMemory<char>.Empty,
            "b".AsMemory(),
            ReadOnlyMemory<char>.Empty,
            "c".AsMemory()
        );

        var result = Content.Join(separator, values);

        Assert.Equal("a,b,c", GetWrittenString(result));
    }

    [Fact]
    public void Join_IEnumerable_LazyEvaluation_WorksCorrectly()
    {
        var separator = new Content(" ");
        var values = Enumerable.Range(1, 3).Select(i => new Content(i.ToString()));

        var result = Content.Join(separator, values);

        Assert.Equal("1 2 3", GetWrittenString(result));
    }

    [Fact]
    public void Join_IEnumerable_SingleItem_OptimizesToDirectReturn()
    {
        var separator = new Content(", ");
        var singleContent = new Content("single");
        var values = new[] { singleContent };

        var result = Content.Join(separator, values);

        Assert.Equal("single", GetWrittenString(result));
        Assert.Equal(singleContent, result);
    }

    [Fact]
    public void Join_ComplexMixedTypes_ReturnsCorrectResult()
    {
        var separator = new Content(" :: ");
        var values = new[]
        {
            new Content("PlainString"),
            new Content("MemoryContent".AsMemory()),
            new Content(ImmutableArray.Create("Multi", "Part", "String")),
            new Content(ImmutableArray.Create("Mem1".AsMemory(), "Mem2".AsMemory())),
            new Content(ImmutableArray.Create(
                new Content("Nested"),
                new Content("Content")
            ))
        };

        var result = Content.Join(separator, values);

        Assert.Equal("PlainString :: MemoryContent :: MultiPartString :: Mem1Mem2 :: NestedContent", GetWrittenString(result));
    }
}
