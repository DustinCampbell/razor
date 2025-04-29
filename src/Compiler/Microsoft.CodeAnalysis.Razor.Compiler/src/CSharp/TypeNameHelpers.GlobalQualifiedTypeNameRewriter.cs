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
        private readonly Visitor _visitor = new(typeParameterNames);

        public override string Rewrite(string typeName)
        {
            var parsed = SyntaxFactory.ParseTypeName(typeName);
            var rewritten = _visitor.Visit(parsed);

            return rewritten.ToFullString();
        }

        private sealed class Visitor(HashSet<string> typeParameterNames) : CSharpSyntaxRewriter
        {
            private const string DynamicKeyword = "dynamic";

            private static readonly IdentifierNameSyntax s_globalIdentifierName =
                SyntaxFactory.IdentifierName(
                    SyntaxFactory.Token(CSharp.SyntaxKind.GlobalKeyword));

            // List of names to ignore.
            //
            // NOTE: this is the list of type parameters defined on the component.
            private readonly HashSet<string> _typeParameterNames = typeParameterNames;

            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                // If global:: isn't allowed to the left of this identifier, we're done.
                if (!IsGlobalKeywordAllowed(node))
                {
                    return node;
                }

                var identifier = node.Identifier;

                // If the identifier is a keyword of any kind, 'dynamic', or a known type parameter name, we're done.
                if (identifier.IsKeyword() ||
                    identifier.Text == DynamicKeyword ||
                    _typeParameterNames.Contains(identifier.Text))
                {
                    return node;
                }

                // ... otherwise, go ahead and globally-qualify this identifier.
                return SyntaxFactory.AliasQualifiedName(alias: s_globalIdentifierName, name: node);
            }

            private static bool IsGlobalKeywordAllowed(IdentifierNameSyntax identifierName)
            {
                // global:: is syntactically allowed to the left of this identifier if it...
                //
                // 1. doesn't have a parent, e.g. |List
                // 2. is a generic type argument, e.g. List<|X>
                // 3. is the left side of qualified name, e.g. |System.Collections

                return identifierName.Parent is null or TypeArgumentListSyntax ||
                      (identifierName.Parent is QualifiedNameSyntax { Left: var left } && left == identifierName);
            }
        }
    }
}
