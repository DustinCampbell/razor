// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public sealed class SectionIntermediateNode(string sectionName) : ExtensionIntermediateNode
{
    public string SectionName { get; } = sectionName;

    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public override void Accept(IntermediateNodeVisitor visitor)
        => AcceptExtensionNode(this, visitor);

    public override void WriteNode(CodeTarget target, CodeRenderingContext context)
    {
        if (!TryGetExtensionAndReportIfMissing<ISectionTargetExtension>(target, context, out var extension))
        {
            return;
        }

        extension.WriteSection(context, this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(SectionName);

        formatter.WriteProperty(nameof(SectionName), SectionName);
    }
}
