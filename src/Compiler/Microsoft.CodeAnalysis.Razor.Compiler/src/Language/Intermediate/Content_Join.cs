// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public readonly partial record struct Content
{
    public static Content Join(Content separator, ImmutableArray<Content> values)
        => Join(separator, values.AsSpan());

    public static Content Join(Content separator, params ReadOnlySpan<Content> values)
    {
        var nonEmptyValues = 0;

        foreach (var value in values)
        {
            if (!value.IsEmpty)
            {
                nonEmptyValues++;
            }
        }

        if (nonEmptyValues == 0)
        {
            return Empty;
        }

        var capacity = !separator.IsEmpty ? (nonEmptyValues * 2) - 1 : nonEmptyValues;
        var array = new Content[capacity];

        for (int i = 0, j = 0; i < values.Length; i++)
        {
            var value = values[i];
            if (value.IsEmpty)
            {
                continue;
            }

            if (j > 0 && !separator.IsEmpty)
            {
                array[j++] = separator;
            }

            array[j++] = value;
        }

        return new(array);
    }

    public static Content Join(Content separator, ImmutableArray<ReadOnlyMemory<char>> values)
        => Join(separator, values.AsSpan());

    public static Content Join(Content separator, params ReadOnlySpan<ReadOnlyMemory<char>> values)
    {
        var nonEmptyValues = 0;

        foreach (var value in values)
        {
            if (!value.IsEmpty)
            {
                nonEmptyValues++;
            }
        }

        if (nonEmptyValues == 0)
        {
            return Empty;
        }

        var capacity = !separator.IsEmpty ? (nonEmptyValues * 2) - 1 : nonEmptyValues;
        var array = new Content[capacity];

        for (int i = 0, j = 0; i < values.Length; i++)
        {
            Content value = values[i];

            if (value.IsEmpty)
            {
                continue;
            }

            if (j > 0 && !separator.IsEmpty)
            {
                array[j++] = separator;
            }

            array[j++] = value;
        }

        return new(array);
    }

    public static Content Join(Content separator, ImmutableArray<string> values)
        => Join(separator, values.AsSpan());

    public static Content Join(Content separator, params ReadOnlySpan<string> values)
    {
        var nonEmptyValues = 0;

        foreach (var value in values)
        {
            if (!value.IsNullOrEmpty())
            {
                nonEmptyValues++;
            }
        }

        if (nonEmptyValues == 0)
        {
            return Empty;
        }

        var capacity = !separator.IsEmpty ? (nonEmptyValues * 2) - 1 : nonEmptyValues;
        var array = new Content[capacity];

        for (int i = 0, j = 0; i < values.Length; i++)
        {
            Content value = values[i];

            if (value.IsEmpty)
            {
                continue;
            }

            if (j > 0 && !separator.IsEmpty)
            {
                array[j++] = separator;
            }

            array[j++] = value;
        }

        return new(array);
    }

    public static Content Join(Content separator, IEnumerable<Content> values)
    {
        using var enumerator = values.GetEnumerator();

        if (!enumerator.MoveNext())
        {
            return Empty;
        }

        var first = enumerator.Current;
        if (!enumerator.MoveNext())
        {
            return first;
        }

        using var builder = new PooledArrayBuilder<Content>();
        builder.Add(first);

        do
        {
            var current = enumerator.Current;

            if (current.IsEmpty)
            {
                continue;
            }

            if (!separator.IsEmpty)
            {
                builder.Add(separator);
            }

            builder.Add(current);
        }
        while (enumerator.MoveNext());

        return builder.ToContent();
    }

    public static Content Join(Content separator, IEnumerable<ReadOnlyMemory<char>> values)
    {
        using var enumerator = values.GetEnumerator();

        if (!enumerator.MoveNext())
        {
            return Empty;
        }

        var first = enumerator.Current;
        if (!enumerator.MoveNext())
        {
            return first;
        }

        using var builder = new PooledArrayBuilder<Content>();
        builder.Add(first);

        do
        {
            Content current = enumerator.Current;
            if (current.IsEmpty)
            {
                continue;
            }

            if (!separator.IsEmpty)
            {
                builder.Add(separator);
            }

            builder.Add(current);
        }
        while (enumerator.MoveNext());

        return builder.ToContent();
    }

    public static Content Join(Content separator, IEnumerable<string> values)
    {
        using var enumerator = values.GetEnumerator();

        if (!enumerator.MoveNext())
        {
            return Empty;
        }

        var first = enumerator.Current;
        if (!enumerator.MoveNext())
        {
            return first;
        }

        using var builder = new PooledArrayBuilder<Content>();
        builder.Add(first);

        do
        {
            Content current = enumerator.Current;
            if (current.IsEmpty)
            {
                continue;
            }

            if (!separator.IsEmpty)
            {
                builder.Add(separator);
            }

            builder.Add(current);
        }
        while (enumerator.MoveNext());

        return builder.ToContent();
    }
}
