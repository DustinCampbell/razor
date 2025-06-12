// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public abstract class ExtensionIntermediateNode : IntermediateNode
{
    public abstract void WriteNode(CodeTarget target, CodeRenderingContext context);

    protected static void AcceptExtensionNode<TNode>(TNode node, IntermediateNodeVisitor visitor)
        where TNode : ExtensionIntermediateNode
    {
        if (visitor is IExtensionIntermediateNodeVisitor<TNode> typedVisitor)
        {
            typedVisitor.VisitExtension(node);
        }
        else
        {
            visitor.VisitExtension(node);
        }
    }

    protected static bool TryGetExtensionAndReportIfMissing<T>(
        CodeTarget target, CodeRenderingContext context, [NotNullWhen(true)] out T? result)
        where T : class, ICodeTargetExtension
    {
        if (target.GetExtension<T>() is T extension)
        {
            result = extension;
            return true;
        }

        ReportMissingCodeTargetExtension<T>(context);

        result = null;
        return false;
    }

    protected static void ReportMissingCodeTargetExtension<TDependency>(CodeRenderingContext context)
    {
        context.AddDiagnostic(
            RazorDiagnosticFactory.CreateCodeTarget_UnsupportedExtension(
                context.DocumentKind ?? string.Empty,
                typeof(TDependency)));
    }
}
