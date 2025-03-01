// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class RazorSourceCodeKindTest
{
    [Fact]
    public void ValuesAreSequential()
    {
        var values =
#if NET
            Enum.GetValues<RazorSourceCodeKind>();
#else
            (RazorSourceCodeKind[])Enum.GetValues(typeof(RazorSourceCodeKind));
#endif

        var lastValue = (int)values[0];
        Assert.True(lastValue == 0, "First enum value should be 0.");

        foreach (var value in values.AsSpan()[1..])
        {
            var current = (int)value;
            Assert.Equal(lastValue + 1, current);

            lastValue = current;
        }
    }

    [Fact]
    public void MaxValueIsComponentImport()
    {
        var values =
#if NET
            Enum.GetValues<RazorSourceCodeKind>();
#else
            (RazorSourceCodeKind[])Enum.GetValues(typeof(RazorSourceCodeKind));
#endif

        var maxValue = (RazorSourceCodeKind)values.Max(static x => (int)x);
        Assert.Equal(RazorSourceCodeKind.ComponentImport, maxValue);
        Assert.Equal(SourceCodeFileKinds.MaxValue, maxValue);
    }
}
