// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal sealed class DesignTimeDirectiveIntermediateNode : ExtensionIntermediateNode
{
    private const string DirectiveTokenHelperMethodName = "__RazorDirectiveTokenHelpers__";
    private const string TypeHelper = "__typeHelper";

    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public override void Accept(IntermediateNodeVisitor visitor)
        => AcceptExtensionNode(this, visitor);

    public override void WriteNode(CodeTarget target, CodeRenderingContext context)
    {
        context.CodeWriter
            .WriteLine("#pragma warning disable 219")
            .WriteLine($"private void {DirectiveTokenHelperMethodName}() {{");

        foreach (var child in Children)
        {
            if (child is DirectiveTokenIntermediateNode directiveTokenNode)
            {
                WriteDesignTimeDirectiveToken(directiveTokenNode, context);
            }
        }

        context.CodeWriter
            .WriteLine("}")
            .WriteLine("#pragma warning restore 219");
    }

    private static void WriteDesignTimeDirectiveToken(DirectiveTokenIntermediateNode node, CodeRenderingContext context)
    {
        if (node.Source is not SourceSpan source ||
            !string.Equals(context.SourceDocument.FilePath, source.FilePath, StringComparison.OrdinalIgnoreCase))
        {
            // We don't want to handle directives from imports.
            return;
        }

        var tokenKind = node.DirectiveToken.Kind;

        if (tokenKind == DirectiveTokenKind.Attribute)
        {
            // We don't need to do anything special here.
            // We let the Roslyn take care of providing syntax errors for C# attributes.
            return;
        }

        // Wrap the directive token in a lambda to isolate variable names.
        var writer = context.CodeWriter;

        writer.Write($"((global::{typeof(Action).FullName})(");

        using (writer.BuildLambda())
        {
            var originalIndent = writer.CurrentIndent;
            writer.CurrentIndent = 0;
            switch (tokenKind)
            {
                case DirectiveTokenKind.Type:

                    if (node.Content.IsNullOrEmpty())
                    {
                        // This is most likely a marker token.
                        WriteMarkerToken(context, node);
                        break;
                    }

                    // {node.Content} __typeHelper = default({node.Content});
                    using (writer.BuildLinePragma(source, context))
                    {
                        context.AddSourceMappingFor(node);
                        writer.Write($"{node.Content} {TypeHelper} = default");

                        if (!context.Options.SuppressNullabilityEnforcement)
                        {
                            writer.Write("!");
                        }

                        writer.WriteLine(";");
                    }

                    break;

                case DirectiveTokenKind.Member:

                    if (string.IsNullOrEmpty(node.Content))
                    {
                        // This is most likely a marker token.
                        WriteMarkerToken(context, node);
                        break;
                    }

                    // Type parameters are mapped to actual source, so no need to generate design-time code for them here
                    if (node.DirectiveToken.Name == ComponentResources.TypeParamDirective_Token_Name)
                    {
                        break;
                    }

                    // global::System.Object {node.content} = null;
                    using (writer.BuildLinePragma(source, context))
                    {
                        writer.Write($"global::{typeof(object).FullName} ");
                        context.AddSourceMappingFor(node);
                        writer.Write($"{node.Content} = null");

                        if (!context.Options.SuppressNullabilityEnforcement)
                        {
                            writer.Write("!");
                        }

                        writer.WriteLine(";");
                    }
                    break;

                case DirectiveTokenKind.Namespace or DirectiveTokenKind.IdentifierOrExpression:

                    if (string.IsNullOrEmpty(node.Content))
                    {
                        // This is most likely a marker token.
                        WriteMarkerToken(context, node);
                        break;
                    }

                    // global::System.Object __typeHelper = nameof({node.Content});
                    using (writer.BuildLinePragma(source, context))
                    {
                        writer.Write($"global::{typeof(object).FullName} {TypeHelper} = nameof(");
                        context.AddSourceMappingFor(node);
                        writer.WriteLine($"{node.Content});");
                    }

                    break;

                case DirectiveTokenKind.String:

                    // Add a string syntax to the directive if the document is a Razor page or a Blazor component.
                    // language=Route tells Roslyn that this string is a route template. A classifier that's
                    // part of ASP.NET Core will run that colorizes the route string.
                    var stringSyntax = context.DocumentKind switch
                    {
                        "mvc.1.0.razor-page" => "Route",
                        "component.1.0" => "Route,Component",
                        _ => null
                    };

                    if (stringSyntax is not null)
                    {
                        writer.Write($"// language={stringSyntax}");
                    }

                    // global::System.Object __typeHelper = "{node.Content}";
                    using (writer.BuildLinePragma(source, context))
                    {
                        writer.Write($"global::{typeof(object).FullName} {TypeHelper} = ");

                        if (node.Content.StartsWith('"'))
                        {
                            context.AddSourceMappingFor(node);
                            writer.Write(node.Content);
                        }
                        else
                        {
                            writer.Write("\"");
                            context.AddSourceMappingFor(node);
                            writer.Write($"{node.Content}\"");
                        }

                        writer.WriteLine(";");
                    }
                    break;

                case DirectiveTokenKind.Boolean:
                    // global::System.Boolean __typeHelper = {node.Content};
                    using (writer.BuildLinePragma(source, context))
                    {
                        writer.Write($"global::{typeof(bool).FullName} {TypeHelper} = ");
                        context.AddSourceMappingFor(node);
                        writer.WriteLine($"{node.Content};");
                    }

                    break;
            }
            writer.CurrentIndent = originalIndent;
        }

        writer.WriteLine("))();");
    }

    private static void WriteMarkerToken(CodeRenderingContext context, DirectiveTokenIntermediateNode node)
    {
        // Marker tokens exist to be filled with other content a user might write. In an end-to-end
        // scenario markers prep the Razor documents C# projections to have an empty projection that
        // can be filled with other user content. This content can trigger a multitude of other events,
        // such as completion. In the case of completion, a completion session can occur when a marker
        // hasn't been filled and then we will fill it as a user types. The line pragma is necessary
        // for consistency so when a C# completion session starts, filling user code doesn't result in
        // a previously non-existent line pragma from being added and destroying the context in which
        // the completion session was started.
        using (context.CodeWriter.BuildLinePragma(node.Source, context))
        {
            context.AddSourceMappingFor(node);
            context.CodeWriter.Write(" ");
        }
    }
}
