// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Language;

public readonly partial struct Content
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [InterpolatedStringHandler]
    public ref struct ContentInterpolatedStringHandler
    {
        private Builder _builder;

        public ContentInterpolatedStringHandler(int literalLength, int formattedCount)
        {
            _builder = new(initialCapacity: Math.Max(formattedCount, 16));
        }

        public Content ToContent()
        {
            var result = _builder.ToContent();

            _builder.Dispose();

            return result;
        }

        public void AppendLiteral(string value)
        {
            if (!value.IsNullOrEmpty())
            {
                _builder.Add(value);
            }
        }

        public void AppendFormatted(Content value)
        {
            if (!value.IsEmpty)
            {
                foreach (var part in value.Parts.NonEmpty)
                {
                    _builder.Add(part);
                }
            }
        }

        public void AppendFormatted(ReadOnlyMemory<char> value)
        {
            if (!value.IsEmpty)
            {
                _builder.Add(value);
            }
        }

        public void AppendFormatted(string? value)
        {
            if (!value.IsNullOrEmpty())
            {
                _builder.Add(value);
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
                case ImmutableArray<Content> array:
                    if (!array.IsDefaultOrEmpty)
                    {
                        foreach (var content in array)
                        {
                            AppendFormatted(content);
                        }
                    }

                    break;

                case ImmutableArray<ReadOnlyMemory<char>> array:
                    if (!array.IsDefaultOrEmpty)
                    {
                        foreach (var part in array)
                        {
                            AppendFormatted(part);
                        }
                    }

                    break;

                case ImmutableArray<string> array:
                    if (!array.IsDefaultOrEmpty)
                    {
                        foreach (var part in array)
                        {
                            AppendFormatted(part);
                        }
                    }

                    break;

                case BuilderVariableName name:
                    _builder.Add(name);
                    break;

                case RenderModeVariableName name:
                    _builder.Add(name);
                    break;

                case FormNameVariableName name:
                    _builder.Add(name);
                    break;

                case ComponentNodeWriter.SeqName name:
                    _builder.Add(name);
                    break;

                case ComponentNodeWriter.ParameterName name:
                    _builder.Add(name);
                    break;

                case ComponentNodeWriter.TypeInferenceArgName name:
                    _builder.Add(name);
                    break;

                case IWriteableValue writeableValue:
                    Debug.Assert(!typeof(T).IsValueType, $"Handle {typeof(T).FullName} to avoid boxing to {nameof(IWriteableValue)}");
                    _builder.Add(writeableValue);
                    break;

                default:
                    _builder.Add(value.ToString() ?? string.Empty);
                    break;

            }
        }
    }
}
