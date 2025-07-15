// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal sealed class RazorCompiledItemAttributeIntermediateNode : ExtensionIntermediateNode
{
    public required Content TypeName { get; init; }
    public required Content Kind { get; init; }
    public required Content Identifier { get; init; }

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

        extension.WriteRazorCompiledItemAttribute(context, this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteProperty(nameof(Identifier), Identifier);
        formatter.WriteProperty(nameof(Kind), Kind);
        formatter.WriteProperty(nameof(TypeName), TypeName);
    }
}
