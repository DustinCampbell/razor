// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using MessagePack.Resolvers;
using MessagePack;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit.Abstractions;
using Microsoft.AspNetCore.Razor.Serialization.MessagePack.Resolvers;
using Xunit;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.ProjectEngineHost.Test.Serialization;

public class CSharpParseOptionsSerializationTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private static readonly MessagePackSerializerOptions s_options = MessagePackSerializerOptions.Standard
        .WithResolver(CompositeResolver.Create(
            CSharpParseOptionsResolver.Instance,
            StandardResolver.Instance));

    [Fact]
    public void RoundTrip()
    {
        var parseOptions = new CSharpParseOptions(
            LanguageVersion.CSharp11,
            DocumentationMode.Parse,
            SourceCodeKind.Regular,
            preprocessorSymbols: ["DEBUG", "RELEASE", "DISCO"]);

        parseOptions = parseOptions.WithFeatures([
            KeyValuePair.Create("CoolFeature", "on"),
            KeyValuePair.Create("SuperCoolFeature", "off")]);

        var bytes = MessagePackConvert.Serialize(parseOptions, s_options);
        var roundtrippedParseOptions = MessagePackConvert.Deserialize<CSharpParseOptions>(bytes, s_options);

        Assert.NotSame(parseOptions, roundtrippedParseOptions);
        Assert.Equal(parseOptions, roundtrippedParseOptions);
    }
}
