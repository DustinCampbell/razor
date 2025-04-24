// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.Razor;

internal static partial class TypeNameHelpers
{
    // Rewrites type names to use the 'global::' prefix for identifiers.
    //
    // This is useful when we're generating code in a different namespace than
    // what the user code lives in. When we synthesize a namespace it's easy to have
    // clashes.
    private sealed class GlobalQualifiedTypeNameRewriter(HashSet<string> typeParameterNames) : TypeNameRewriter
    {
        // List of names to ignore.
        //
        // NOTE: this is the list of type parameters defined on the component.
        private readonly HashSet<string> _typeParameterNames = typeParameterNames;

        public override string Rewrite(string typeName)
        {
            var parsed = SyntaxFactory.ParseTypeName(typeName);
            var rewritten = (TypeSyntax)new Visitor(_typeParameterNames).Visit(parsed);
            return rewritten.ToFullString();
        }

        private sealed class Visitor(HashSet<string> typeParameterNames) : CSharpSyntaxRewriter
        {
            private readonly HashSet<string> _typeParameterNames = typeParameterNames;

            public override SyntaxNode? VisitQualifiedName(QualifiedNameSyntax node)
            {
                if (node.Parent is QualifiedNameSyntax)
                {
                    return base.VisitQualifiedName(node);
                }

                // Need to rewrite postorder so we can rewrite the names of generic type arguments.
                var newNode = (QualifiedNameSyntax?)base.VisitQualifiedName(node);

                if (newNode is null)
                {
                    return null;
                }

                // Rewriting these is complicated, best to just tostring and parse again.
                return SyntaxFactory
                    .ParseTypeName(IsAliasQualified(newNode) ? newNode.ToString() : "global::" + newNode.ToString())
                    .WithTriviaFrom(node);

                static bool IsAliasQualified(QualifiedNameSyntax? node)
                {
                    while (node is not null)
                    {
                        if (node.Left is AliasQualifiedNameSyntax)
                        {
                            return true;
                        }

                        node = node.Left as QualifiedNameSyntax;
                    }

                    return false;
                }
            }

            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                if (_typeParameterNames.Contains(node.ToString()))
                {
                    return node;
                }

                if (node.Parent != null)
                {
                    return node;
                }

                return SyntaxFactory.AliasQualifiedName(
                    alias: SyntaxFactory.IdentifierName(
                        identifier: SyntaxFactory.Token(CSharp.SyntaxKind.GlobalKeyword)),
                    name: node);
            }
        }
    }
}
