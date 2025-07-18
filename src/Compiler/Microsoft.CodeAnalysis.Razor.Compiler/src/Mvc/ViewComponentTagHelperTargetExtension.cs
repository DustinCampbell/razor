// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

internal class ViewComponentTagHelperTargetExtension : IViewComponentTagHelperTargetExtension
{
    private static readonly ImmutableArray<Content> s_publicModifiers = ["public"];

    public const string TagHelperTypeName = "Microsoft.AspNetCore.Razor.TagHelpers.TagHelper";
    public const string ViewComponentHelperTypeName = "global::Microsoft.AspNetCore.Mvc.IViewComponentHelper";
    public const string ViewComponentHelperVariableName = "__helper";
    public const string ViewComponentInvokeMethodName = "InvokeAsync";
    public const string HtmlAttributeNotBoundAttributeTypeName = "Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNotBoundAttribute";
    public const string ViewContextAttributeTypeName = "global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewContextAttribute";
    public const string ViewContextTypeName = "global::Microsoft.AspNetCore.Mvc.Rendering.ViewContext";
    public const string ViewContextPropertyName = "ViewContext";
    public const string HtmlTargetElementAttributeTypeName = "Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute";
    public const string TagHelperProcessMethodName = "ProcessAsync";
    public const string TagHelperProcessInvokeAsyncArgsMethodName = "ProcessInvokeAsyncArgs";
    public const string TagHelperContextTypeName = "Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext";
    public const string TagHelperContextVariableName = "__context";
    public const string TagHelperContextAttributesVariableName = "AllAttributes";
    public const string TagHelperOutputTypeName = "Microsoft.AspNetCore.Razor.TagHelpers.TagHelperOutput";
    public const string TagHelperOutputVariableName = "__output";
    public const string TagHelperOutputTagNamePropertyName = "TagName";
    public const string TagHelperOutputContentPropertyName = "Content";
    public const string TagHelperContentSetMethodName = "SetHtmlContent";
    public const string TagHelperContentVariableName = "__helperContent";
    public const string IViewContextAwareTypeName = "global::Microsoft.AspNetCore.Mvc.ViewFeatures.IViewContextAware";
    public const string IViewContextAwareContextualizeMethodName = "Contextualize";

    public void WriteViewComponentTagHelper(CodeRenderingContext context, ViewComponentTagHelperIntermediateNode node)
    {
        // Add target element.
        WriteTargetElementString(context.CodeWriter, node.TagHelper);

        // Initialize declaration.
        using (context.CodeWriter.BuildClassDeclaration(
            s_publicModifiers,
            node.ClassName,
            new BaseTypeWithModel(TagHelperTypeName),
            interfaces: default,
            typeParameters: default,
            context))
        {
            // Add view component helper.
            context.CodeWriter.WriteVariableDeclaration(
                new($"private readonly {ViewComponentHelperTypeName}"),
                ViewComponentHelperVariableName);

            // Add constructor.
            WriteConstructorString(context.CodeWriter, node.ClassName);

            // Add attributes.
            WriteAttributeDeclarations(context.CodeWriter, node.TagHelper);

            // Add process method.
            WriteProcessMethodString(context.CodeWriter, node.TagHelper);

            // We pre-process the arguments passed to `InvokeAsync` to ensure that the
            // provided markup attributes (in kebab-case) are matched to the associated
            // properties in the VCTH class.
            WriteProcessInvokeAsyncArgsMethodString(context.CodeWriter, node.TagHelper);
        }
    }

    private static void WriteConstructorString(CodeWriter writer, Content className)
    {
        writer.WriteLine($"public {className}({ViewComponentHelperTypeName} helper)");

        using (writer.BuildScope())
        {
            writer.WriteStartAssignment(ViewComponentHelperVariableName)
                .WriteLine("helper;");
        }
    }

    private static void WriteAttributeDeclarations(CodeWriter writer, TagHelperDescriptor tagHelper)
    {
        writer.Write("[")
          .Write(HtmlAttributeNotBoundAttributeTypeName)
          .WriteParameterSeparator()
          .Write(ViewContextAttributeTypeName)
          .WriteLine("]");

        writer.WriteAutoPropertyDeclaration(
            s_publicModifiers,
            ViewContextTypeName,
            ViewContextPropertyName);

        foreach (var attribute in tagHelper.BoundAttributes)
        {
            writer.WriteAutoPropertyDeclaration(
                s_publicModifiers,
                attribute.TypeName,
                attribute.GetPropertyName());

            if (attribute.IndexerTypeName != null)
            {
                writer.Write(" = ")
                    .WriteStartNewObject(attribute.TypeName)
                    .WriteEndMethodInvocation();
            }
        }
    }

    private static void WriteProcessMethodString(CodeWriter writer, TagHelperDescriptor tagHelper)
    {
        using (writer.BuildMethodDeclaration(
            new($"public override async"),
            new($"global::{typeof(Task).FullName}"),
            TagHelperProcessMethodName,
            parameters: [
                (TagHelperContextTypeName, TagHelperContextVariableName),
                (TagHelperOutputTypeName, TagHelperOutputVariableName)]))
        {
            writer.WriteInstanceMethodInvocation(
                new($"({ViewComponentHelperVariableName} as {IViewContextAwareTypeName})?"),
                IViewContextAwareContextualizeMethodName,
                [ViewContextPropertyName]);

            var methodArguments = GetMethodArguments(tagHelper);
            writer.Write("var ")
                .WriteStartAssignment(TagHelperContentVariableName)
                .WriteInstanceMethodInvocation(new($"await {ViewComponentHelperVariableName}"), ViewComponentInvokeMethodName, methodArguments);
            writer.WriteStartAssignment(new($"{TagHelperOutputVariableName}.{TagHelperOutputTagNamePropertyName}"))
                .WriteLine("null;");
            writer.WriteInstanceMethodInvocation(
                new($"{TagHelperOutputVariableName}.{TagHelperOutputContentPropertyName}"),
                TagHelperContentSetMethodName,
                [TagHelperContentVariableName]);
        }
    }

    private static void WriteProcessInvokeAsyncArgsMethodString(CodeWriter writer, TagHelperDescriptor tagHelper)
    {
        var methodReturnType = "Dictionary<string, object>";
        using (writer.BuildMethodDeclaration(
            "private",
            methodReturnType,
            TagHelperProcessInvokeAsyncArgsMethodName,
            parameters: (TagHelperContextTypeName, TagHelperContextVariableName)))
        {
            writer.WriteStartAssignment(new($"{methodReturnType} args"))
                .WriteStartNewObject(methodReturnType)
                .WriteEndMethodInvocation();

            foreach (var attribute in tagHelper.BoundAttributes)
            {
                var attributeName = attribute.Name;
                var parameterName = attribute.GetPropertyName();
                writer.WriteLine($"if (__context.AllAttributes.ContainsName(\"{attributeName}\"))");
                writer.WriteLine("{");
                writer.CurrentIndent += writer.TabSize;
                writer.WriteLine($"args[nameof({parameterName})] = {parameterName};");
                writer.CurrentIndent -= writer.TabSize;
                writer.WriteLine("}");
            }
            writer.WriteLine("return args;");
        }
    }

    private static ImmutableArray<Content> GetMethodArguments(TagHelperDescriptor tagHelper)
    {
        var viewComponentName = tagHelper.GetViewComponentName();

        return [
            new($"\"{viewComponentName}\""),
            new($"{TagHelperProcessInvokeAsyncArgsMethodName}({TagHelperContextVariableName})")
        ];
    }

    private static void WriteTargetElementString(CodeWriter writer, TagHelperDescriptor tagHelper)
    {
        Debug.Assert(tagHelper.TagMatchingRules.Length == 1);

        var rule = tagHelper.TagMatchingRules[0];

        writer.Write("[")
            .WriteStartMethodInvocation(HtmlTargetElementAttributeTypeName)
            .WriteStringLiteral(rule.TagName)
            .WriteLine(")]");
    }
}
