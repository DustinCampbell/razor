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
    /// Gets or sets the attribute key.
    /// </summary>
    public required Content Key { get; init; }

    /// <summary>
    /// Gets or sets the attribute value.
    /// </summary>
    public required Content Value { get; init; }

    /// <summary>
    /// Gets or sets an optional string syntax for the <see cref="Value"/>
    /// </summary>
    public Content ValueStringSyntax { get; init; }

    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public override void Accept(IntermediateNodeVisitor visitor)
        => AcceptExtensionNode(this, visitor);

    public override void WriteNode(CodeTarget target, CodeRenderingContext context)
    {
        var extension = target.GetExtension<IMetadataAttributeTargetExtension>();
        if (extension == null)
        {
            ReportMissingCodeTargetExtension<IMetadataAttributeTargetExtension>(context);
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
