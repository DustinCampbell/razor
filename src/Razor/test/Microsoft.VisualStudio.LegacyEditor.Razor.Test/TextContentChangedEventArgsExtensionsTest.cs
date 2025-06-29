﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.Editor;
using Microsoft.VisualStudio.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.LegacyEditor.Razor;

public class TextContentChangedEventArgsExtensionsTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    public void TextChangeOccurred_NoChanges_ReturnsFalse()
    {
        // Arrange
        var before = new StringTextSnapshot(string.Empty);
        var after = new StringTextSnapshot(string.Empty);
        var testArgs = new TestTextContentChangedEventArgs(before, after);

        // Act
        var result = testArgs.TextChangeOccurred(out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TextChangeOccurred_CancelingChanges_ReturnsFalse()
    {
        // Arrange
        var before = new StringTextSnapshot("by");
        before.Version.Changes.Add(new TestTextChange(new SourceChange(0, 2, "hi")));
        before.Version.Changes.Add(new TestTextChange(new SourceChange(0, 2, "by")));
        var after = new StringTextSnapshot("by");
        var testArgs = new TestTextContentChangedEventArgs(before, after);

        // Act
        var result = testArgs.TextChangeOccurred(out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TextChangeOccurred_SingleChange_ReturnsTrue()
    {
        // Arrange
        var before = new StringTextSnapshot("by");
        var firstChange = new TestTextChange(new SourceChange(0, 2, "hi"));
        before.Version.Changes.Add(firstChange);
        var after = new StringTextSnapshot("hi");
        var testArgs = new TestTextContentChangedEventArgs(before, after);

        // Act
        var result = testArgs.TextChangeOccurred(out var changeInformation);

        // Assert
        Assert.True(result);
        Assert.Same(firstChange, changeInformation.firstChange);
        Assert.Equal(firstChange, changeInformation.lastChange);
        Assert.Equal("hi", changeInformation.newText);
        Assert.Equal("by", changeInformation.oldText);
    }

    [Fact]
    public void TextChangeOccurred_MultipleChanges_ReturnsTrue()
    {
        // Arrange
        var before = new StringTextSnapshot("by by");
        var firstChange = new TestTextChange(new SourceChange(0, 2, "hi"));
        before.Version.Changes.Add(firstChange);
        var lastChange = new TestTextChange(new SourceChange(3, 2, "hi"));
        before.Version.Changes.Add(lastChange);
        var after = new StringTextSnapshot("hi hi");
        var testArgs = new TestTextContentChangedEventArgs(before, after);

        // Act
        var result = testArgs.TextChangeOccurred(out var changeInformation);

        // Assert
        Assert.True(result);
        Assert.Same(firstChange, changeInformation.firstChange);
        Assert.Equal(lastChange, changeInformation.lastChange);
        Assert.Equal("hi hi", changeInformation.newText);
        Assert.Equal("by by", changeInformation.oldText);
    }

    private class TestTextContentChangedEventArgs(ITextSnapshot before, ITextSnapshot after)
        : TextContentChangedEventArgs(before, after, EditOptions.DefaultMinimalChange, editTag: null)
    {
    }
}
