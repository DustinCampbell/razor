// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal sealed partial class MarkupMinimizedTagHelperDirectiveAttributeSyntax
{
    public string FullName
    {
        get
        {
            return field ??= string.Build((Transition, Name, Colon, ParameterName), AppendContent);

            static void AppendContent(
                ref MemoryBuilder<ReadOnlyMemory<char>> builder,
                (RazorMetaCodeSyntax, MarkupTextLiteralSyntax, RazorMetaCodeSyntax?, MarkupTextLiteralSyntax?) state)
            {
                var (transition, name, colon, parameterName) = state;

                transition.AppendContent(ref builder);
                name.AppendContent(ref builder);
                colon?.AppendContent(ref builder);
                parameterName?.AppendContent(ref builder);
            }
        }
    }
}
