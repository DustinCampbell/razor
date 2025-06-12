// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public class InjectIntermediateNode(
    string typeName,
    string memberName,
    SourceSpan? typeSource = null,
    SourceSpan? memberSource = null,
    bool isMalformed = false)
    : ExtensionIntermediateNode
{
    public string TypeName { get; } = typeName;
    public SourceSpan? TypeSource { get; } = typeSource;

    public string MemberName { get; } = memberName;
    public SourceSpan? MemberSource { get; } = memberSource;

    public bool IsMalformed { get; } = isMalformed;

    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public override void Accept(IntermediateNodeVisitor visitor)
        => AcceptExtensionNode(this, visitor);

    public override void WriteNode(CodeTarget target, CodeRenderingContext context)
    {
        if (!TryGetExtensionAndReportIfMissing<IInjectTargetExtension>(target, context, out var extension))
        {
            return;
        }

        extension.WriteInjectProperty(context, this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(MemberName);

        formatter.WriteProperty(nameof(MemberName), MemberName);
        formatter.WriteProperty(nameof(TypeName), TypeName);
        formatter.WriteProperty(nameof(IsMalformed), IsMalformed.ToString());
    }
}
