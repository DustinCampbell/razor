// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal class MetadataAttributeTargetExtension : IMetadataAttributeTargetExtension
{
    public const string CompiledItemAttributeName = "global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute";
    public const string SourceChecksumAttributeName = "global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute";
    public const string CompiledItemMetadataAttributeName = "global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemMetadataAttribute";

    public void WriteRazorCompiledItemAttribute(CodeRenderingContext context, RazorCompiledItemAttributeIntermediateNode node)
    {
        // [assembly: global::...RazorCompiledItem(typeof({node.TypeName}), @"{node.Kind}", @"{node.Identifier}")]
        context.CodeWriter.WriteLine($"""
            [assembly: {CompiledItemAttributeName}(typeof({node.TypeName}), @"{node.Kind}", @"{node.Identifier}")]
            """);
    }

    public void WriteRazorCompiledItemMetadataAttribute(CodeRenderingContext context, RazorCompiledItemMetadataAttributeIntermediateNode node)
    {
        var writer = context.CodeWriter;

        // [global::...RazorCompiledItemAttribute(@"{node.Key}", @"{node.Value}")]
        writer.Write($"[{CompiledItemMetadataAttributeName}(");
        writer.WriteStringLiteral(node.Key);
        writer.WriteParameterSeparator();

        if (!context.Options.DesignTime && node.Source is SourceSpan nodeSource)
        {
            writer.WriteLine();

            if (!node.ValueStringSyntax.IsEmpty)
            {
                writer.Write("// language=");
                writer.WriteLine(node.ValueStringSyntax);
            }

            using (context.BuildEnhancedLinePragma(nodeSource))
            {
                context.AddSourceMappingFor(node);
                writer.WriteStringLiteral(node.Value);
            }
        }
        else
        {
            writer.WriteStringLiteral(node.Value);
        }

        writer.WriteLine(")]");
    }

    public void WriteRazorSourceChecksumAttribute(CodeRenderingContext context, RazorSourceChecksumAttributeIntermediateNode node)
    {
        // [global::...RazorSourceChecksum(@"{node.ChecksumAlgorithm}", @"{node.Checksum}", @"{node.Identifier}")]
        context.CodeWriter.WriteLine($"""
            [{SourceChecksumAttributeName}(@"{node.ChecksumAlgorithm}", @"{ChecksumUtilities.BytesToString(node.Checksum)}", @"{node.Identifier}")]
            """);
    }
}
