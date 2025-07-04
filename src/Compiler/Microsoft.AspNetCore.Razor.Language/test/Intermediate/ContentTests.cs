// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
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
}
