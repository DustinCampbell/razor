// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

internal static class ContentExtensions
{
    public static bool IsNullOrWhiteSpace(this Content? content)
        => content is null || content.GetValueOrDefault().IsEmptyOrWhiteSpace();

    public static StringBuilder Append(this StringBuilder builder, Content content)
    {
        foreach (var part in content.AllParts)
        {
#if NET
            builder.Append(part);
#else
            unsafe
            {
                fixed (char* ptr = part.Span)
                {
                    builder.Append(ptr, part.Length);
                }
            }
#endif
        }

        return builder;
    }
}
