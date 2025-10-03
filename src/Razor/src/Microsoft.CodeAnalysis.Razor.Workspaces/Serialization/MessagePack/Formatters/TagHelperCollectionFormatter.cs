// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using MessagePack;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.Razor.Serialization.MessagePack.Formatters.TagHelpers;
using Microsoft.CodeAnalysis.Razor.Utilities;

namespace Microsoft.CodeAnalysis.Razor.Serialization.MessagePack.Formatters;

internal sealed class TagHelperCollectionFormatter : ValueFormatter<TagHelperCollection>
{
    // Properties: Count, Checksum array, TagHelperDescriptor array
    private const int PropertyCount = 3;

    public static readonly ValueFormatter<TagHelperCollection> Instance = new TagHelperCollectionFormatter();

    private TagHelperCollectionFormatter()
    {
    }

    public override TagHelperCollection Deserialize(ref MessagePackReader reader, SerializerCachingOptions options)
    {
        reader.ReadArrayHeaderAndVerify(PropertyCount);

        // 1. Read count
        var count = reader.ReadInt32();

        // 2. Read checksum array
        reader.ReadArrayHeaderAndVerify(count);

        using var checksums = new MemoryBuilder<Checksum>(count);

        for (var i = 0; i < count; i++)
        {
            var checksum = reader.Deserialize<Checksum>(options);
            checksums.Append(checksum);
        }

        // 3. Read tag helper array, skimming past any we already have cached.
        reader.ReadArrayHeaderAndVerify(count);

        using var builder = new TagHelperCollection.RefBuilder(capacity: count);
        var cache = TagHelperCache.Default;

        foreach (var checksum in checksums.AsMemory().Span)
        {
            if (!cache.TryGet(checksum, out var tagHelper))
            {
                tagHelper = TagHelperFormatter.Instance.Deserialize(ref reader, options);
                cache.TryAdd(checksum, tagHelper);
            }
            else
            {
                TagHelperFormatter.Instance.Skim(ref reader, options);
            }

            builder.Add(tagHelper);
        }

        return builder.ToCollection();
    }

    public override void Serialize(ref MessagePackWriter writer, TagHelperCollection value, SerializerCachingOptions options)
    {
        writer.WriteArrayHeader(PropertyCount);

        // 1. Write count
        writer.WriteInt32(value.Count);

        // 2. Write checksum array
        writer.WriteArrayHeader(value.Count);

        foreach (var item in value)
        {
            writer.Serialize(item.Checksum, options);
        }

        // 3. Write tag helper array
        writer.WriteArrayHeader(value.Count);

        foreach (var item in value)
        {
            writer.Serialize(item, options);
        }
    }
}
