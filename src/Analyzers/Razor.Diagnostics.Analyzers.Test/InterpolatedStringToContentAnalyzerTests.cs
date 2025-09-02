// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace Razor.Diagnostics.Analyzers.Test;

using VerifyCS = CSharpAnalyzerVerifier<InterpolatedStringToContentAnalyzer>;

public class InterpolatedStringToContentAnalyzerTests
{
    private const string ContentSource = """
        namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
        {
            internal struct Content
            {
                public static implicit operator Content(string value)
                    => default;
            }
        }
        """;

    [Fact]
    public async Task TestLocalVariable_CSharpAsync()
    {
        var code = $$"""
            using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

            class C
            {
                void Method()
                {
                    Content c = {|RZD004:$"{42} is the answer!"|};
                    System.Console.WriteLine(c.ToString());
                }
            }

            {{ContentSource}}
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
        }.RunAsync();
    }
}
