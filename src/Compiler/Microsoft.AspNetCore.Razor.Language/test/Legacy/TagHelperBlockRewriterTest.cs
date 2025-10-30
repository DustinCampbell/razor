// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Components;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class TagHelperBlockRewriterTest : TagHelperRewritingTestBase
{
    public static readonly TagHelperCollection SymbolBoundAttributes_TagHelpers =
    [
        TagHelperDescriptorBuilder.CreateTagHelper("CatchAllTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule
                .RequireTagName("*")
                .RequireAttributeDescriptor(attribute => attribute.Name("bound")))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("[item]")
                .PropertyName("ListItems")
                .TypeName(typeof(List<string>).Namespace + "List<System.String>"))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("[(item)]")
                .PropertyName("ArrayItems")
                .TypeName(typeof(string[]).Namespace + "System.String[]"))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("(click)")
                .PropertyName("Event1")
                .TypeName(typeof(Action).FullName))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("(^click)")
                .PropertyName("Event2")
                .TypeName(typeof(Action).FullName))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("*something")
                .PropertyName("StringProperty1")
                .TypeName(typeof(string).FullName))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("#local")
                .PropertyName("StringProperty2")
                .TypeName(typeof(string).FullName))
            .Build()
    ];

    [Fact]
    public void CanHandleSymbolBoundAttributes1()
    {
        EvaluateData(SymbolBoundAttributes_TagHelpers, """
            <ul bound [item]='items'></ul>
            """);
    }

    [Fact]
    public void CanHandleSymbolBoundAttributes2()
    {
        EvaluateData(SymbolBoundAttributes_TagHelpers, """
            <ul bound [(item)]='items'></ul>
            """);
    }

    [Fact]
    public void CanHandleSymbolBoundAttributes3()
    {
        EvaluateData(SymbolBoundAttributes_TagHelpers, """
            <button bound (click)='doSomething()'>Click Me</button>
            """);
    }

    [Fact]
    public void CanHandleSymbolBoundAttributes4()
    {
        EvaluateData(SymbolBoundAttributes_TagHelpers, """
            <button bound (^click)='doSomething()'>Click Me</button>
            """);
    }

    [Fact]
    public void CanHandleSymbolBoundAttributes5()
    {
        EvaluateData(SymbolBoundAttributes_TagHelpers, """
            <template bound *something='value'></template>
            """);
    }

    [Fact]
    public void CanHandleSymbolBoundAttributes6()
    {
        EvaluateData(SymbolBoundAttributes_TagHelpers, """
            <div bound #localminimized></div>
            """);
    }

    [Fact]
    public void CanHandleSymbolBoundAttributes7()
    {
        EvaluateData(SymbolBoundAttributes_TagHelpers, """
            <div bound #local='value'></div>
            """);
    }

    public static readonly TagHelperCollection WithoutEndTag_TagHelpers =
    [
        TagHelperDescriptorBuilder.CreateTagHelper("InputTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule
                .RequireTagName("input")
                .RequireTagStructure(TagStructure.WithoutEndTag))
            .Build()
    ];

    [Fact]
    public void CanHandleWithoutEndTagTagStructure1()
    {
        EvaluateData(WithoutEndTag_TagHelpers, """
            <input>
            """);
    }

    [Fact]
    public void CanHandleWithoutEndTagTagStructure2()
    {
        EvaluateData(WithoutEndTag_TagHelpers, """
            <input type='text'>
            """);
    }

    [Fact]
    public void CanHandleWithoutEndTagTagStructure3()
    {
        EvaluateData(WithoutEndTag_TagHelpers, """
            <input><input>
            """);
    }

    [Fact]
    public void CanHandleWithoutEndTagTagStructure4()
    {
        EvaluateData(WithoutEndTag_TagHelpers, """
            <input type='text'><input>
            """);
    }

    [Fact]
    public void CanHandleWithoutEndTagTagStructure5()
    {
        EvaluateData(WithoutEndTag_TagHelpers, """
            <div><input><input></div>
            """);
    }

    public static TagHelperCollection GetTagStructureCompatibilityTagHelpers(TagStructure structure1, TagStructure structure2)
    {
        return
        [
            TagHelperDescriptorBuilder.CreateTagHelper("InputTagHelper1", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("input")
                    .RequireTagStructure(structure1))
                .Build(),
            TagHelperDescriptorBuilder.CreateTagHelper("InputTagHelper2", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("input")
                    .RequireTagStructure(structure2))
                .Build()
        ];
    }

    [Fact]
    public void AllowsCompatibleTagStructures1()
    {
        var tagHelpers = GetTagStructureCompatibilityTagHelpers(TagStructure.Unspecified, TagStructure.Unspecified);

        EvaluateData(tagHelpers, """
            <input></input>
            """);
    }

    [Fact]
    public void AllowsCompatibleTagStructures2()
    {
        var tagHelpers = GetTagStructureCompatibilityTagHelpers(TagStructure.Unspecified, TagStructure.Unspecified);

        EvaluateData(tagHelpers, """
            <input />
            """);
    }

    [Fact]
    public void AllowsCompatibleTagStructures3()
    {
        var tagHelpers = GetTagStructureCompatibilityTagHelpers(TagStructure.Unspecified, TagStructure.WithoutEndTag);

        EvaluateData(tagHelpers, """
            <input type='text'>
            """);
    }

    [Fact]
    public void AllowsCompatibleTagStructures4()
    {
        var tagHelpers = GetTagStructureCompatibilityTagHelpers(TagStructure.WithoutEndTag, TagStructure.WithoutEndTag);

        EvaluateData(tagHelpers, """
            <input><input>
            """);
    }

    [Fact]
    public void AllowsCompatibleTagStructures5()
    {
        var tagHelpers = GetTagStructureCompatibilityTagHelpers(TagStructure.Unspecified, TagStructure.NormalOrSelfClosing);

        EvaluateData(tagHelpers, """
            <input type='text'></input>
            """);
    }

    [Fact]
    public void AllowsCompatibleTagStructures6()
    {
        var tagHelpers = GetTagStructureCompatibilityTagHelpers(TagStructure.Unspecified, TagStructure.WithoutEndTag);

        EvaluateData(tagHelpers, """
            <input />
            """);
    }

    [Fact]
    public void AllowsCompatibleTagStructures7()
    {
        var tagHelpers = GetTagStructureCompatibilityTagHelpers(TagStructure.NormalOrSelfClosing, TagStructure.Unspecified);

        EvaluateData(tagHelpers, """
            <input />
            """);
    }

    [Fact]
    public void AllowsCompatibleTagStructures8()
    {
        var tagHelpers = GetTagStructureCompatibilityTagHelpers(TagStructure.WithoutEndTag, TagStructure.Unspecified);

        EvaluateData(tagHelpers, """
            <input></input>
            """);
    }

    [Fact]
    public void AllowsCompatibleTagStructures_DirectiveAttribute_SelfClosing()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateEventHandler("InputTagHelper1", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("*")
                    .RequireAttributeDescriptor(b => b
                        .Name("@onclick")
                        .IsDirectiveAttribute()))
                .Build(),
        ];

        EvaluateData(tagHelpers, """
            <input @onclick="@test"/>
            """);
    }

    [Fact]
    public void AllowsCompatibleTagStructures_DirectiveAttribute_Void()
    {
        TagHelperCollection tagHelpers =
        [
            TagHelperDescriptorBuilder.CreateEventHandler("InputTagHelper1", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("*")
                    .RequireAttributeDescriptor(b => b
                        .Name("@onclick")
                        .IsDirectiveAttribute()))
                .Build(),
        ];

        EvaluateData(tagHelpers, """
            <input @onclick="@test">
            """);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelpersWithAttributes1()
    {
        RunParseTreeRewriterTest("""
            <p class='
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelpersWithAttributes2()
    {
        RunParseTreeRewriterTest("""
            <p bar="false"" <strong>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelpersWithAttributes3()
    {
        RunParseTreeRewriterTest("""
            <p bar='false  <strong>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelpersWithAttributes4()
    {
        RunParseTreeRewriterTest("""
            <p bar='false  <strong'
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelpersWithAttributes5()
    {
        RunParseTreeRewriterTest("""
            <p bar=false'
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelpersWithAttributes6()
    {
        RunParseTreeRewriterTest("""
            <p bar="false'
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelpersWithAttributes7()
    {
        RunParseTreeRewriterTest("""
            <p bar="false' ></p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelpersWithAttributes8()
    {
        RunParseTreeRewriterTest("""
            <p foo bar<strong>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelpersWithAttributes9()
    {
        RunParseTreeRewriterTest("""
            <p class=btn" bar<strong>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelpersWithAttributes10()
    {
        RunParseTreeRewriterTest("""
            <p class=btn" bar="foo"<strong>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelpersWithAttributes11()
    {
        RunParseTreeRewriterTest("""
            <p class="btn bar="foo"<strong>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelpersWithAttributes12()
    {
        RunParseTreeRewriterTest("""
            <p class="btn bar="foo"></p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelpersWithAttributes13()
    {
        RunParseTreeRewriterTest("""
            <p @DateTime.Now class="btn"></p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelpersWithAttributes14()
    {
        RunParseTreeRewriterTest("""
            <p @DateTime.Now="btn"></p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelpersWithAttributes15()
    {
        RunParseTreeRewriterTest("""
            <p class=@DateTime.Now"></p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelpersWithAttributes16()
    {
        RunParseTreeRewriterTest("""
            <p class="@do {
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelpersWithAttributes17()
    {
        RunParseTreeRewriterTest("""
            <p class="@do {"></p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelpersWithAttributes18()
    {
        RunParseTreeRewriterTest("""
            <p @do { someattribute="btn"></p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelpersWithAttributes19()
    {
        RunParseTreeRewriterTest("""
            <p class=some=thing attr="@value"></p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelpersWithAttributes20()
    {
        RunParseTreeRewriterTest("""
            <p attr="@if (true) <p attr='@foo'> }"></p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelper1()
    {
        RunParseTreeRewriterTest("""
            <p
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelper2()
    {
        RunParseTreeRewriterTest("""
            <p></p
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelper3()
    {
        RunParseTreeRewriterTest("""
            <p><strong
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelper4()
    {
        RunParseTreeRewriterTest("""
            <strong <p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelper5()
    {
        RunParseTreeRewriterTest("""
            <strong </strong
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelper6()
    {
        RunParseTreeRewriterTest("""
            <<</strong> <<p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelper7()
    {
        RunParseTreeRewriterTest("""
            <<<strong>> <<>>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void CreatesErrorForMalformedTagHelper8()
    {
        RunParseTreeRewriterTest("""
            <str<strong></p></strong>
            """,
            tagNames: ["strong", "p"]);
    }

    public static readonly TagHelperCollection CodeTagHelperAttributes_TagHelpers =
    [
        TagHelperDescriptorBuilder.CreateTagHelper("PersonTagHelper", "personAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("person"))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("age")
                .PropertyName("Age")
                .TypeName(typeof(int).FullName))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("birthday")
                .PropertyName("BirthDay")
                .TypeName(typeof(DateTime).FullName))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("name")
                .PropertyName("Name")
                .TypeName(typeof(string).FullName))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("alive")
                .PropertyName("Alive")
                .TypeName(typeof(bool).FullName))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("tag")
                .PropertyName("Tag")
                .TypeName(typeof(object).FullName))
            .Build()
    ];

    [Fact]
    public void UnderstandsMultipartNonStringTagHelperAttributes()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            <person age="(() => 123)()" />
            """);
    }

    [Fact]
    public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes1()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            <person age="12" />
            """);
    }

    [Fact]
    public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes2()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            <person birthday="DateTime.Now" />
            """);
    }

    [Fact]
    public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes3()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            <person age="@DateTime.Now.Year" />
            """);
    }

    [Fact]
    public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes4()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            <person age=" @DateTime.Now.Year" />
            """);
    }

    [Fact]
    public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes5()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            <person name="John" />
            """);
    }

    [Fact]
    public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes6()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            <person name="Time: @DateTime.Now" />
            """);
    }

    [Fact]
    public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes7()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            <person age="1 + @value + 2" birthday='(bool)@Bag["val"] ? @@DateTime : @DateTime.Now'/>
            """);
    }

    [Fact]
    public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes8()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            <person age="12" birthday="DateTime.Now" name="Time: @DateTime.Now" />
            """);
    }

    [Fact]
    public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes9()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            <person age="12" birthday="DateTime.Now" name="Time: @@ @DateTime.Now" />
            """);
    }

    [Fact]
    public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes10()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            <person age="12" birthday="DateTime.Now" name="@@BoundStringAttribute" />
            """);
    }

    [Fact]
    public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes11()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            <person age="@@@(11+1)" birthday="DateTime.Now" name="Time: @DateTime.Now" />
            """);
    }

    [Fact]
    public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes12()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            <person age="@{flag == 0 ? 11 : 12}" />
            """);
    }
    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10186")]
    public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes13()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            @{ 
                var count = "1";
            }
            <person age="Convert.ToInt32(@count)" />
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10186")]
    public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes14()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            @{ 
                var @string = "1";
            }
            <person age="Convert.ToInt32(@string)" />
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10186")]
    public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes15()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            @{ 
                var count = "1";
            }
            <person age=Convert.ToInt32(@count) />
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10186")]
    public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes16()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            @{ 
                var count = "1";
            }
            <person age='Convert.ToInt32(@count + "2")' />
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10186")]
    public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes17()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            @{ 
                var count = 1;
            }
            <person age='@@count' />
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10186")]
    public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes18()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            @{ 
                var count = 1;
            }
            <person age="@@count" />
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10186")]
    public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes19()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            @{ 
                var count = 1;
            }
            <person age=@@count />
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10186")]
    public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes20()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            @{ 
                var isAlive = true;
            }
            <person alive="!@isAlive" />
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10186")]
    public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes21()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            @{ 
                var obj = new { Prop = (object)1 };
            }
            <person age="(int)@obj.Prop" />
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10186")]
    public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes22()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            @{ 
                var obj = new { Prop = (object)1 };
            }
            <person tag="new { @params = 1 }" />
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10186")]
    public void CreatesMarkupCodeSpansForNonStringTagHelperAttributes23()
    {
        TagHelperCollection tagHelpers = [
            TagHelperDescriptorBuilder.CreateTagHelper("InputTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("input"))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("tuple-dictionary")
                    .PropertyName("DictionaryOfBoolAndStringTupleProperty")
                    .TypeName(typeof(IDictionary<string, int>).Namespace + ".IDictionary<System.String, (System.Boolean, System.String)>")
                    .AsDictionaryAttribute("tuple-prefix-", typeof((bool, string)).FullName))
                .Build()
        ];

        EvaluateData(tagHelpers, """
            @{ 
                var item = new { Items = new System.List<string>() { "one", "two" } };
            }
            <input tuple-prefix-test='(@item. Items.Where(i=>i.Contains("one")). Count()>0, @item. Items.FirstOrDefault(i=>i.Contains("one"))?. Replace("one",""))' />
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10426")]
    public void CreatesMarkupCodeSpans_EscapedExpression_01()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            <person tag='@new string("1, 2")' />
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10426")]
    public void CreatesMarkupCodeSpans_EscapedExpression_02()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            <person tag="@(new string("1, 2"))" />
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10426")]
    public void CreatesMarkupCodeSpans_EscapedExpression_03()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            <person tag="@new string(@x("1, 2"))" />
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10426")]
    public void CreatesMarkupCodeSpans_EscapedExpression_04()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            <person tag='new string("1, 2")' />
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10426")]
    public void CreatesMarkupCodeSpans_EscapedExpression_05()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            <person tag='new string(@x("1, 2"))' />
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10426")]
    public void CreatesMarkupCodeSpans_EscapedExpression_06()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            <person tag='@new string("1 2")' />
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10426")]
    public void CreatesMarkupCodeSpans_EscapedExpression_07()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            <person tag="@(new string("1 2"))" />
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10426")]
    public void CreatesMarkupCodeSpans_EscapedExpression_08()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            <person tag="@(new string(@x("1 2")))" />
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10426")]
    public void CreatesMarkupCodeSpans_EscapedExpression_09()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            <person tag='"0" + new string("1 2")' />
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10426")]
    public void CreatesMarkupCodeSpans_EscapedExpression_10()
    {
        EvaluateData(CodeTagHelperAttributes_TagHelpers, """
            <person tag='"0" + new @String("1 2")' />
            """);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_CreatesErrorForIncompleteTagHelper1()
    {
        RunParseTreeRewriterTest("""
            <p class=foo dynamic=@DateTime.Now style=color:red;><strong></p></strong>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_CreatesErrorForIncompleteTagHelper2()
    {
        RunParseTreeRewriterTest("""
            <div><p>Hello <strong>World</strong></div>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_CreatesErrorForIncompleteTagHelper3()
    {
        RunParseTreeRewriterTest("""
            <div><p>Hello <strong>World</div>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_CreatesErrorForIncompleteTagHelper4()
    {
        RunParseTreeRewriterTest("""
            <p class="foo">Hello <p style="color:red;">World</p>
            """,
            tagNames: ["strong", "p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesOddlySpacedTagHelperTagBlocks1()
    {
        RunParseTreeRewriterTest("""
            <p      class="     foo"    style="   color :  red  ;   "    ></p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesOddlySpacedTagHelperTagBlocks2()
    {
        RunParseTreeRewriterTest("""
            <p      class="     foo"    style="   color :  red  ;   "    >Hello World</p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesOddlySpacedTagHelperTagBlocks3()
    {
        RunParseTreeRewriterTest("""
            <p     class="   foo  " >Hello</p> <p    style="  color:red; " >World</p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesComplexAttributeTagHelperTagBlocks1()
    {
        const string expression = "@DateTime.Now";

        RunParseTreeRewriterTest($$"""
            <p class="{{expression}}" style='{{expression}}'></p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesComplexAttributeTagHelperTagBlocks2()
    {
        const string doWhile = "@do { var foo = bar; <text>Foo</text> foo++; } while (foo<bar>);";

        RunParseTreeRewriterTest($$"""
            <p class="{{doWhile}}" style='{{doWhile}}'></p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesComplexAttributeTagHelperTagBlocks3()
    {
        const string expression = "@DateTime.Now";

        RunParseTreeRewriterTest($$"""
            <p class="{{expression}}" style='{{expression}}'>Hello World</p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesComplexAttributeTagHelperTagBlocks4()
    {
        const string doWhile = "@do { var foo = bar; <text>Foo</text> foo++; } while (foo<bar>);";

        RunParseTreeRewriterTest($$"""
            <p class="{{doWhile}}" style='{{doWhile}}'>Hello World</p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesComplexAttributeTagHelperTagBlocks5()
    {
        const string expression = "@DateTime.Now";

        RunParseTreeRewriterTest($$"""
            <p class="{{expression}}">Hello</p> <p style='{{expression}}'>World</p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesComplexAttributeTagHelperTagBlocks6()
    {
        const string expression = "@do { var foo = bar; <text>Foo</text> foo++; } while (foo<bar>);";

        RunParseTreeRewriterTest($$"""
            <p class="{{expression}}">Hello</p> <p style='{{expression}}'>World</p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesComplexAttributeTagHelperTagBlocks7()
    {
        const string expression = "@DateTime.Now";

        RunParseTreeRewriterTest($$"""
            <p class="{{expression}}" style='{{expression}}'>Hello World <strong class="{{expression}}">inside of strong tag</strong></p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesComplexTagHelperTagBlocks1()
    {
        const string expression = "@DateTime.Now";

        RunParseTreeRewriterTest($$"""
            <p>{{expression}}</p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesComplexTagHelperTagBlocks2()
    {
        const string doWhile = "@do { var foo = bar; <p>Foo</p> foo++; } while (foo<bar>);";

        RunParseTreeRewriterTest($$"""
            <p>{{doWhile}}</p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesComplexTagHelperTagBlocks3()
    {
        const string expression = "@DateTime.Now";

        RunParseTreeRewriterTest($$"""
            <p>Hello World {{expression}}</p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesComplexTagHelperTagBlocks4()
    {
        const string doWhile = "@do { var foo = bar; <p>Foo</p> foo++; } while (foo<bar>);";

        RunParseTreeRewriterTest($$"""
            <p>Hello World {{doWhile}}</p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesComplexTagHelperTagBlocks5()
    {
        const string expression = "@DateTime.Now";

        RunParseTreeRewriterTest($$"""
            <p>{{expression}}</p> <p>{{expression}}</p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesComplexTagHelperTagBlocks6()
    {
        const string doWhile = "@do { var foo = bar; <p>Foo</p> foo++; } while (foo<bar>);";

        RunParseTreeRewriterTest($$"""
            <p>{{doWhile}}</p> <p>{{doWhile}}</p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesComplexTagHelperTagBlocks7()
    {
        const string expression = "@DateTime.Now";

        RunParseTreeRewriterTest($$"""
            <p>Hello {{expression}}<strong>inside of {{expression}} strong tag</strong></p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesComplexTagHelperTagBlocks8()
    {
        const string doWhile = "@do { var foo = bar; <p>Foo</p> foo++; } while (foo<bar>);";

        RunParseTreeRewriterTest($$"""
            <p>Hello {{doWhile}}<strong>inside of {{doWhile}} strong tag</strong></p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_AllowsInvalidHtml1()
    {
        RunParseTreeRewriterTest("""
            <<<p>>></p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_AllowsInvalidHtml2()
    {
        RunParseTreeRewriterTest("""
            <<p />
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_AllowsInvalidHtml3()
    {
        RunParseTreeRewriterTest("""
            < p />
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_AllowsInvalidHtml4()
    {
        RunParseTreeRewriterTest("""
            <input <p />
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_AllowsInvalidHtml5()
    {
        RunParseTreeRewriterTest("""
            < class="foo" <p />
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_AllowsInvalidHtml6()
    {
        RunParseTreeRewriterTest("""
            </<<p>/></p>>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_AllowsInvalidHtml7()
    {
        RunParseTreeRewriterTest("""
            </<<p>/><strong></p>>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_AllowsInvalidHtml8()
    {
        RunParseTreeRewriterTest("""
            </<<p>@DateTime.Now/><strong></p>>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_AllowsInvalidHtml9()
    {
        RunParseTreeRewriterTest("""
            </  /<  ><p>@DateTime.Now / ><strong></p></        >
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_AllowsInvalidHtml10()
    {
        RunParseTreeRewriterTest("""
            <p>< @DateTime.Now ></ @DateTime.Now ></p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void UnderstandsEmptyAttributeTagHelpers1()
    {
        RunParseTreeRewriterTest("""
            <p class=""></p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void UnderstandsEmptyAttributeTagHelpers2()
    {
        RunParseTreeRewriterTest("""
            <p class=''></p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void UnderstandsEmptyAttributeTagHelpers3()
    {
        RunParseTreeRewriterTest("""
            <p class=></p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void UnderstandsEmptyAttributeTagHelpers4()
    {
        RunParseTreeRewriterTest("""
            <p class1='' class2= class3="" />
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void UnderstandsEmptyAttributeTagHelpers5()
    {
        RunParseTreeRewriterTest("""
            <p class1=''class2=""class3= />
            """,
            tagNames: ["p"]);
    }

    public static readonly TagHelperCollection EmptyTagHelperBoundAttribute_TagHelpers =
    [
        TagHelperDescriptorBuilder.CreateTagHelper("mythTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("myth"))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("bound")
                .PropertyName("Bound")
                .TypeName(typeof(bool).FullName))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("name")
                .PropertyName("Name")
                .TypeName(typeof(string).FullName))
            .Build()
    ];

    [Fact]
    public void CreatesErrorForEmptyTagHelperBoundAttributes1()
    {
        EvaluateData(EmptyTagHelperBoundAttribute_TagHelpers, """
            <myth bound='' />
            """);
    }

    [Fact]
    public void CreatesErrorForEmptyTagHelperBoundAttributes2()
    {
        EvaluateData(EmptyTagHelperBoundAttribute_TagHelpers, """
            <myth bound='    true' />
            """);
    }

    [Fact]
    public void CreatesErrorForEmptyTagHelperBoundAttributes3()
    {
        EvaluateData(EmptyTagHelperBoundAttribute_TagHelpers, """
            <myth bound='    ' />
            """);
    }

    [Fact]
    public void CreatesErrorForEmptyTagHelperBoundAttributes4()
    {
        EvaluateData(EmptyTagHelperBoundAttribute_TagHelpers, """
            <myth bound=''  bound="" />
            """);
    }

    [Fact]
    public void CreatesErrorForEmptyTagHelperBoundAttributes5()
    {
        EvaluateData(EmptyTagHelperBoundAttribute_TagHelpers, """
            <myth bound=' '  bound="  " />
            """);
    }

    [Fact]
    public void CreatesErrorForEmptyTagHelperBoundAttributes6()
    {
        EvaluateData(EmptyTagHelperBoundAttribute_TagHelpers, """
            <myth bound='true' bound=  />
            """);
    }

    [Fact]
    public void CreatesErrorForEmptyTagHelperBoundAttributes7()
    {
        EvaluateData(EmptyTagHelperBoundAttribute_TagHelpers, """
            <myth bound= name='' />
            """);
    }

    [Fact]
    public void CreatesErrorForEmptyTagHelperBoundAttributes8()
    {
        EvaluateData(EmptyTagHelperBoundAttribute_TagHelpers, """
            <myth bound= name='  ' />
            """);
    }

    [Fact]
    public void CreatesErrorForEmptyTagHelperBoundAttributes9()
    {
        EvaluateData(EmptyTagHelperBoundAttribute_TagHelpers, """
            <myth bound='true' name='john' bound= name= />
            """);
    }

    [Fact]
    public void CreatesErrorForEmptyTagHelperBoundAttributes10()
    {
        EvaluateData(EmptyTagHelperBoundAttribute_TagHelpers, """
            <myth BouND='' />
            """);
    }

    [Fact]
    public void CreatesErrorForEmptyTagHelperBoundAttributes11()
    {
        EvaluateData(EmptyTagHelperBoundAttribute_TagHelpers, """
            <myth BOUND=''    bOUnd="" />
            """);
    }

    [Fact]
    public void CreatesErrorForEmptyTagHelperBoundAttributes12()
    {
        EvaluateData(EmptyTagHelperBoundAttribute_TagHelpers, """
            <myth BOUND= nAMe='john'></myth>
            """);
    }

    [Fact]
    public void CreatesErrorForEmptyTagHelperBoundAttributes13()
    {
        EvaluateData(EmptyTagHelperBoundAttribute_TagHelpers, """
            <myth bound='    @true  ' />
            """);
    }

    [Fact]
    public void CreatesErrorForEmptyTagHelperBoundAttributes14()
    {
        EvaluateData(EmptyTagHelperBoundAttribute_TagHelpers, """
            <myth bound='    @(true)  ' />
            """);
    }
    [Fact]
    public void TagHelperParseTreeRewriter_RewritesScriptTagHelpers1()
    {
        RunParseTreeRewriterTest("""
            <script><script></foo></script>
            """,
            tagNames: ["p", "div", "script"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesScriptTagHelpers2()
    {
        RunParseTreeRewriterTest("""
            <script>Hello World <div></div></script>
            """,
            tagNames: ["p", "div", "script"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesScriptTagHelpers3()
    {
        RunParseTreeRewriterTest("""
            <script>Hel<p>lo</p></script> <p><div>World</div></p>
            """,
            tagNames: ["p", "div", "script"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesScriptTagHelpers4()
    {
        RunParseTreeRewriterTest("""
            <script>Hel<strong>lo</strong></script> <script><span>World</span></script>
            """,
            tagNames: ["p", "div", "script"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesScriptTagHelpers5()
    {
        RunParseTreeRewriterTest("""
            <script class="foo" style="color:red;" />
            """,
            tagNames: ["p", "div", "script"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesScriptTagHelpers6()
    {
        RunParseTreeRewriterTest("""
            <p>Hello <script class="foo" style="color:red;"></script> World</p>
            """,
            tagNames: ["p", "div", "script"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesScriptTagHelpers7()
    {
        RunParseTreeRewriterTest("""
            <p>Hello <script class="@@foo@bar.com" style="color:red;"></script> World</p>
            """,
            tagNames: ["p", "div", "script"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesSelfClosingTagHelpers1()
    {
        RunParseTreeRewriterTest("""
            <p class="foo" style="color:red;" />
            """,
            tagNames: ["p"]);
    }


    [Fact]
    public void TagHelperParseTreeRewriter_RewritesSelfClosingTagHelpers2()
    {
        RunParseTreeRewriterTest("""
            <p>Hello <p class="foo" style="color:red;" /> World</p>
            """,
            tagNames: ["p"]);
    }


    [Fact]
    public void TagHelperParseTreeRewriter_RewritesSelfClosingTagHelpers3()
    {
        RunParseTreeRewriterTest("""
            Hello<p class="foo" /> <p style="color:red;" />World
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesTagHelpersWithQuotelessAttributes1()
    {
        RunParseTreeRewriterTest("""
            <p class=foo dynamic=@DateTime.Now style=color:red;></p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesTagHelpersWithQuotelessAttributes2()
    {
        RunParseTreeRewriterTest("""
            <p class=foo dynamic=@DateTime.Now style=color:red;>Hello World</p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesTagHelpersWithQuotelessAttributes3()
    {
        RunParseTreeRewriterTest("""
            <p class=foo dynamic=@DateTime.Now style=color@@:red;>Hello World</p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesTagHelpersWithQuotelessAttributes4()
    {
        RunParseTreeRewriterTest("""
            <p class=foo dynamic=@DateTime.Now>Hello</p> <p style=color:red; dynamic=@DateTime.Now>World</p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesTagHelpersWithQuotelessAttributes5()
    {
        RunParseTreeRewriterTest("""
            <p class=foo dynamic=@DateTime.Now style=color:red;>Hello World <strong class="foo">inside of strong tag</strong></p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesTagHelpersWithPlainAttributes1()
    {
        RunParseTreeRewriterTest("""
            <p class="foo" style="color:red;"></p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesTagHelpersWithPlainAttributes2()
    {
        RunParseTreeRewriterTest("""
            <p class="foo" style="color:red;">Hello World</p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesTagHelpersWithPlainAttributes3()
    {
        RunParseTreeRewriterTest("""
            <p class="foo">Hello</p> <p style="color:red;">World</p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesTagHelpersWithPlainAttributes4()
    {
        RunParseTreeRewriterTest("""
            <p class="foo" style="color:red;">Hello World <strong class="foo">inside of strong tag</strong></p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesPlainTagHelperTagBlocks1()
    {
        RunParseTreeRewriterTest("""
            <p></p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesPlainTagHelperTagBlocks2()
    {
        RunParseTreeRewriterTest("""
            <p>Hello World</p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesPlainTagHelperTagBlocks3()
    {
        RunParseTreeRewriterTest("""
            <p>Hello</p> <p>World</p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void TagHelperParseTreeRewriter_RewritesPlainTagHelperTagBlocks4()
    {
        RunParseTreeRewriterTest("""
            <p>Hello World <strong>inside of strong tag</strong></p>
            """,
            tagNames: ["p"]);
    }

    [Fact]
    public void GeneratesExpectedOutputForUnboundDataDashAttributes_Document1()
    {
        var expression = "@DateTime.Now";

        RunParseTreeRewriterTest($"""
            <input data-required='{expression}' />
            """,
            tagNames: ["input"]);
    }

    [Fact]
    public void GeneratesExpectedOutputForUnboundDataDashAttributes_Document2()
    {
        RunParseTreeRewriterTest("""
            <input data-required='value' />
            """,
            tagNames: ["input"]);
    }

    [Fact]
    public void GeneratesExpectedOutputForUnboundDataDashAttributes_Document3()
    {
        var expression = "@DateTime.Now";

        RunParseTreeRewriterTest($"""
            <input data-required='prefix {expression}' />
            """,
            tagNames: ["input"]);
    }

    [Fact]
    public void GeneratesExpectedOutputForUnboundDataDashAttributes_Document4()
    {
        var expression = "@DateTime.Now";

        RunParseTreeRewriterTest($"""
            <input data-required='{expression} suffix' />
            """,
            tagNames: ["input"]);
    }

    [Fact]
    public void GeneratesExpectedOutputForUnboundDataDashAttributes_Document5()
    {
        var expression = "@DateTime.Now";

        RunParseTreeRewriterTest($"""
            <input data-required='prefix {expression} suffix' />
            """,
            tagNames: ["input"]);
    }

    [Fact]
    public void GeneratesExpectedOutputForUnboundDataDashAttributes_Document6()
    {
        var expression = "@DateTime.Now";

        RunParseTreeRewriterTest($"""
            <input pre-attribute data-required='prefix {expression} suffix' post-attribute />
            """,
            tagNames: ["input"]);
    }

    [Fact]
    public void GeneratesExpectedOutputForUnboundDataDashAttributes_Document7()
    {
        var expression = "@DateTime.Now";

        RunParseTreeRewriterTest($"""
            <input data-required='{expression} middle {expression}' />
            """,
            tagNames: ["input"]);
    }

    [Fact]
    public void GeneratesExpectedOutputForUnboundDataDashAttributes_Block1()
    {
        var expression = "@DateTime.Now";

        RunParseTreeRewriterTest(WrapInCSharpBlock($"""
            <input data-required='{expression}' />
            """),
            tagNames: ["input"]);
    }

    [Fact]
    public void GeneratesExpectedOutputForUnboundDataDashAttributes_Block2()
    {
        RunParseTreeRewriterTest(WrapInCSharpBlock("""
            <input data-required='value' />
            """),
            tagNames: ["input"]);
    }

    [Fact]
    public void GeneratesExpectedOutputForUnboundDataDashAttributes_Block3()
    {
        var expression = "@DateTime.Now";

        RunParseTreeRewriterTest(WrapInCSharpBlock($"""
            <input data-required='prefix {expression}' />
            """),
            tagNames: ["input"]);
    }

    [Fact]
    public void GeneratesExpectedOutputForUnboundDataDashAttributes_Block4()
    {
        var expression = "@DateTime.Now";

        RunParseTreeRewriterTest(WrapInCSharpBlock($"""
            <input data-required='{expression} suffix' />
            """),
            tagNames: ["input"]);
    }

    [Fact]
    public void GeneratesExpectedOutputForUnboundDataDashAttributes_Block5()
    {
        var expression = "@DateTime.Now";

        RunParseTreeRewriterTest(WrapInCSharpBlock($"""
            <input data-required='prefix {expression} suffix' />
            """),
            tagNames: ["input"]);
    }

    [Fact]
    public void GeneratesExpectedOutputForUnboundDataDashAttributes_Block6()
    {
        var expression = "@DateTime.Now";

        RunParseTreeRewriterTest(WrapInCSharpBlock($"""
            <input pre-attribute data-required='prefix {expression} suffix' post-attribute />
            """),
            tagNames: ["input"]);
    }

    [Fact]
    public void GeneratesExpectedOutputForUnboundDataDashAttributes_Block7()
    {
        var expression = "@DateTime.Now";

        RunParseTreeRewriterTest(WrapInCSharpBlock($"""
            <input data-required='{expression} middle {expression}' />
            """),
            tagNames: ["input"]);
    }

    public static readonly TagHelperCollection MinimizedAttribute_TagHelpers =
    [
        TagHelperDescriptorBuilder.CreateTagHelper("InputTagHelper1", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule
                .RequireTagName("input")
                .RequireAttributeDescriptor(attribute => attribute.Name("unbound-required")))
            .TagMatchingRuleDescriptor(rule => rule
                .RequireTagName("input")
                .RequireAttributeDescriptor(attribute => attribute.Name("bound-required-string")))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("bound-required-string")
                .PropertyName("BoundRequiredString")
                .TypeName(typeof(string).FullName))
            .Build(),
        TagHelperDescriptorBuilder.CreateTagHelper("InputTagHelper2", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule
                .RequireTagName("input")
                .RequireAttributeDescriptor(attribute => attribute.Name("bound-required-int")))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("bound-required-int")
                .PropertyName("BoundRequiredInt")
                .TypeName(typeof(int).FullName))
            .Build(),
        TagHelperDescriptorBuilder.CreateTagHelper("InputTagHelper3", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("input"))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("int-dictionary")
                .PropertyName("DictionaryOfIntProperty")
                .TypeName(typeof(IDictionary<string, int>).Namespace + ".IDictionary<System.String, System.Int32>")
                .AsDictionaryAttribute("int-prefix-", typeof(int).FullName))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("string-dictionary")
                .PropertyName("DictionaryOfStringProperty")
                .TypeName(typeof(IDictionary<string, string>).Namespace + ".IDictionary<System.String, System.String>")
                .AsDictionaryAttribute("string-prefix-", typeof(string).FullName))
            .Build(),
        TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper", "SomeAssembly")
            .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("bound-string")
                .PropertyName("BoundRequiredString")
                .TypeName(typeof(string).FullName))
            .BoundAttributeDescriptor(attribute => attribute
                .Name("bound-int")
                .PropertyName("BoundRequiredString")
                .TypeName(typeof(int).FullName))
            .Build()
    ];

    [Fact]
    public void UnderstandsMinimizedAttributes_Document1()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input unbound-required />
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document2()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <p bound-string></p>
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document3()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input bound-required-string />
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document4()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input bound-required-int />
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document5()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <p bound-int></p>
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document6()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input int-dictionary/>
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document7()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input string-dictionary />
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document8()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input int-prefix- />
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document9()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input string-prefix-/>
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document10()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input int-prefix-value/>
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document11()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input string-prefix-value />
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document12()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input int-prefix-value='' />
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document13()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input string-prefix-value=''/>
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document14()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input int-prefix-value='3'/>
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document15()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input string-prefix-value='some string' />
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document16()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input unbound-required bound-required-string />
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document17()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <p bound-int bound-string></p>
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document18()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input bound-required-int unbound-required bound-required-string />
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document19()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <p bound-int bound-string bound-string></p>
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document20()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input unbound-required class='btn' />
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document21()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <p bound-string class='btn'></p>
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document22()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input class='btn' unbound-required />
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document23()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <p class='btn' bound-string></p>
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document24()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input bound-required-string class='btn' />
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document25()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input class='btn' bound-required-string />
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document26()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input bound-required-int class='btn' />
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document27()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <p bound-int class='btn'></p>
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document28()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input class='btn' bound-required-int />
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document29()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <p class='btn' bound-int></p>
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document30()
    {
        const string expression = "@DateTime.Now + 1";

        EvaluateData(MinimizedAttribute_TagHelpers, $"""
            <input class='{expression}' bound-required-int />
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document31()
    {
        const string expression = "@DateTime.Now + 1";

        EvaluateData(MinimizedAttribute_TagHelpers, $"""
            <p class='{expression}' bound-int></p>
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document32()
    {
        const string expression = "@DateTime.Now + 1";

        EvaluateData(MinimizedAttribute_TagHelpers, $"""
            <input    bound-required-int class='{expression}'   bound-required-string class='{expression}'  unbound-required  />
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Document33()
    {
        const string expression = "@DateTime.Now + 1";

        EvaluateData(MinimizedAttribute_TagHelpers, $"""
            <p    bound-int class='{expression}'   bound-string class='{expression}'  bound-string></p>
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block1()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <input unbound-required />
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block2()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <p bound-string></p>
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block3()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <input bound-required-string />
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block4()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <input bound-required-int />
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block5()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <p bound-int></p>
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block6()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <input int-dictionary/>
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block7()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <input string-dictionary />
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block8()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <input int-prefix- />
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block9()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <input string-prefix-/>
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block10()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <input int-prefix-value/>
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block11()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <input string-prefix-value />
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block12()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <input int-prefix-value='' />
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block13()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <input string-prefix-value=''/>
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block14()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <input int-prefix-value='3'/>
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block15()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <input string-prefix-value='some string' />
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block16()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <input unbound-required bound-required-string />
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block17()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <p bound-int bound-string></p>
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block18()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <input bound-required-int unbound-required bound-required-string />
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block19()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <p bound-int bound-string bound-string></p>
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block20()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <input unbound-required class='btn' />
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block21()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <p bound-string class='btn'></p>
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block22()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <input class='btn' unbound-required />
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block23()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <p class='btn' bound-string></p>
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block24()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <input bound-required-string class='btn' />
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block25()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <input class='btn' bound-required-string />
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block26()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <input bound-required-int class='btn' />
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block27()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <p bound-int class='btn'></p>
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block28()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <input class='btn' bound-required-int />
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block29()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock("""
            <p class='btn' bound-int></p>
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block30()
    {
        const string expression = "@DateTime.Now + 1";

        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock($"""
            <input class='{expression}' bound-required-int />
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block31()
    {
        const string expression = "@DateTime.Now + 1";

        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock($"""
            <p class='{expression}' bound-int></p>
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block32()
    {
        const string expression = "@DateTime.Now + 1";

        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock($"""
            <input    bound-required-int class='{expression}'   bound-required-string class='{expression}'  unbound-required  />
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_Block33()
    {
        const string expression = "@DateTime.Now + 1";

        EvaluateData(MinimizedAttribute_TagHelpers, WrapInCSharpBlock($"""
            <p    bound-int class='{expression}'   bound-string class='{expression}'  bound-string></p>
            """));
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_PartialTags1()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input unbound-required
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_PartialTags2()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input bound-required-string
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_PartialTags3()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input bound-required-int
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_PartialTags4()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input bound-required-int unbound-required bound-required-string
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_PartialTags5()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <p bound-string
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_PartialTags6()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <p bound-int
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_PartialTags7()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <p bound-int bound-string
            """);
    }

    [Fact]
    public void UnderstandsMinimizedAttributes_PartialTags8()
    {
        EvaluateData(MinimizedAttribute_TagHelpers, """
            <input bound-required-int unbound-required bound-required-string<p bound-int bound-string
            """);
    }

    [Fact]
    public void UnderstandsMinimizedBooleanBoundAttributes()
    {
        TagHelperCollection tagHelpers = [
            TagHelperDescriptorBuilder.CreateTagHelper("InputTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("input"))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("boundbool")
                    .PropertyName("BoundBoolProp")
                    .TypeName(typeof(bool).FullName))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("boundbooldict")
                    .PropertyName("BoundBoolDictProp")
                    .TypeName("System.Collections.Generic.IDictionary<string, bool>")
                    .AsDictionary("boundbooldict-", typeof(bool).FullName))
                .Build(),
        ];

        EvaluateData(tagHelpers, """
            <input boundbool boundbooldict-key />
            """);
    }

    [Fact]
    public void FeatureDisabled_AddsErrorForMinimizedBooleanBoundAttributes()
    {
        TagHelperCollection tagHelpers = [
            TagHelperDescriptorBuilder.CreateTagHelper("InputTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("input"))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("boundbool")
                    .PropertyName("BoundBoolProp")
                    .TypeName(typeof(bool).FullName))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("boundbooldict")
                    .PropertyName("BoundBoolDictProp")
                    .TypeName("System.Collections.Generic.IDictionary<string, bool>")
                    .AsDictionary("boundbooldict-", typeof(bool).FullName))
                .Build()
        ];

        EvaluateData(tagHelpers, """
            <input boundbool boundbooldict-key />
            """,
            languageVersion: RazorLanguageVersion.Version_2_0,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public void Rewrites_ComponentDirectiveAttributes()
    {
        TagHelperCollection tagHelpers = [
            TagHelperDescriptorBuilder.Create(TagHelperKind.Bind, "Bind", ComponentsApi.AssemblyName)
                .TypeName(
                    fullName: "Microsoft.AspNetCore.Components.Bind",
                    typeNamespace: "Microsoft.AspNetCore.Components",
                    typeNameIdentifier: "Bind")
                .ClassifyAttributesOnly(true)
                .Metadata(new BindMetadata()
                {
                    IsFallback = true
                })
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("*")
                    .RequireAttributeDescriptor(attribute => attribute
                        .Name("@bind-", RequiredAttributeNameComparison.PrefixMatch)
                        .IsDirectiveAttribute()))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("@bind-...")
                    .PropertyName("Bind")
                    .AsDictionaryAttribute("@bind-", typeof(object).FullName)
                    .TypeName("System.Collections.Generic.Dictionary<string, object>")
                    .IsDirectiveAttribute()
                    .BindAttributeParameter(p =>
                    {
                        p.Name = "event";
                        p.PropertyName = "Event";
                        p.TypeName = typeof(string).FullName;
                    }))
                .Build()
        ];

        EvaluateData(tagHelpers, """
            <input @bind-value="Message" @bind-value:event="onchange" />
            """,
            configureParserOptions: builder =>
            {
                builder.AllowCSharpInMarkupAttributeArea = false;
            });
    }

    [Fact]
    public void Rewrites_MinimizedComponentDirectiveAttributes()
    {
        TagHelperCollection tagHelpers = [
            TagHelperDescriptorBuilder.Create(TagHelperKind.Bind, "Bind", ComponentsApi.AssemblyName)
                .TypeName(
                    fullName: "Microsoft.AspNetCore.Components.Bind",
                    typeNamespace: "Microsoft.AspNetCore.Components",
                    typeNameIdentifier: "Bind")
                .ClassifyAttributesOnly(true)
                .Metadata(new BindMetadata()
                {
                    IsFallback = true
                })
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("*")
                    .RequireAttributeDescriptor(attribute => attribute
                        .Name("@bind-", RequiredAttributeNameComparison.PrefixMatch)
                        .IsDirectiveAttribute()))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("@bind-...")
                    .PropertyName("Bind")
                    .AsDictionaryAttribute("@bind-", typeof(object).FullName)
                    .TypeName("System.Collections.Generic.Dictionary<string, object>")
                    .IsDirectiveAttribute()
                    .BindAttributeParameter(p =>
                    {
                        p.Name = "param";
                        p.PropertyName = "Param";
                        p.TypeName = typeof(string).FullName;
                    }))
                .Build()
        ];

        EvaluateData(tagHelpers, """
            <input @bind-foo @bind-foo:param />
            """,
            configureParserOptions: builder =>
            {
                builder.AllowCSharpInMarkupAttributeArea = false;
            });
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/12261")]
    public void TagHelper_AttributeAfterRazorComment()
    {
        TagHelperCollection tagHelpers = [
            TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("attribute-1")
                    .PropertyName("Attribute1")
                    .TypeName(typeof(string).FullName))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("not-visible")
                    .PropertyName("NotVisible")
                    .TypeName(typeof(bool).FullName))
                .Build()
        ];

        EvaluateData(tagHelpers, """
            <p
              attribute-1="true"
              @* visible *@
              not-visible>
            </p>
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/12261")]
    public void TagHelper_MultipleAttributesAfterRazorComment()
    {
        TagHelperCollection tagHelpers = [
            TagHelperDescriptorBuilder.CreateTagHelper("PTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("attr-1")
                    .PropertyName("Attr1")
                    .TypeName(typeof(string).FullName))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("attr-2")
                    .PropertyName("Attr2")
                    .TypeName(typeof(string).FullName))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("attr-3")
                    .PropertyName("Attr3")
                    .TypeName(typeof(string).FullName))
                .Build()
        ];

        EvaluateData(tagHelpers, """
            <p attr-1="first" @* comment *@ attr-2="second" attr-3="third"></p>
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/12261")]
    public void TagHelper_MultipleInterleavedRazorComments()
    {
        TagHelperCollection tagHelpers = [
            TagHelperDescriptorBuilder.CreateTagHelper("InputTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("input"))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("type")
                    .PropertyName("Type")
                    .TypeName(typeof(string).FullName))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("value")
                    .PropertyName("Value")
                    .TypeName(typeof(string).FullName))
                .Build()
        ];

        EvaluateData(tagHelpers, """
            <input @* comment1 *@ type="text" @* comment2 *@ value="test" @* comment3 *@ />
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/12261")]
    public void TagHelper_MinimizedAttributeAfterRazorComment()
    {
        TagHelperCollection tagHelpers = [
            TagHelperDescriptorBuilder.CreateTagHelper("InputTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("input"))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("type")
                    .PropertyName("Type")
                    .TypeName(typeof(string).FullName))
                .BoundAttributeDescriptor(attribute => attribute
                    .Name("checked")
                    .PropertyName("Checked")
                    .TypeName(typeof(bool).FullName))
                .Build()
        ];

        EvaluateData(tagHelpers, """
            <input type="checkbox" @* comment *@ checked />
            """);
    }

    private static string WrapInCSharpBlock(string text)
        => $"@{{{text.TrimEnd()}}}";
}
