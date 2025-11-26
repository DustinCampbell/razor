// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Test.Common;

internal static class SimpleTagHelpers
{
    public static TagHelperCollection Default { get; }

    static SimpleTagHelpers()
    {
        var builder1 = TagHelperDescriptorBuilder.CreateTagHelper("Test1TagHelper", "TestAssembly")
            .TypeName("Test1TagHelper")
            .AddTagMatchingRule("test1")
            .BoundAttribute<bool>(name: "bool-val", propertyName: "BoolVal")
            .BoundAttribute<int>(name: "int-val", propertyName: "IntVal");

        var builder1WithRequiredParent = TagHelperDescriptorBuilder.CreateTagHelper("Test1TagHelper.SomeChild", "TestAssembly")
            .TypeName("Test1TagHelper.SomeChild")
            .AddTagMatchingRule(tagName: "SomeChild", parentTagName: "test1")
            .BoundAttribute<string>(name: "attribute", propertyName: "Attribute");

        var builder2 = TagHelperDescriptorBuilder.CreateTagHelper("Test2TagHelper", "TestAssembly")
            .TypeName("Test2TagHelper")
            .AddTagMatchingRule("test2")
            .BoundAttribute<bool>(name: "bool-val", propertyName: "BoolVal")
            .BoundAttribute<int>(name: "int-val", propertyName: "IntVal");

        var builder3 = TagHelperDescriptorBuilder.CreateComponent("Component1TagHelper", "TestAssembly")
            .TypeName(fullName: "System.Component1", typeNamespace: "System", typeNameIdentifier: "Component1")
            .AddTagMatchingRule("Component1")
            .BoundAttribute<bool>(name: "bool-val", propertyName: "BoolVal")
            .BoundAttribute<int>(name: "int-val", propertyName: "IntVal")
            .BoundAttribute<string>(name: "Title", propertyName: "Title")
            .IsFullyQualifiedNameMatch(true);

        var textComponent = TagHelperDescriptorBuilder.CreateComponent("TextTagHelper", "TestAssembly")
            .TypeName(fullName: "System.Text", typeNamespace: "System", typeNameIdentifier: "Text")
            .AddTagMatchingRule("Text")
            .IsFullyQualifiedNameMatch(true);

        var directiveAttribute1 = TagHelperDescriptorBuilder.CreateComponent("TestDirectiveAttribute", "TestAssembly")
            .TypeName("TestDirectiveAttribute")
            .TagMatchingRule("*", builder => builder
                .AddAttribute("@test", RequiredAttributeNameComparison.PrefixMatch))
            .TagMatchingRule("*", builder => builder
                .AddAttribute("@test", RequiredAttributeNameComparison.FullMatch))
            .BoundAttribute<string>(name: "@test", propertyName: "Test", builder => builder
                .IsDirectiveAttribute(true)
                .BindAttributeParameter(parameter =>
                {
                    parameter.Name = "something";
                    parameter.PropertyName = "Something";
                    parameter.TypeName = typeof(string).FullName;
                }))
            .IsFullyQualifiedNameMatch(true)
            .ClassifyAttributesOnly(true);

        var directiveAttribute2 = TagHelperDescriptorBuilder.CreateComponent("MinimizedDirectiveAttribute", "TestAssembly")
        .TypeName("TestDirectiveAttribute")
        .TagMatchingRule("*", builder => builder
            .AddAttribute("@minimized", RequiredAttributeNameComparison.PrefixMatch))
        .TagMatchingRule("*", builder => builder
            .AddAttribute("@minimized", RequiredAttributeNameComparison.FullMatch))
        .BoundAttribute<bool>(name: "@minimized", propertyName: "Minimized", builder => builder
            .IsDirectiveAttribute(true)
            .BindAttributeParameter(parameter =>
            {
                parameter.Name = "something";
                parameter.PropertyName = "Something";
                parameter.TypeName = typeof(string).FullName;
            }))
        .IsFullyQualifiedNameMatch(true)
        .ClassifyAttributesOnly(true);

        var directiveAttribute3 = TagHelperDescriptorBuilder.CreateEventHandler("OnClickDirectiveAttribute", "TestAssembly")
            .TypeName(
                fullName: "Microsoft.AspNetCore.Components.Web.EventHandlers",
                typeNamespace: "Microsoft.AspNetCore.Components.Web",
                typeNameIdentifier: "EventHandlers")
            .TagMatchingRule("*", builder => builder
                .AddAttribute("@onclick", RequiredAttributeNameComparison.FullMatch, isDirectiveAttribute: true))
            .TagMatchingRule("*", builder => builder
                .AddAttribute("@onclick", RequiredAttributeNameComparison.PrefixMatch, isDirectiveAttribute: true))
            .BoundAttribute(name: "@onclick", propertyName: "onclick", typeName: "Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>", builder =>
            {
                builder.IsDirectiveAttribute = true;
                builder.IsWeaklyTyped = true;
            })
            .IsFullyQualifiedNameMatch(true)
            .ClassifyAttributesOnly(true)
            .Metadata(new EventHandlerMetadata()
            {
                EventArgsType = "Microsoft.AspNetCore.Components.Web.MouseEventArgs"
            });

        var htmlTagMutator = TagHelperDescriptorBuilder.CreateTagHelper("HtmlMutator", "TestAssembly")
            .TypeName("HtmlMutator")
            .TagMatchingRule("title", builder => builder
                .AddAttribute("mutator"))
            .BoundAttribute<bool>(name: "Extra", propertyName: "Extra");

        Default =
        [
            builder1.Build(),
            builder1WithRequiredParent.Build(),
            builder2.Build(),
            builder3.Build(),
            textComponent.Build(),
            directiveAttribute1.Build(),
            directiveAttribute2.Build(),
            directiveAttribute3.Build(),
            htmlTagMutator.Build(),
        ];
    }
}
