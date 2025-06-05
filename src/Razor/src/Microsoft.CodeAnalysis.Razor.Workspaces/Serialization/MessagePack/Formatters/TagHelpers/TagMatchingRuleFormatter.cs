// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using MessagePack;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.Serialization.MessagePack.Formatters.TagHelpers;

internal sealed class TagMatchingRuleFormatter : ValueFormatter<TagMatchingRuleDescriptor>
{
    public static readonly ValueFormatter<TagMatchingRuleDescriptor> Instance = new TagMatchingRuleFormatter();

    private TagMatchingRuleFormatter()
    {
    }

    public override TagMatchingRuleDescriptor Deserialize(ref MessagePackReader reader, SerializerCachingOptions options)
    {
        reader.ReadArrayHeaderAndVerify(5);

        var flags = (TagMatchingRuleFlags)reader.ReadByte();
        var tagName = CachedStringFormatter.Instance.Deserialize(ref reader, options).AssumeNotNull();
        var parentTag = CachedStringFormatter.Instance.Deserialize(ref reader, options);
        var attributes = reader.Deserialize<ImmutableArray<RequiredAttributeDescriptor>>(options);
        var diagnostics = reader.Deserialize<ImmutableArray<RazorDiagnostic>>(options);

        return new TagMatchingRuleDescriptor(flags, tagName, parentTag, attributes, diagnostics);
    }

    public override void Serialize(ref MessagePackWriter writer, TagMatchingRuleDescriptor value, SerializerCachingOptions options)
    {
        writer.WriteArrayHeader(5);

        writer.Write((byte)value.Flags);
        CachedStringFormatter.Instance.Serialize(ref writer, value.TagName, options);
        CachedStringFormatter.Instance.Serialize(ref writer, value.ParentTag, options);
        writer.Serialize(value.Attributes, options);
        writer.Serialize(value.Diagnostics, options);
    }

    public override void Skim(ref MessagePackReader reader, SerializerCachingOptions options)
    {
        reader.ReadArrayHeaderAndVerify(5);

        reader.Skip(); // Flags
        CachedStringFormatter.Instance.Skim(ref reader, options); // TagName
        CachedStringFormatter.Instance.Skim(ref reader, options); // ParentTag
        RequiredAttributeFormatter.Instance.SkimArray(ref reader, options); // Attributes
        RazorDiagnosticFormatter.Instance.SkimArray(ref reader, options); // Diagnostics
    }
}
