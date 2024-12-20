// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using MessagePack;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Razor.Serialization.MessagePack.Formatters;

internal sealed class CSharpParseOptionsFormatter : NonCachingFormatter<CSharpParseOptions>
{
    public static readonly NonCachingFormatter<CSharpParseOptions> Instance = new CSharpParseOptionsFormatter();

    private CSharpParseOptionsFormatter()
    {
    }

    public override CSharpParseOptions Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var count = reader.ReadArrayHeader();

        // There are at least 5 values:
        // - Kind, DocumentationMode, and LanguageVersion
        // - PreprocessorSymbolNames count
        // - Features count
        Assumed.True(count >= 5);

        // First, read the simple values.
        var kind = (SourceCodeKind)reader.ReadInt32();
        var documentationMode = (DocumentationMode)reader.ReadInt32();
        var languageVersion = (LanguageVersion)reader.ReadInt32();

        count -= 3;

        // Next, read the preprocessor symbol names.
        var preprocessorSymbolNames = reader.ReadArray(ref count,
            static (ref MessagePackReader reader) => reader.ReadString().AssumeNotNull());

        // Finally, read the features map.
        var features = reader.ReadArray(ref count,
            static (ref MessagePackReader reader) =>
            {
                var key = reader.ReadString().AssumeNotNull();
                var value = reader.ReadString();

                return KeyValuePair.Create(key, value);
            });

        Assumed.True(count == 0);

        var result = new CSharpParseOptions(languageVersion, documentationMode, kind, preprocessorSymbolNames);

        if (features.Length > 0)
        {
            result = result.WithFeatures(features);
        }

        return result;
    }

    public override void Serialize(ref MessagePackWriter writer, CSharpParseOptions value, MessagePackSerializerOptions options)
    {
        ReadOnlySpan<string> preprocessorSymbolNames = [.. value.PreprocessorSymbolNames];

        // Sort the features map for determinism.
        var features = value.Features.OrderByAsArray(x => x.Key);

        // Compute the number of elements that we're about to write.
        // Start with Kind + DocumentationMode + SpecifiedLanguageVersion
        var count = 3;

        // Add PreprocessorSymbolNames: Count + Contents
        count += 1 + preprocessorSymbolNames.Length;

        // Add features: Count + Contents (keys and values) 
        count += 1 + (features.Length * 2);

        writer.WriteArrayHeader(count);

        // First, write the simple values.
        writer.Write((int)value.Kind);
        writer.Write((int)value.DocumentationMode);
        writer.Write((int)value.SpecifiedLanguageVersion);

        // Next, write the preprocessor symbol names.
        writer.Write(preprocessorSymbolNames.Length);
        foreach (var name in preprocessorSymbolNames)
        {
            writer.Write(name);
        }

        // Finally, write the features map.
        writer.Write(features.Length);
        foreach (var kvp in features)
        {
            writer.Write(kvp.Key);
            writer.Write(kvp.Value);
        }
    }
}
