// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public sealed class DefaultTagHelperCreateIntermediateNode(string fieldName, string typeName) : ExtensionIntermediateNode
{
    public string FieldName { get; } = fieldName;
    public string TypeName { get; } = typeName;

    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public override void Accept(IntermediateNodeVisitor visitor)
        => AcceptExtensionNode(this, visitor);

    public override void WriteNode(CodeTarget target, CodeRenderingContext context)
    {
        if (!TryGetExtensionAndReportIfMissing<IDefaultTagHelperTargetExtension>(target, context, out var extension))
        {
            return;
        }

        extension.WriteTagHelperCreate(context, this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(TypeName);

        formatter.WriteProperty(nameof(FieldName), FieldName);
        formatter.WriteProperty(nameof(TypeName), TypeName);
    }
}
