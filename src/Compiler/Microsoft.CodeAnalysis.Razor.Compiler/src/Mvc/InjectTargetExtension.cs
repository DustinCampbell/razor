﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Extensions;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public class InjectTargetExtension(bool considerNullabilityEnforcement) : IInjectTargetExtension
{
    private const string RazorInjectAttribute = "[global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]";

    public void WriteInjectProperty(CodeRenderingContext context, InjectIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (!context.Options.DesignTime && node.TypeSource is SourceSpan typeSource && !typeSource.FilePath.IsNullOrWhiteSpace())
        {
            if (node.TypeName == "")
            {
                // if we don't even have a type name, just emit an empty mapped region so that intellisense still works
                using (context.BuildEnhancedLinePragma(typeSource))
                {
                }
            }
            else
            {
                context.CodeWriter.WriteLine(RazorInjectAttribute);
                var memberName = node.MemberName ?? "Member_" + DefaultTagHelperTargetExtension.GetDeterministicId(context);
                context.CodeWriter.WriteAutoPropertyDeclaration(["public"], node.TypeName, memberName, typeSource, node.MemberSource, context, privateSetter: true, defaultValue: true);
            }
        }
        else if (!node.IsMalformed)
        {
            var property = $"public {node.TypeName} {node.MemberName} {{ get; private set; }}";
            if (considerNullabilityEnforcement && !context.Options.SuppressNullabilityEnforcement)
            {
                property += " = default!;";
            }

            if (node.Source is SourceSpan nodeSource)
            {
                using (context.BuildLinePragma(nodeSource))
                {
                    WriteProperty();
                }
            }
            else
            {
                WriteProperty();
            }

            void WriteProperty()
            {
                if (considerNullabilityEnforcement && !context.Options.SuppressNullabilityEnforcement)
                {
                    context.CodeWriter.WriteLine("#nullable restore");
                }

                context.CodeWriter
                    .WriteLine(RazorInjectAttribute)
                    .WriteLine(property);

                if (considerNullabilityEnforcement && !context.Options.SuppressNullabilityEnforcement)
                {
                    context.CodeWriter.WriteLine("#nullable disable");
                }
            }
        }
    }
}
