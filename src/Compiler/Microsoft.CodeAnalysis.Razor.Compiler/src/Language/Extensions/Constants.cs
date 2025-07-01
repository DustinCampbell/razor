// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal static class Constants
{
    public const string TagHelperAttributeTypeName = "global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute";
    public const string EncodedHtmlStringTypeName = "global::Microsoft.AspNetCore.Html.HtmlString";
    public const string HtmlAttributeValueStyleTypeName = "global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle";
    public const string RazorCompiledItemAttributeTypeName = "global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute";
    public const string RazorCompiledItemMetadataAttributeTypeName = "global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemMetadataAttribute";
    public const string RazorSourceChecksumAttributeTypeName = "global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute";

    public const string ExecutionContextVariableName = "__tagHelperExecutionContext";
    public const string ExecutionContextAddHtmlAttributeMethodName = "AddHtmlAttribute";
    public const string ExecutionContextAddTagHelperAttributeMethodName = "AddTagHelperAttribute";
    public const string FormatInvalidIndexerAssignmentMethodName = "InvalidTagHelperIndexerAssignment";
}
