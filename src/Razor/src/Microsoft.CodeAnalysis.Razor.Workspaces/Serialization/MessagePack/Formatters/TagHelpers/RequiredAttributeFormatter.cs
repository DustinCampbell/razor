// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using MessagePack;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using static Microsoft.AspNetCore.Razor.Language.RequiredAttributeDescriptor;

namespace Microsoft.CodeAnalysis.Razor.Serialization.MessagePack.Formatters.TagHelpers;

internal sealed class RequiredAttributeFormatter : ValueFormatter<RequiredAttributeDescriptor>
{
    public static readonly ValueFormatter<RequiredAttributeDescriptor> Instance = new RequiredAttributeFormatter();

    private RequiredAttributeFormatter()
    {
    }

    public override RequiredAttributeDescriptor Deserialize(ref MessagePackReader reader, SerializerCachingOptions options)
    {
        reader.ReadArrayHeaderAndVerify(7);

        var flags = (RequiredAttributeFlags)reader.ReadByte();
        var name = CachedStringFormatter.Instance.Deserialize(ref reader, options);
        var nameComparison = (NameComparisonMode)reader.ReadInt32();
        var value = CachedStringFormatter.Instance.Deserialize(ref reader, options);
        var valueComparison = (ValueComparisonMode)reader.ReadInt32();
        var displayName = CachedStringFormatter.Instance.Deserialize(ref reader, options).AssumeNotNull();

        var diagnostics = reader.Deserialize<ImmutableArray<RazorDiagnostic>>(options);

        return new RequiredAttributeDescriptor(
            flags, name!, nameComparison,
            value, valueComparison,
            displayName, diagnostics);
    }

    public override void Serialize(ref MessagePackWriter writer, RequiredAttributeDescriptor value, SerializerCachingOptions options)
    {
        writer.WriteArrayHeader(7);

        writer.Write((byte)value.Flags);
        CachedStringFormatter.Instance.Serialize(ref writer, value.Name, options);
        writer.Write((int)value.NameComparison);
        CachedStringFormatter.Instance.Serialize(ref writer, value.Value, options);
        writer.Write((int)value.ValueComparison);
        CachedStringFormatter.Instance.Serialize(ref writer, value.DisplayName, options);

        writer.Serialize(value.Diagnostics, options);
    }

    public override void Skim(ref MessagePackReader reader, SerializerCachingOptions options)
    {
        reader.ReadArrayHeaderAndVerify(7);

        reader.Skip(); // Flags
        CachedStringFormatter.Instance.Skim(ref reader, options); // Name
        reader.Skip(); // NameComparison
        CachedStringFormatter.Instance.Skim(ref reader, options); // Value
        reader.Skip(); // ValueComparison
        CachedStringFormatter.Instance.Skim(ref reader, options); // DisplayName

        RazorDiagnosticFormatter.Instance.SkimArray(ref reader, options); // Diagnostics
    }
}
