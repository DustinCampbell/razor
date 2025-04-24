// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CSharpSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace Microsoft.CodeAnalysis.Razor;

internal static partial class TypeNameHelpers
{
    public static ImmutableArray<string> ParseTypeArguments(string typeName)
    {
        var type = SyntaxFactory.ParseTypeName(typeName);

        if (type is IdentifierNameSyntax)
        {
            return [];
        }

        using var builder = new PooledArrayBuilder<string>();

        CollectTypeArguments(type, ref builder.AsRef());

        return builder.DrainToImmutable();

        static void CollectTypeArguments(TypeSyntax type, ref PooledArrayBuilder<string> builder)
        {
            switch (type.Kind())
            {
                case CSharpSyntaxKind.IdentifierName:
                    var identifierName = (IdentifierNameSyntax)type;
                    if (identifierName.Parent is not QualifiedNameSyntax)
                    {
                        builder.Add(identifierName.Identifier.Text);
                    }

                    break;

                case CSharpSyntaxKind.QualifiedName:
                    CollectTypeArguments(((QualifiedNameSyntax)type).Right, ref builder);
                    break;

                case CSharpSyntaxKind.AliasQualifiedName:
                    CollectTypeArguments(((AliasQualifiedNameSyntax)type).Name, ref builder);
                    break;

                case CSharpSyntaxKind.GenericName:
                    foreach (var typeArgument in ((GenericNameSyntax)type).TypeArgumentList.Arguments)
                    {
                        CollectTypeArguments(typeArgument, ref builder);
                    }

                    break;

                case CSharpSyntaxKind.ArrayType:
                    CollectTypeArguments(((ArrayTypeSyntax)type).ElementType, ref builder);
                    break;

                case CSharpSyntaxKind.TupleType:
                    foreach (var element in ((TupleTypeSyntax)type).Elements)
                    {
                        CollectTypeArguments(element.Type, ref builder);
                    }

                    break;
            }
        }
    }

    public static TypeNameRewriter CreateGenericTypeRewriter(Dictionary<string, string> bindings)
    {
        if (bindings == null)
        {
            throw new ArgumentNullException(nameof(bindings));
        }

        return new GenericTypeNameRewriter(bindings);
    }

    public static TypeNameRewriter CreateGlobalQualifiedTypeNameRewriter(HashSet<string> typeParameterNames)
    {
        return new GlobalQualifiedTypeNameRewriter(typeParameterNames);
    }

    public static bool IsLambda(string expression)
    {
        var parsed = SyntaxFactory.ParseExpression(expression);
        return parsed is LambdaExpressionSyntax;
    }
}
