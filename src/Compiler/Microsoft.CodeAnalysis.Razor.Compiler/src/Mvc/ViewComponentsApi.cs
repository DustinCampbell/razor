// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

internal static class ViewComponentsApi
{
    private const string ViewFeaturesAssemblyName = "Microsoft.AspNetCore.Mvc.ViewFeatures";

    public static class ViewComponentAttribute
    {
        public const string AssemblyName = ViewFeaturesAssemblyName;
        public const string FullTypeName = "Microsoft.AspNetCore.Mvc.ViewComponentAttribute";
    }

    public static class NonViewComponentAttribute
    {
        public const string AssemblyName = ViewFeaturesAssemblyName;
        public const string FullTypeName = "Microsoft.AspNetCore.Mvc.NonViewComponentAttribute";
    }

    public static class HtmlAttributeNotBoundAttribute
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNotBoundAttribute";
    }

    public static class HtmlTargetElementAttribute
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute";
    }

    public static class IViewComponentHelper
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Mvc.IViewComponentHelper";
        public const string GloballyQualifiedTypeName = $"global::{FullTypeName}";

        public const string InvokeMethodName = "InvokeAsync";
    }

    public static class IViewContextAware
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Mvc.ViewFeatures.IViewContextAware";
        public const string GloballyQualifiedTypeName = $"global::{FullTypeName}";

        public const string ContextualizeMethodName = "Contextualize";
    }

    public static class TagHelper
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Razor.TagHelpers.TagHelper";
    }

    public static class TagHelperContext
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext";
    }

    public static class TagHelperOutput
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Razor.TagHelpers.TagHelperOutput";

        public const string ContentPropertyName = "Content";
        public const string TagNamePropertyName = "TagName";

        public const string ContentSetMethodName = "SetHtmlContent";
    }

    public static class ViewContext
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Mvc.Rendering.ViewContext";
        public const string GloballyQualifiedTypeName = $"global::{FullTypeName}";
    }

    public static class ViewContextAttribute
    {
        public const string FullTypeName = "Microsoft.AspNetCore.Mvc.ViewFeatures.ViewContextAttribute";
        public const string GloballyQualifiedTypeName = $"global::{FullTypeName}";
    }

    public const string ProcessAsyncMethodName = "ProcessAsync";
    public const string ProcessInvokeAsyncArgsMethodName = "ProcessInvokeAsyncArgs";
    public const string ViewContextPropertyName = "ViewContext";
}
