// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal static class PooledArrayBuilderExtensions
{
    /// <summary>
    /// Gets a mutable reference to a <see cref="PooledArrayBuilder{T}"/> stored in a <c>using</c> variable.
    /// </summary>
    /// <remarks>
    /// <para>This supporting method allows <see cref="PooledArrayBuilder{T}"/>, a non-copyable <see langword="struct"/>
    /// implementing <see cref="IDisposable"/>, to be used with <c>using</c> statements while still allowing them to
    /// be passed by reference in calls. The following two calls are equivalent:</para>
    ///
    /// <code>
    /// using var array = PooledArrayBuilder&lt;T&gt;.Empty;
    ///
    /// // Using the 'Unsafe.AsRef' method
    /// Method(ref Unsafe.AsRef(in builder));
    ///
    /// // Using this helper method
    /// Method(ref builder.AsRef());
    /// </code>
    ///
    /// <para>⚠ Do not move or rename this method without updating the corresponding
    /// Razor.Diagnostics.Analyzers\PooledArrayBuilderAsRefAnalyzer.cs.</para>
    /// </remarks>
    /// <typeparam name="T">The type of element stored in the pooled array builder.</typeparam>
    /// <param name="builder">A read-only reference to a pooled array builder which is part of a <c>using</c> statement.</param>
    /// <returns>A mutable reference to the pooled array builder.</returns>
    public static ref PooledArrayBuilder<T> AsRef<T>(this in PooledArrayBuilder<T> builder)
#pragma warning disable RS0042
        => ref Unsafe.AsRef(in builder);
#pragma warning restore RS0042

    public static string CreateString(this ref readonly PooledArrayBuilder<ReadOnlyMemory<char>> builder)
    {
        if (builder.Count == 0)
        {
            return string.Empty;
        }

        if (builder.Count == 1)
        {
            return builder[0].ToString();
        }

        var length = 0;

        foreach (var item in builder)
        {
            length += item.Length;
        }

        if (length == 0)
        {
            return string.Empty;
        }

        using var _ = ArrayPool<ReadOnlyMemory<char>>.Shared.GetPooledArray(length, out var array);
        builder.CopyTo(array);

        return string.Create(length, (array, length), static (span, state) =>
        {
            var source = state.array.AsSpan(0, state.length);

            foreach (var item in source)
            {
                item.Span.CopyTo(span);
                span = span[item.Length..];
            }

            Debug.Assert(span.IsEmpty, "Not all characters were written to the span.");
        });
    }
}
