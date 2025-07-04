// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

/// <summary>
///  This is used to represent content in Razor code generation. It can be a single string,
///  a <see cref="ReadOnlyMemory{T}"/>, or an array of <see cref="Content"/> parts.
/// </summary>
/// <remarks>
///  This can be used to avoid allocations caused by string concatenation.
/// </remarks>
public readonly partial record struct Content : IEnumerable<char>
{
    public static Content Empty => default;

    // If _parts is null, then _value is the only content.
    private readonly ReadOnlyMemory<char> _value;

    // Can be a Content[], a ReadOnlyMemory<char>[], or a string[].
    // If this is set, _value is empty.
    private readonly Array? _parts;

    public bool IsEmpty
        => _value.IsEmpty && _parts is null;

    [MemberNotNullWhen(true, nameof(_parts))]
    public bool IsComposite
        => _parts is not null;

    public Content(Content value)
    {
        _value = value._value;
        _parts = value._parts;
    }

    public Content(ReadOnlyMemory<char> value)
    {
        _value = value;
        _parts = null;
    }

    public Content(string value)
    {
        _value = value.AsMemory();
        _parts = null;
    }

    public Content(ImmutableArray<Content> parts)
        : this(ImmutableCollectionsMarshal.AsArray(parts).AssumeNotNull())
    {
    }

    private Content(Content[] parts)
    {
        Debug.Assert(parts is not null);

        (_value, _parts) = parts switch
        {
            [] => (ReadOnlyMemory<char>.Empty, null),
            [var part] => (part._value, part._parts),
            _ => (ReadOnlyMemory<char>.Empty, parts)
        };
    }

    public Content(ImmutableArray<ReadOnlyMemory<char>> parts)
    {
        var array = ImmutableCollectionsMarshal.AsArray(parts);

        Debug.Assert(array is not null);

        (_value, _parts) = array switch
        {
            [] => (ReadOnlyMemory<char>.Empty, null),
            [var part] => (part, null),
            _ => (ReadOnlyMemory<char>.Empty, array)
        };
    }

    public Content(ImmutableArray<string> parts)
    {
        var array = ImmutableCollectionsMarshal.AsArray(parts);

        Debug.Assert(array is not null);

        (_value, _parts) = array switch
        {
            [] => (ReadOnlyMemory<char>.Empty, null),
            [var part] => (part.AsMemory(), null),
            _ => (ReadOnlyMemory<char>.Empty, array)
        };
    }

    public Content(ref ContentInterpolatedStringHandler handler)
        : this(handler.ToArray())
    {
    }

    public static bool IsNullOrEmpty(Content? content)
        => content is not Content value || value.IsEmpty;

    public static bool IsNullOrWhiteSpace(Content? content)
        => content is not Content value || value.IsEmptyOrWhiteSpace();

    public bool IsEmptyOrWhiteSpace()
    {
        if (IsEmpty)
        {
            return true;
        }

        switch (_parts)
        {
            case null:
                return _value.Span.IsWhiteSpace();

            case Content[] contentParts:
                foreach (var part in contentParts)
                {
                    if (!part.IsEmptyOrWhiteSpace())
                    {
                        return false;
                    }
                }

                break;

            case ReadOnlyMemory<char>[] memoryParts:
                foreach (var part in memoryParts)
                {
                    if (!part.Span.IsWhiteSpace())
                    {
                        return false;
                    }
                }

                break;

            case string[] stringParts:
                foreach (var part in stringParts)
                {
                    if (!part.AsMemory().Span.IsWhiteSpace())
                    {
                        return false;
                    }
                }

                break;

            default:
                Assumed.Unreachable();
                break;
        }

        return true;
    }

    public void WriteTo(CodeWriter writer)
    {
        switch (_parts)
        {
            // If _parts is null, then _value is the only content.
            case null:
                if (!_value.IsEmpty)
                {
                    writer.Write(_value);
                }

                break;

            case Content[] contentParts:
                foreach (var part in contentParts)
                {
                    part.WriteTo(writer);
                }

                break;

            case ReadOnlyMemory<char>[] memoryParts:
                foreach (var part in memoryParts)
                {
                    writer.Write(part);
                }

                break;

            case string[] stringParts:
                foreach (var part in stringParts)
                {
                    writer.Write(part);
                }

                break;

            default:
                Assumed.Unreachable();
                break;
        }
    }

    public override string ToString()
    {
        switch (_parts)
        {
            case null:
                return _value.ToString();

            case ReadOnlyMemory<char>[] memoryParts:
                {
                    var length = 0;

                    foreach (var part in memoryParts)
                    {
                        length += part.Length;
                    }

                    return string.Create(length, memoryParts, static (span, memoryParts) =>
                    {
                        foreach (var part in memoryParts)
                        {
                            if (part.IsEmpty)
                            {
                                continue;
                            }

                            part.Span.CopyTo(span);
                            span = span[part.Length..];
                        }

                        Debug.Assert(span.IsEmpty, "Not all characters were written to the span.");
                    });
                }

            case string[] stringParts:
                {
                    var length = 0;

                    foreach (var part in stringParts)
                    {
                        length += part.Length;
                    }

                    return string.Create(length, stringParts, static (span, stringParts) =>
                    {
                        foreach (var part in stringParts)
                        {
                            if (part is null)
                            {
                                continue;
                            }

                            part.AsSpan().CopyTo(span);
                            span = span[part.Length..];
                        }

                        Debug.Assert(span.IsEmpty, "Not all characters were written to the span.");
                    });
                }

            case Content[]:
                {
                    using var _ = ArrayBuilderPool<ReadOnlyMemory<char>>.GetPooledObject(out var parts);
                    CollectAllParts(parts);

                    return parts.Join();
                }

            default:
                return Assumed.Unreachable<string>();
        }
    }

    public AllContentParts AllParts
        => new(this);

    public Enumerator GetEnumerator()
        => new(this);

    IEnumerator<char> IEnumerable<char>.GetEnumerator()
        => new EnumeratorImpl(this);

    IEnumerator IEnumerable.GetEnumerator()
        => new EnumeratorImpl(this);

    internal void CollectAllParts(ImmutableArray<ReadOnlyMemory<char>>.Builder collecter)
    {
        switch (_parts)
        {
            case null:
                if (!_value.IsEmpty)
                {
                    collecter.Add(_value);
                }

                break;

            case ReadOnlyMemory<char>[] memoryParts:
                collecter.AddRange(memoryParts);
                break;

            case string[] stringParts:
                foreach (var part in stringParts)
                {
                    collecter.Add(part.AsMemory());
                }

                break;

            case Content[] contentParts:
                foreach (var part in contentParts)
                {
                    part.CollectAllParts(collecter);
                }

                break;

            default:
                Assumed.Unreachable();
                break;
        }
    }

    internal void CollectAllParts(ref PooledArrayBuilder<ReadOnlyMemory<char>> collecter)
    {
        switch (_parts)
        {
            case null:
                if (!_value.IsEmpty)
                {
                    collecter.Add(_value);
                }

                break;

            case ReadOnlyMemory<char>[] memoryParts:
                collecter.AddRange(memoryParts);
                break;

            case string[] stringParts:
                foreach (var part in stringParts)
                {
                    collecter.Add(part.AsMemory());
                }

                break;

            case Content[] contentParts:
                foreach (var part in contentParts)
                {
                    part.CollectAllParts(ref collecter);
                }

                break;

            default:
                Assumed.Unreachable();
                break;
        }
    }

    public bool Equals(Content other)
    {
        return (_parts, other._parts) switch
        {
            (null, null) => ValueComparer.Instance.Equals(_value, other._value),

            (Content[] parts, Content[] otherParts)
                => parts.SequenceEqual(otherParts),

            (ReadOnlyMemory<char>[] parts, ReadOnlyMemory<char>[] otherParts)
                => parts.SequenceEqual(otherParts, ValueComparer.Instance),

            (string[] parts, string[] otherParts)
                => parts.SequenceEqual(otherParts),

            _ => false,
        };
    }

    public override int GetHashCode()
        => ValueComparer.Instance.GetHashCode(_value);

    public static implicit operator Content(ReadOnlyMemory<char> value)
        => new(value);

    public static implicit operator Content(string value)
        => new(value);

    public static implicit operator Content(ImmutableArray<Content> parts)
        => new(parts);

    public static implicit operator Content(ImmutableArray<ReadOnlyMemory<char>> parts)
        => new(parts);

    public static implicit operator Content(ImmutableArray<string> parts)
        => new(parts);
}
