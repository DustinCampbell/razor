// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using static Microsoft.AspNetCore.Razor.Language.Extensions.Constants;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal sealed class PreallocatedTagHelperHtmlAttributeIntermediateNode(string variableName) : ExtensionIntermediateNode
{
    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public string VariableName { get; } = variableName;

    public override void Accept(IntermediateNodeVisitor visitor)
        => AcceptExtensionNode(this, visitor);

    public override void WriteNode(CodeTarget target, CodeRenderingContext context)
    {
        Debug.Assert(
            context.Parent is TagHelperIntermediateNode,
            message: Resources.FormatIntermediateNodes_InvalidParentNode(GetType(), typeof(TagHelperIntermediateNode)));

        context.CodeWriter
            .WriteStartInstanceMethodInvocation(ExecutionContextVariableName, ExecutionContextAddHtmlAttributeMethodName)
            .Write(VariableName)
            .WriteEndMethodInvocation();
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(VariableName);

        formatter.WriteProperty(nameof(VariableName), VariableName);
    }
}
