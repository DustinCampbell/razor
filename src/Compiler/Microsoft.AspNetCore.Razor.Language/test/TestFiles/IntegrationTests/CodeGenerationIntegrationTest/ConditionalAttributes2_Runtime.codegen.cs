﻿#pragma checksum "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "ee976c57374bafda343bd6b2086b223381f842016566d5a83498a6d0cbec2f54"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCoreGeneratedDocument.TestFiles_IntegrationTests_CodeGenerationIntegrationTest_ConditionalAttributes2), @"mvc.1.0.view", @"/TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml")]
namespace AspNetCoreGeneratedDocument
{
    #line default
    using global::System;
    using global::System.Collections.Generic;
    using global::System.Linq;
    using global::System.Threading.Tasks;
    using global::Microsoft.AspNetCore.Mvc;
    using global::Microsoft.AspNetCore.Mvc.Rendering;
    using global::Microsoft.AspNetCore.Mvc.ViewFeatures;
    #line default
    #line hidden
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"Sha256", @"ee976c57374bafda343bd6b2086b223381f842016566d5a83498a6d0cbec2f54", @"/TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemMetadataAttribute("Identifier", "/TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml")]
    [global::System.Runtime.CompilerServices.CreateNewOnMetadataUpdateAttribute]
    #nullable restore
    internal sealed class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_ConditionalAttributes2 : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    #nullable disable
    {
        #line hidden
        #pragma warning disable 0649
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext;
        #pragma warning restore 0649
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
        #pragma warning disable 0169
        private string __tagHelperStringValueBuffer;
        #pragma warning restore 0169
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
        {
            get
            {
                if (__backed__tagHelperScopeManager == null)
                {
                    __backed__tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
                }
                return __backed__tagHelperScopeManager;
            }
        }
        private global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
#nullable restore
#line (1,3)-(5,1) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"

    var ch = true;
    var cls = "bar";
    var s = "str";

#line default
#line hidden
#nullable disable

            WriteLiteral("    <a ");
            Write(
#nullable restore
#line (5,9)-(5,10) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
s

#line default
#line hidden
#nullable disable
            );
            WriteLiteral(" href=\"x\" />\r\n    <p ");
            Write(
#nullable restore
#line (6,9)-(6,10) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
s

#line default
#line hidden
#nullable disable
            );
            BeginWriteAttribute("class", " class=\"", 98, "\"", 110, 1);
            WriteAttributeValue("", 106, 
#nullable restore
#line (6,19)-(6,22) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
cls

#line default
#line hidden
#nullable disable
            , 106, 4, false);
            EndWriteAttribute();
            WriteLiteral(" />\r\n    <p ");
            Write(
#nullable restore
#line (7,9)-(7,10) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
s

#line default
#line hidden
#nullable disable
            );
            BeginWriteAttribute("class", " class=\"", 125, "\"", 139, 2);
            WriteAttributeValue("", 133, "x", 133, 1, true);
            WriteAttributeValue(" ", 134, 
#nullable restore
#line (7,21)-(7,24) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
cls

#line default
#line hidden
#nullable disable
            , 135, 4, false);
            EndWriteAttribute();
            WriteLiteral(" />\r\n    <p ");
            Write(
#nullable restore
#line (8,9)-(8,10) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
s

#line default
#line hidden
#nullable disable
            );
            BeginWriteAttribute("class", " class=\"", 154, "\"", 168, 2);
            WriteAttributeValue("", 162, 
#nullable restore
#line (8,19)-(8,22) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
cls

#line default
#line hidden
#nullable disable
            , 162, 4, false);
            WriteAttributeValue(" ", 166, "x", 167, 2, true);
            EndWriteAttribute();
            WriteLiteral(" />\r\n    <input type=\"checkbox\" ");
            Write(
#nullable restore
#line (9,29)-(9,30) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
s

#line default
#line hidden
#nullable disable
            );
            BeginWriteAttribute("checked", " checked=\"", 203, "\"", 216, 1);
            WriteAttributeValue("", 213, 
#nullable restore
#line (9,41)-(9,43) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
ch

#line default
#line hidden
#nullable disable
            , 213, 3, false);
            EndWriteAttribute();
            WriteLiteral(" />\r\n    <input type=\"checkbox\" ");
            Write(
#nullable restore
#line (10,29)-(10,30) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
s

#line default
#line hidden
#nullable disable
            );
            BeginWriteAttribute("checked", " checked=\"", 251, "\"", 266, 2);
            WriteAttributeValue("", 261, "x", 261, 1, true);
            WriteAttributeValue(" ", 262, 
#nullable restore
#line (10,43)-(10,45) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
ch

#line default
#line hidden
#nullable disable
            , 263, 3, false);
            EndWriteAttribute();
            WriteLiteral(" />\r\n    <p ");
            Write(
#nullable restore
#line (11,9)-(11,10) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
s

#line default
#line hidden
#nullable disable
            );
            BeginWriteAttribute("class", " class=\"", 281, "\"", 314, 1);
            WriteAttributeValue("", 289, new Microsoft.AspNetCore.Mvc.Razor.HelperResult(async(__razor_attribute_value_writer) => {
                PushWriter(__razor_attribute_value_writer);
#nullable restore
#line (11,19)-(11,37) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
if(cls != null) { 

#line default
#line hidden
#nullable disable
                Write(
#nullable restore
#line (11,38)-(11,41) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
cls

#line default
#line hidden
#nullable disable
                );
#nullable restore
#line (11,41)-(11,43) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
 }

#line default
#line hidden
#nullable disable
                PopWriter();
            }
            ), 289, 25, false);
            EndWriteAttribute();
            WriteLiteral(" />\r\n    ");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("a", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "__UniqueIdSuppressedForTesting__", async() => {
            }
            );
            __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n    <script ");
            Write(
#nullable restore
#line (13,14)-(13,15) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
s

#line default
#line hidden
#nullable disable
            );
            BeginWriteAttribute("src", " src=\"", 359, "\"", 410, 1);
            WriteAttributeValue("", 365, 
#nullable restore
#line (13,22)-(13,66) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
Url.Content("~/Scripts/jquery-1.6.2.min.js")

#line default
#line hidden
#nullable disable
            , 365, 45, false);
            EndWriteAttribute();
            WriteLiteral(" type=\"text/javascript\"></script>\r\n    <p ");
            Write(
#nullable restore
#line (14,9)-(14,10) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
s

#line default
#line hidden
#nullable disable
            );
            BeginWriteAttribute("class", " class=\"", 455, "\"", 468, 1);
            WriteAttributeValue("", 463, 
#nullable restore
#line (14,19)-(14,23) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
null

#line default
#line hidden
#nullable disable
            , 463, 5, false);
            EndWriteAttribute();
            WriteLiteral(" />\r\n    <p");
            BeginWriteAttribute("class", " class=\"", 480, "\"", 493, 1);
            WriteAttributeValue("", 488, 
#nullable restore
#line (15,16)-(15,20) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
null

#line default
#line hidden
#nullable disable
            , 488, 5, false);
            EndWriteAttribute();
            WriteLiteral(" ");
            Write(
#nullable restore
#line (15,23)-(15,24) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
s

#line default
#line hidden
#nullable disable
            );
            WriteLiteral(" />\r\n");
            WriteLiteral("    <p ");
            WriteLiteral(" ");
            Write(
#nullable restore
#line (17,23)-(17,24) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
s

#line default
#line hidden
#nullable disable
            );
            BeginWriteAttribute("style", " style=\"", 527, "\"", 540, 1);
            WriteAttributeValue("", 535, 
#nullable restore
#line (17,33)-(17,37) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
null

#line default
#line hidden
#nullable disable
            , 535, 5, false);
            EndWriteAttribute();
            WriteLiteral(">x</p>\r\n");
            WriteLiteral("    <p ");
            Write(
#nullable restore
#line (19,10)-(19,15) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
s + s

#line default
#line hidden
#nullable disable
            );
            BeginWriteAttribute("style", " style=\"", 566, "\"", 579, 1);
            WriteAttributeValue("", 574, 
#nullable restore
#line (19,25)-(19,29) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
null

#line default
#line hidden
#nullable disable
            , 574, 5, false);
            EndWriteAttribute();
            WriteLiteral(">x</p>\r\n    <p ");
#nullable restore
#line (20,10)-(20,31) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
if (s.Length != 0) { 

#line default
#line hidden
#nullable disable

            Write(
#nullable restore
#line (20,32)-(20,33) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
s

#line default
#line hidden
#nullable disable
            );
#nullable restore
#line (20,33)-(20,35) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
 }

#line default
#line hidden
#nullable disable

            BeginWriteAttribute("style", " style=\"", 623, "\"", 636, 1);
            WriteAttributeValue("", 631, 
#nullable restore
#line (20,45)-(20,49) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
null

#line default
#line hidden
#nullable disable
            , 631, 5, false);
            EndWriteAttribute();
            WriteLiteral(">x</p>\r\n    <p ");
            WriteLiteral("@s");
            BeginWriteAttribute("style", " style=\"", 655, "\"", 668, 1);
            WriteAttributeValue("", 663, 
#nullable restore
#line (21,20)-(21,24) "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/ConditionalAttributes2.cshtml"
null

#line default
#line hidden
#nullable disable
            , 663, 5, false);
            EndWriteAttribute();
            WriteLiteral(">x</p>\r\n");
        }
        #pragma warning restore 1998
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; } = default!;
        #nullable disable
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; } = default!;
        #nullable disable
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; } = default!;
        #nullable disable
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; } = default!;
        #nullable disable
        #nullable restore
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; } = default!;
        #nullable disable
    }
}
#pragma warning restore 1591
