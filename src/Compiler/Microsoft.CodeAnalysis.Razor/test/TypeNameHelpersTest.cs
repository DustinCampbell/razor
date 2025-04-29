// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor;

public class TypeNameHelpersTest
{
    [Theory]
    [InlineData("C", 0)]
    [InlineData("T", 0)]
    [InlineData("T[]", 1)]
    [InlineData("T[][]", 1)]
    [InlineData("(T, T)[]", 2)]
    [InlineData("(T X, T Y)[]", 2)]
    [InlineData("(T[], T)[]", 2)]
    [InlineData("(T[] X, T Y)[]", 2)]
    [InlineData("C<T>", 1)]
    [InlineData("C<T[]>", 1)]
    [InlineData("C<T[][]>", 1)]
    [InlineData("C<(T, T)[]>", 2)]
    [InlineData("C<(T X, T Y)[]>", 2)]
    [InlineData("C<(T[], T)[]>", 2)]
    [InlineData("C<(T[] X, T Y)[]>", 2)]
    [InlineData("C<D<T>>", 1), WorkItem("https://github.com/dotnet/razor/issues/9631")]
    [InlineData("C<D<T>[]>>", 1)]
    [InlineData("C<D<T[]>>", 1)]
    [InlineData("C<NS.T>", 0)]
    public void ParseTypeArguments(string input, int expectedNumberOfTs)
    {
        // Act.
        var parsed = TypeNameHelpers.ParseTypeArguments(input);

        // Assert.
        Assert.Equal(Enumerable.Repeat("T", expectedNumberOfTs), parsed);
    }

    [Theory]
    [InlineData("TItem2", "Type2")]

    // Unspecified argument -> object
    [InlineData("TItem3", "object")]

    // Not a type parameter
    [InlineData("TItem4", "TItem4")]

    // In a qualified name, not a type parameter
    [InlineData("TItem1.TItem2", "TItem1.TItem2")]

    // Type parameters can't have type parameters
    [InlineData("TItem1.TItem2<TItem1, TItem2, TItem3>", "TItem1.TItem2<Type1, Type2, object>")]
    [InlineData("TItem2<TItem1<TItem3>, System.TItem2, RenderFragment<List<TItem1>>", "TItem2<TItem1<object>, System.TItem2, RenderFragment<List<Type1>>")]

    // Tuples
    [InlineData("List<(TItem1 X, TItem2 Y)>", "List<(Type1 X, Type2 Y)>")]
    [InlineData("List<(TItem1, TItem2)>", "List<(Type1, Type2)>")]
    [InlineData("List<(TItem1/*test*/,TItem2)>", "List<(Type1/*test*/,Type2)>")]
    [InlineData("List<(TItem1/*test*/X, TItem2 Y)>", "List<(Type1/*test*/X, Type2 Y)>")]
    [InlineData("""
        List<(TItem1 X // Test
        , TItem2 Y)>
        """,
    """
        List<(Type1 X // Test
        , Type2 Y)>
        """)]
    [InlineData("""
        List<(TItem1// Test
        X, TItem2 Y)>
        """,
    """
        List<(Type1// Test
        X, Type2 Y)>
        """)]
    [InlineData("""
        List<(TItem1
        X, TItem2 Y)>
        """,
    """
        List<(Type1
        X, Type2 Y)>
        """)]
    [InlineData("""
        List<(TItem1 X /* Test
        another line */,
        TItem2 Y)>
        """,
    """
        List<(Type1 X /* Test
        another line */,
        Type2 Y)>
        """)]
    public void CreateGenericTypeNameRewriter_CanReplaceTypeParametersWithTypeArguments(string original, string expected)
    {
        // Arrange
        var visitor = TypeNameHelpers.CreateGenericTypeRewriter(new Dictionary<string, string>()
            {
                { "TItem1", "Type1" },
                { "TItem2", "Type2" },
                { "TItem3", null! },
            });

        // Act
        var actual = visitor.Rewrite(original);

        // Assert
        Assert.Equal(expected, actual.ToString());
    }

    [Theory]
    [InlineData("String", "global::String")]
    [InlineData("System.String", "global::System.String")]
    [InlineData("TItem2", "TItem2")]
    [InlineData("System.Collections.Generic.List<String>", "global::System.Collections.Generic.List<global::String>")]
    [InlineData("System.Collections.Generic.List<System.String>", "global::System.Collections.Generic.List<global::System.String>")]
    [InlineData("System.Collections.Generic.Dictionary<System.String, TItem1>", "global::System.Collections.Generic.Dictionary<global::System.String, TItem1>")]
    [InlineData("System.Collections.Generic.Dictionary<dynamic, TItem1>", "global::System.Collections.Generic.Dictionary<dynamic, TItem1>")]
    [InlineData("System.Collections.TItem3.Dictionary<System.String, TItem1>", "global::System.Collections.TItem3.Dictionary<global::System.String, TItem1>")]
    [InlineData("System.Collections.TItem3.TItem1<System.String, TItem1>", "global::System.Collections.TItem3.TItem1<global::System.String, TItem1>")]
    [InlineData("M.RenderFragment<(N.MyClass I1, N.MyStruct I2, TItem1 P)>", "global::M.RenderFragment<(global::N.MyClass I1, global::N.MyStruct I2, TItem1 P)>")]

    // This case is interesting because we know TITem2 to be a generic type parameter,
    // and we know that this will never be valid, which is why we don't bother rewriting.
    [InlineData("TItem2<System.String, TItem1>", "TItem2<global::System.String, TItem1>")]
    public void CreateGlobalQualifiedTypeNameRewriter_CanQualifyNames(string original, string expected)
    {
        // Arrange
        var visitor = TypeNameHelpers.CreateGlobalQualifiedTypeNameRewriter(["TItem1", "TItem2", "TItem3"]);

        // Act
        var actual = visitor.Rewrite(original);

        // Assert
        Assert.Equal(expected, actual.ToString());
    }
}
