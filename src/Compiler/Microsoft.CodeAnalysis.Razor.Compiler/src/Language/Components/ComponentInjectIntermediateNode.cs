// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class ComponentInjectIntermediateNode(
    string typeName,
    string? memberName,
    SourceSpan? typeSpan,
    SourceSpan? memberSpan,
    bool isMalformed)
    : ExtensionIntermediateNode
{
    private static readonly ImmutableArray<string> s_injectedPropertyModifiers =
    [
        $"[global::{ComponentsApi.InjectAttribute.FullTypeName}]",
        "private" // Encapsulation is the default
    ];

    public string TypeName { get; } = typeName;
    public string? MemberName { get; } = memberName;
    public SourceSpan? TypeSpan { get; } = typeSpan;
    public SourceSpan? MemberSpan { get; } = memberSpan;
    public bool IsMalformed { get; } = isMalformed;

    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public override void Accept(IntermediateNodeVisitor visitor)
        => AcceptExtensionNode(this, visitor);

    public override void WriteNode(CodeTarget target, CodeRenderingContext context)
    {
        if (TypeName == string.Empty && TypeSpan.HasValue && !context.Options.DesignTime)
        {
            // if we don't even have a type name, just emit an empty mapped region so that intellisense still works
            context.CodeWriter.BuildEnhancedLinePragma(TypeSpan.Value, context).Dispose();
        }
        else
        {
            if (!context.Options.DesignTime || !IsMalformed)
            {
                var memberName = MemberName ?? "Member_" + DefaultTagHelperTargetExtension.GetDeterministicId(context);

                context.CodeWriter.WriteAutoPropertyDeclaration(
                    s_injectedPropertyModifiers,
                    TypeName,
                    memberName,
                    TypeSpan,
                    MemberSpan,
                    context,
                    defaultValue: true);
            }
        }
    }
}
