﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.Razor.Serialization;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.AspNetCore.Razor.Test.Common.TagHelperTestData;

namespace Microsoft.CodeAnalysis.Remote.Razor;

public class TagHelperDeltaResultTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    public void Apply_Noop()
    {
        // Arrange
        var delta = new TagHelperDeltaResult(IsDelta: true, ResultId: 1337, ImmutableArray<Checksum>.Empty, ImmutableArray<Checksum>.Empty);

        // Act
        var checksums = delta.Apply(Project1TagHelperChecksums);

        // Assert
        Assert.Equal<Checksum>(Project1TagHelperChecksums, checksums);
    }

    [Fact]
    public void Apply_Added()
    {
        // Arrange
        var delta = new TagHelperDeltaResult(IsDelta: true, ResultId: 1337, Project1TagHelperChecksums, ImmutableArray<Checksum>.Empty);

        // Act
        var checksums = delta.Apply(Project2TagHelperChecksums);

        // Assert
        Assert.Equal<Checksum>(Project1AndProject2TagHelperChecksums, checksums);
    }

    [Fact]
    public void Apply_Removed()
    {
        // Arrange
        var delta = new TagHelperDeltaResult(IsDelta: true, ResultId: 1337, ImmutableArray<Checksum>.Empty, Project1TagHelperChecksums);

        // Act
        var checksums = delta.Apply(Project1AndProject2TagHelperChecksums);

        // Assert
        Assert.Equal<Checksum>(Project2TagHelperChecksums, checksums);
    }

    [Fact]
    public void Apply_AddAndRemoved()
    {
        // Arrange
        var delta = new TagHelperDeltaResult(IsDelta: true, ResultId: 1337, Project1TagHelperChecksums, Project2TagHelperChecksums);

        // Act
        var checksums = delta.Apply(Project2TagHelperChecksums);

        // Assert
        Assert.Equal<Checksum>(Project1TagHelperChecksums, checksums);
    }
}
