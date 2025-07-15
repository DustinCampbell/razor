// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal sealed class RazorSourceChecksumAttributeIntermediateNode : ExtensionIntermediateNode
{
    public required ImmutableArray<byte> Checksum { get; init; }
    public required SourceHashAlgorithm ChecksumAlgorithm { get; init; }
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

        extension.WriteRazorSourceChecksumAttribute(context, this);
    }
}
