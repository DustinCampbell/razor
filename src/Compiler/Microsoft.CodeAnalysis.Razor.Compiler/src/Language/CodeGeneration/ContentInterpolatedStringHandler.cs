// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

[EditorBrowsable(EditorBrowsableState.Never)]
[InterpolatedStringHandler]
public ref struct ContentInterpolatedStringHandler
{
    private ContentBuilder _builder;

    public ContentInterpolatedStringHandler(int literalLength, int formattedCount)
    {
        _builder = new ContentBuilder(initialCapacity: Math.Max(formattedCount, 4));
    }

    public Content[] ToArray()
        => _builder.ToArrayAndClear();

    internal void AppendAllContentAndClear(ref MemoryBuilder<Content> builder)
        => _builder.AppendAllContentAndClear(ref builder);

    public void WriteAllContentAndClear(CodeWriter writer)
        => _builder.WriteAllContentAndClear(writer);

    public void AppendLiteral(string value)
    {
        if (value is not null)
        {
            _builder.Append(value);
        }
    }

    public void AppendFormatted<T>(T value)
    {
        if (value is null)
        {
            return;
        }

        switch (value)
        {
            case Content content:
                _builder.Append(content);
                break;

            case ReadOnlyMemory<char> memory:
                _builder.Append(memory);
                break;

            case string s:
                _builder.Append(s);
                break;

            case ImmutableArray<Content> array:
                _builder.Append(array);
                break;

            case ImmutableArray<ReadOnlyMemory<char>> array:
                _builder.Append(array);
                break;

            case ImmutableArray<string> array:
                _builder.Append(array);
                break;

            case BuilderVariableName name:
                name.AppendTo(ref _builder);
                break;

            case RenderModeVariableName name:
                name.AppendTo(ref _builder);
                break;

            case FormNameVariableName name:
                name.AppendTo(ref _builder);
                break;

            case ComponentNodeWriter.SeqName name:
                name.AppendTo(ref _builder);
                break;

            case ComponentNodeWriter.ParameterName name:
                name.AppendTo(ref _builder);
                break;

            case ComponentNodeWriter.TypeInferenceArgName name:
                name.AppendTo(ref _builder);
                break;

            case IWriteableValue writeableValue:
                Debug.Assert(!typeof(T).IsValueType, $"Handle {typeof(T).FullName} to avoid boxing to {nameof(IWriteableValue)}");
                writeableValue.AppendTo(ref _builder);
                break;

            default:
                _builder.Append(value.ToString().AsMemory());
                break;
        }
    }
}
