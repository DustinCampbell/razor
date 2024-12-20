// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using MessagePack;
using MessagePack.Formatters;
using Microsoft.AspNetCore.Razor.Serialization.MessagePack.Formatters;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Razor.Serialization.MessagePack.Resolvers;

internal sealed class CSharpParseOptionsResolver : IFormatterResolver
{
    public static readonly CSharpParseOptionsResolver Instance = new();

    private CSharpParseOptionsResolver()
    {
    }

    public IMessagePackFormatter<T>? GetFormatter<T>()
    {
        return Cache<T>.Formatter;
    }

    private static class Cache<T>
    {
        public static readonly IMessagePackFormatter<T>? Formatter;

        static Cache()
        {
            if (typeof(T) == typeof(CSharpParseOptions))
            {
                Formatter = (IMessagePackFormatter<T>)CSharpParseOptionsFormatter.Instance;
            }
        }
    }
}
