// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal sealed class RazorCompiledItemAttributeIntermediateNode : ExtensionIntermediateNode
{
    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public string TypeName { get; set; }

    public string Kind { get; set; }

    public string Identifier { get; set; }

    public override void Accept(IntermediateNodeVisitor visitor)
        => AcceptExtensionNode(this, visitor);

    public override void WriteNode(CodeTarget target, CodeRenderingContext context)
    {
        if (!TryGetExtensionAndReportIfMissing<IMetadataAttributeTargetExtension>(target, context, out var extension))
        {
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
