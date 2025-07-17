// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

    /// <summary>
    /// Computes the sum of the sequence of <see cref="int"/> values that are obtained
    /// by invoking a transform function on each element of this builder.
    /// </summary>
    /// <typeparam name="T">The type of element stored in the pooled array builder.</typeparam>
    /// <param name="builder">A pooled array builder whose elements to sum.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>The sum of the projected values.</returns>
    public static int Sum<T>(this ref readonly PooledArrayBuilder<T> builder, Func<T, int> selector)
    {
        var result = 0;

        foreach (var item in builder)
        {
            result += selector(item);
        }

        return result;
    }
}
