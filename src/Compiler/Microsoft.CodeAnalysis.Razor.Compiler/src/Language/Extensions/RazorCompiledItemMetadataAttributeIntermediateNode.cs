// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

/// <summary>
/// An <see cref="ExtensionIntermediateNode"/> that generates code for <c>RazorCompiledItemMetadataAttribute</c>.
/// </summary>
public class RazorCompiledItemMetadataAttributeIntermediateNode : ExtensionIntermediateNode
{
    /// <summary>
    /// Gets the attribute key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the attribute value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets an optional string syntax for the <see cref="Value"/>
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

    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public override void Accept(IntermediateNodeVisitor visitor)
        => AcceptExtensionNode(this, visitor);

    public override void WriteNode(CodeTarget target, CodeRenderingContext context)
    {
        if (!TryGetExtensionAndReportIfMissing<IMetadataAttributeTargetExtension>(target, context, out var extension))
        {
            return;
        }

        extension.WriteRazorCompiledItemMetadataAttribute(context, this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteProperty(nameof(Key), Key);
        formatter.WriteProperty(nameof(Value), Value);
        formatter.WriteProperty(nameof(ValueStringSyntax), ValueStringSyntax);
    }
}
