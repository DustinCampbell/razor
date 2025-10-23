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
        private MemoryBuilder<ReadOnlyMemory<char>> _parts;

        public ContentInterpolatedStringHandler(int literalLength, int formattedCount)
        {
            _parts = new(initialCapacity: Math.Max(formattedCount, 16));
        }

        public ReadOnlyMemory<char>[] ToParts()
        {
            var parts = _parts.AsMemory().ToArray();

            _parts.Dispose();

            return parts;
        }

        public void AppendLiteral(string value)
        {
            if (!value.IsNullOrEmpty())
            {
                _parts.Append(value);
            }
        }

        public void AppendFormatted(Content value)
        {
            if (!value.IsEmpty)
            {
                foreach (var part in value.Parts.NonEmpty)
                {
                    _parts.Append(part);
                }
            }
        }

        public void AppendFormatted(ReadOnlyMemory<char> value)
        {
            if (!value.IsEmpty)
            {
                _parts.Append(value);
            }
        }

        public void AppendFormatted(string? value)
        {
            if (!value.IsNullOrEmpty())
            {
                _parts.Append(value);
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
                    name.AppendTo(ref _parts);
                    break;

                case RenderModeVariableName name:
                    name.AppendTo(ref _parts);
                    break;

                case FormNameVariableName name:
                    name.AppendTo(ref _parts);
                    break;

                case ComponentNodeWriter.SeqName name:
                    name.AppendTo(ref _parts);
                    break;

                case ComponentNodeWriter.ParameterName name:
                    name.AppendTo(ref _parts);
                    break;

                case ComponentNodeWriter.TypeInferenceArgName name:
                    name.AppendTo(ref _parts);
                    break;

                case IWriteableValue writeableValue:
                    Debug.Assert(!typeof(T).IsValueType, $"Handle {typeof(T).FullName} to avoid boxing to {nameof(IWriteableValue)}");
                    writeableValue.AppendTo(ref _parts);
                    break;

                default:
                    _parts.Append(value.ToString() ?? string.Empty);
                    break;

            }
        }
    }
}
