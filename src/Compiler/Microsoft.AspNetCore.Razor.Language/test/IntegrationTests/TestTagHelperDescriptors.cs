// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public static class TestTagHelperDescriptors
{
    public static TagHelperCollection SimpleTagHelperDescriptors
        =>
        [
            CreateTagHelper(
                tagName: "span",
                typeName: "SpanTagHelper",
                assemblyName: "TestAssembly"),
            CreateTagHelper(
                tagName: "div",
                typeName: "DivTagHelper",
                assemblyName: "TestAssembly"),
            CreateTagHelper(
                tagName: "input",
                typeName: "InputTagHelper",
                assemblyName: "TestAssembly",
                builder => builder
                    .BoundAttribute<string>(name: "value", propertyName: "FooProp")
                    .BoundAttribute<string>(name: "bound", propertyName: "BoundProp")
                    .BoundAttribute<int>(name: "age", propertyName: "AgeProp")
                    .BoundAttribute<bool>(name: "alive", propertyName: "AliveProp")
                    .BoundAttribute<object>(name: "tag", propertyName: "TagProp")
                    .BoundAttribute(
                        name: "tuple-dictionary",
                        propertyName: "DictionaryOfBoolAndStringTupleProperty",
                        typeName: $"{typeof(IDictionary<string, int>).Namespace}.IDictionary<System.String, (System.Boolean, System.String)>",
                        builder => builder.AsDictionaryAttribute("tuple-prefix-", typeof((bool, string)).FullName)))
        ];

    public static TagHelperCollection MinimizedBooleanTagHelperDescriptors
        =>
        [
            CreateTagHelper(
                tagName: "span",
                typeName: "SpanTagHelper",
                assemblyName: "TestAssembly"),
            CreateTagHelper(
                tagName: "div",
                typeName: "DivTagHelper",
                assemblyName: "TestAssembly"),
            CreateTagHelper(
                tagName: "input",
                typeName: "InputTagHelper",
                assemblyName: "TestAssembly",
                builder => builder
                    .BoundAttribute<string>(name: "value", propertyName: "FooProp")
                    .BoundAttribute<bool>(name: "bound", propertyName: "BoundProp")
                    .BoundAttribute<int>(name: "age", propertyName: "AgeProp"))
        ];

    public static TagHelperCollection CssSelectorTagHelperDescriptors
        =>
        [
            CreateTagHelper(
                typeName: "TestNamespace.ATagHelper",
                assemblyName: "TestAssembly",
                builder => builder
                    .TagMatchingRule("a", builder => builder
                        .AddAttribute(
                            name: "href", RequiredAttributeNameComparison.FullMatch,
                            value: "~/", RequiredAttributeValueComparison.FullMatch))),
            CreateTagHelper(
                typeName: "TestNamespace.ATagHelperMultipleSelectors",
                assemblyName: "TestAssembly",
                builder => builder
                    .TagMatchingRule("a", builder => builder
                        .AddAttribute(
                            name: "href", RequiredAttributeNameComparison.FullMatch,
                            value: "~/", RequiredAttributeValueComparison.PrefixMatch)
                        .AddAttribute(
                            name: "href", RequiredAttributeNameComparison.FullMatch,
                            value: "?hello=world", RequiredAttributeValueComparison.SuffixMatch))),
            CreateTagHelper(
                typeName: "TestNamespace.InputTagHelper",
                assemblyName: "TestAssembly",
                builder => builder
                    .BoundAttribute("type", s_typePropertyInfo)
                    .TagMatchingRule("input", builder => builder
                        .AddAttribute(
                            name: "type", RequiredAttributeNameComparison.FullMatch,
                            value: "text", RequiredAttributeValueComparison.FullMatch))),
            CreateTagHelper(
                typeName: "TestNamespace.InputTagHelper2",
                assemblyName: "TestAssembly",
                builder => builder
                    .BoundAttribute("type", s_typePropertyInfo)
                    .TagMatchingRule("input", builder => builder
                        .AddAttribute("ty", RequiredAttributeNameComparison.PrefixMatch))),
            CreateTagHelper(
                typeName: "TestNamespace.CatchAllTagHelper",
                assemblyName: "TestAssembly",
                builder => builder
                    .TagMatchingRule("*", builder => builder
                        .AddAttribute(
                            name: "href", RequiredAttributeNameComparison.FullMatch,
                            value: "~/", RequiredAttributeValueComparison.PrefixMatch))),
            CreateTagHelper(
                typeName: "TestNamespace.CatchAllTagHelper2",
                assemblyName: "TestAssembly",
                builder => builder
                    .TagMatchingRule("*", builder => builder
                        .AddAttribute("type", RequiredAttributeNameComparison.FullMatch)))
        ];

    public static TagHelperCollection EnumTagHelperDescriptors
        =>
        [
            CreateTagHelper(
                tagName: "*",
                typeName: "TestNamespace.CatchAllTagHelper",
                assemblyName: "TestAssembly",
                builder => builder
                    .BoundAttribute("catch-all", propertyName: "CatchAll", typeName: "Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestTagHelperDescriptors.MyEnum",
                        builder => builder
                            .AsEnum())),
            CreateTagHelper(
                tagName: "input",
                typeName: "TestNamespace.InputTagHelper",
                assemblyName: "TestAssembly",
                builder => builder
                    .BoundAttribute("value", propertyName: "Value", typeName: "Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestTagHelperDescriptors.MyEnum",
                        builder => builder
                            .AsEnum()))
        ];

    public static TagHelperCollection SymbolBoundTagHelperDescriptors
        =>
        [
            CreateTagHelper(
                typeName: "TestNamespace.CatchAllTagHelper",
                assemblyName: "TestAssembly",
                builder => builder
                    .BoundAttribute(name: "[item]", propertyName: "ListItems", typeName: "System.Collections.Generic.List<string>")
                    .BoundAttribute<string[]>(name: "[(item)]", propertyName: "ArrayItems")
                    .BoundAttribute<Action>(name: "(click)", propertyName: "Event1")
                    .BoundAttribute<Action>(name: "(^click)", propertyName: "Event2")
                    .BoundAttribute<string>(name: "*something", propertyName: "StringProperty1")
                    .BoundAttribute<string>(name: "#local", propertyName: "StringProperty2")
                    .TagMatchingRule("*", builder => builder
                        .AddAttribute("bound")))
        ];

    public static TagHelperCollection MinimizedTagHelpers_Descriptors
        =>
        [
            CreateTagHelper(
                typeName: "TestNamespace.CatchAllTagHelper",
                assemblyName: "TestAssembly",
                builder => builder
                    .BoundAttribute<string>(name: "catchall-bound-string", propertyName: "BoundRequiredString")
                    .TagMatchingRule("*", builder => builder
                        .AddAttribute("catchall-unbound-required"))),
            CreateTagHelper(
                typeName: "TestNamespace.InputTagHelper",
                assemblyName: "TestAssembly",
                builder => builder
                    .BoundAttribute<string>(name: "input-bound-required-string", propertyName: "BoundRequiredString")
                    .BoundAttribute<string>(name: "input-bound-string", propertyName: "BoundString")
                    .TagMatchingRule("input", builder => builder
                        .AddAttribute("input-bound-required-string"))
                    .TagMatchingRule("input", builder => builder
                        .AddAttribute("input-unbound-required"))),
            CreateTagHelper(
                tagName: "div",
                typeName: "DivTagHelper",
                assemblyName: "TestAssembly",
                builder => builder
                    .BoundAttribute<bool>(name: "boundbool", propertyName: "BoundBoolProp")
                    .BoundAttribute(name: "booldict", propertyName: "BoolDictProp", typeName: "System.Collections.Generic.IDictionary<string, bool>", builder => builder
                        .AsDictionaryAttribute("booldict-prefix-", typeof(bool).FullName)))
        ];

    public static TagHelperCollection DynamicAttributeTagHelpers_Descriptors
        =>
        [
            CreateTagHelper(
                tagName: "input",
                typeName: "TestNamespace.InputTagHelper",
                assemblyName: "TestAssembly",
                builder => builder
                    .BoundAttribute<string>(name: "bound", propertyName: "Bound"))
        ];

    public static TagHelperCollection DuplicateTargetTagHelperDescriptors
        =>
        [
            CreateTagHelper(
                typeName: "TestNamespace.CatchAllTagHelper",
                assemblyName: "TestAssembly",
                builder => builder
                    .BoundAttribute("type", s_typePropertyInfo)
                    .BoundAttribute("checked", s_checkedPropertyInfo)
                    .TagMatchingRule("*", builder => builder
                        .AddAttribute("type"))
                    .TagMatchingRule("*", builder => builder
                        .AddAttribute("checked"))),
            CreateTagHelper(
                typeName: "TestNamespace.InputTagHelper",
                assemblyName: "TestAssembly",
                builder => builder
                    .BoundAttribute("type", s_typePropertyInfo)
                    .BoundAttribute("checked", s_checkedPropertyInfo)
                    .TagMatchingRule("input", builder => builder
                        .AddAttribute("type"))
                    .TagMatchingRule("input", builder => builder
                        .AddAttribute("checked"))),
        ];

    public static TagHelperCollection AttributeTargetingTagHelperDescriptors
        =>
        [
        CreateTagHelper(
            typeName: "TestNamespace.PTagHelper",
            assemblyName: "TestAssembly",
            builder => builder
                .TagMatchingRule("p", builder => builder
                    .AddAttribute("class"))),
        CreateTagHelper(
            typeName: "TestNamespace.InputTagHelper",
            assemblyName: "TestAssembly",
            builder => builder
                .BoundAttribute("type", s_typePropertyInfo)
                .TagMatchingRule("input", builder => builder
                    .AddAttribute("type"))),
        CreateTagHelper(
            typeName: "TestNamespace.InputTagHelper2",
            assemblyName: "TestAssembly",
            builder => builder
                .BoundAttribute("type", s_typePropertyInfo)
                .BoundAttribute("checked", s_checkedPropertyInfo)
                .TagMatchingRule("input", builder => builder
                    .AddAttribute("type")
                    .AddAttribute("checked"))),
        CreateTagHelper(
            typeName: "TestNamespace.CatchAllTagHelper",
            assemblyName: "TestAssembly",
            builder => builder
                .TagMatchingRule("*", builder => builder
                    .AddAttribute("catchAll"))),
    ];

    public static TagHelperCollection PrefixedAttributeTagHelperDescriptors
        =>
        [
            CreateTagHelper(
                tagName: "input",
                typeName: "TestNamespace.InputTagHelper1",
                assemblyName: "TestAssembly",
                builder => builder
                    .BoundAttribute<int>(name: "int-prefix-grabber", propertyName: "IntProperty")
                    .BoundAttribute(
                        name: "int-dictionary",
                        propertyName: "IntDictionaryProperty",
                        typeName: "System.Collections.Generic.IDictionary<string, int>", builder => builder
                            .AsDictionaryAttribute("int-prefix-", typeof(int).FullName))
                    .BoundAttribute<string>(name: "string-prefix-grabber", propertyName: "StringProperty")
                    .BoundAttribute(
                        name: "string-dictionary",
                        propertyName: "StringDictionaryProperty",
                        typeName: "Namespace.DictionaryWithoutParameterlessConstructor<string, string>", builder => builder
                            .AsDictionaryAttribute("string-prefix-", typeof(string).FullName))),
            CreateTagHelper(
                tagName: "input",
                typeName: "TestNamespace.InputTagHelper2",
                assemblyName: "TestAssembly",
                builder => builder
                    .BoundAttribute(
                        name: "int-dictionary",
                        propertyName: "IntDictionaryProperty",
                        typeName: typeof(int).FullName, builder => builder
                            .AsDictionaryAttribute("int-prefix-", typeof(int).FullName))
                    .BoundAttribute(
                        name: "string-dictionary",
                        propertyName: "StringDictionaryProperty",
                        typeName: "Namespace.DictionaryWithoutParameterlessConstructor<string, string>", builder => builder
                            .AsDictionaryAttribute("string-prefix-", typeof(string).FullName)))
        ];

    public static TagHelperCollection TagHelpersInSectionDescriptors
        =>
        [
            CreateTagHelper(
                tagName: "MyTagHelper",
                typeName: "TestNamespace.MyTagHelper",
                assemblyName: "TestAssembly",
                builder => builder
                    .BoundAttribute("BoundProperty", s_boundPropertyInfo)),
            CreateTagHelper(
                tagName: "NestedTagHelper",
                typeName: "TestNamespace.NestedTagHelper",
                assemblyName: "TestAssembly"),
        ];

    public static TagHelperCollection DefaultPAndInputTagHelperDescriptors
        =>
        [
            CreateTagHelper(
                typeName: "TestNamespace.PTagHelper",
                assemblyName: "TestAssembly",
                builder => builder
                    .BoundAttribute("age", s_agePropertyInfo)
                    .AddTagMatchingRule("p", tagStructure: TagStructure.NormalOrSelfClosing)),
            CreateTagHelper(
                typeName: "TestNamespace.InputTagHelper",
                assemblyName: "TestAssembly",
                builder => builder
                    .BoundAttribute("type", s_typePropertyInfo)
                    .AddTagMatchingRule("input", tagStructure: TagStructure.WithoutEndTag)),
            CreateTagHelper(
                tagName: "input",
                typeName: "TestNamespace.InputTagHelper2",
                assemblyName: "TestAssembly",
                configure: builder => builder
                    .BoundAttribute("type", s_typePropertyInfo)
                    .BoundAttribute("checked", s_checkedPropertyInfo))
        ];

    private static TagHelperDescriptor CreateTagHelper(
        string typeName,
        string assemblyName,
        Action<TagHelperDescriptorBuilder>? configure = null)
    {
        var builder = TagHelperDescriptorBuilder.CreateTagHelper(typeName, assemblyName);
        builder.SetTypeName(typeName, typeNamespace: null, typeNameIdentifier: null);

        configure?.Invoke(builder);

        return builder.Build();
    }

    private static TagHelperDescriptor CreateTagHelper(
        string tagName,
        string typeName,
        string assemblyName,
        Action<TagHelperDescriptorBuilder>? configure = null)
    {
        var builder = TagHelperDescriptorBuilder.CreateTagHelper(typeName, assemblyName);
        builder.SetTypeName(typeName, typeNamespace: null, typeNameIdentifier: null);

        builder.AddTagMatchingRule(tagName);
        configure?.Invoke(builder);

        return builder.Build();
    }

    private static readonly PropertyInfo s_agePropertyInfo = GetTestTypeRuntimeProperty("Age");
    private static readonly PropertyInfo s_boundPropertyInfo = GetTestTypeRuntimeProperty("BoundProperty");
    private static readonly PropertyInfo s_typePropertyInfo = GetTestTypeRuntimeProperty("Type");
    private static readonly PropertyInfo s_checkedPropertyInfo = GetTestTypeRuntimeProperty("Checked");

    private static PropertyInfo GetTestTypeRuntimeProperty(string name)
    {
        var result = typeof(TestType).GetRuntimeProperty(name);
        Assert.NotNull(result);

        return result;
    }

    private class TestType
    {
        public int Age { get; set; }

        public string? Type { get; set; }

        public bool Checked { get; set; }

        public string? BoundProperty { get; set; }
    }

    public static readonly string Code = """
        namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
        {
            public class TestTagHelperDescriptors
            {
                public enum MyEnum
                {
                    MyValue,
                    MySecondValue
                }
            }
        }
        """;
}
