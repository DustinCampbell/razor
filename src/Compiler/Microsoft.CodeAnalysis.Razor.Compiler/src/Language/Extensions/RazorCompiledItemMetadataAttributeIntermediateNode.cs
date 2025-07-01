// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

/// <summary>
/// An <see cref="ExtensionIntermediateNode"/> that generates code for <c>RazorCompiledItemMetadataAttribute</c>.
/// </summary>
public sealed class RazorCompiledItemMetadataAttributeIntermediateNode : ExtensionIntermediateNode
{
    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    /// <summary>
    /// Gets or sets the attribute key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets or sets the attribute value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets or sets an optional string syntax for the <see cref="Value"/>
    /// </summary>
    public string? ValueStringSyntax { get; }

    public RazorCompiledItemMetadataAttributeIntermediateNode(
        string key, string value, string? valueStringSyntax = null, SourceSpan? source = null)
    {
        Key = key;
        Value = value;
        ValueStringSyntax = valueStringSyntax;
        Source = source;
    }

    public override void Accept(IntermediateNodeVisitor visitor)
        => AcceptExtensionNode(this, visitor);

    public override void WriteNode(CodeTarget target, CodeRenderingContext context)
    {
        var writer = context.CodeWriter;

        // [assembly: global::...RazorCompiledItemAttribute(@"{Key}", @"{Value}")]
        writer.Write($"[{Constants.RazorCompiledItemMetadataAttributeTypeName}(");
        writer.WriteStringLiteral(Key);
        writer.Write(", ");

        if (!context.Options.DesignTime && Source is SourceSpan source)
        {
            writer.WriteLine();

            if (ValueStringSyntax is string valueString)
            {
                writer.WriteLine($"// language={valueString}");
            }

            using (writer.BuildEnhancedLinePragma(source, context))
            {
                context.AddSourceMappingFor(this);
                writer.WriteStringLiteral(Value);
            }
        }
        else
        {
            writer.WriteStringLiteral(Value);
        }

        writer.WriteLine(")]");
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteProperty(nameof(Key), Key);
        formatter.WriteProperty(nameof(Value), Value);
        formatter.WriteProperty(nameof(ValueStringSyntax), ValueStringSyntax);
    }
}
