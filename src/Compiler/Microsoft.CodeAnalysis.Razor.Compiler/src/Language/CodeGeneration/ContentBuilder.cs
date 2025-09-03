// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

internal ref struct ContentBuilder(int initialCapacity = 4)
{
    private MemoryBuilder<Content> _builder = new(initialCapacity);

    public Content[] ToArrayAndClear()
    {
        var result = _builder.AsMemory().ToArray();

        _builder.Dispose();

        return result;
    }

    public void AppendAllContentAndClear(ref MemoryBuilder<Content> builder)
    {
        foreach (var part in _builder.AsMemory().Span)
        {
            builder.Append(part);
        }

        _builder.Dispose();
    }

    public void WriteAllContentAndClear(CodeWriter writer)
    {
        foreach (var part in _builder.AsMemory().Span)
        {
            part.WriteTo(writer);
        }

        _builder.Dispose();
    }

    public void Append(Content content)
    {
        _builder.Append(content);
    }

    public void Append(ref ContentInterpolatedStringHandler handler)
    {
        handler.AppendAllContentAndClear(ref _builder);
    }
}
