// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class RazorFileKindTest
{
    [Fact]
    public void ValuesAreSequential()
    {
        var values =
#if NET
            Enum.GetValues<RazorFileKind>();
#else
            (RazorFileKind[])Enum.GetValues(typeof(RazorFileKind));
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
            Enum.GetValues<RazorFileKind>();
#else
            (RazorFileKind[])Enum.GetValues(typeof(RazorFileKind));
#endif

        var maxValue = (RazorFileKind)values.Max(static x => (int)x);
        Assert.Equal(RazorFileKind.ComponentImport, maxValue);
        Assert.Equal(RazorFileKinds.MaxValue, maxValue);
    }
}
