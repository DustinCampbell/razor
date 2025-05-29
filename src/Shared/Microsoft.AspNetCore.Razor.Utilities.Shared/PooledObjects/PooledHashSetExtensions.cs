// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal static class PooledHashSetExtensions
{
    /// <summary>
    /// Gets a mutable reference to a <see cref="PooledHashSet{T}"/> stored in a <c>using</c> variable.
    /// </summary>
    /// <remarks>
    /// <para>This supporting method allows <see cref="PooledHashSet{T}"/>, a non-copyable <see langword="struct"/>
    /// implementing <see cref="IDisposable"/>, to be used with <c>using</c> statements while still allowing them to
    /// be passed by reference in calls. The following two calls are equivalent:</para>
    ///
    /// <code>
    /// using var set = PooledHashSet&lt;T&gt;.Empty;
    ///
    /// // Using the 'Unsafe.AsRef' method
    /// Method(ref Unsafe.AsRef(in set));
    ///
    /// // Using this helper method
    /// Method(ref set.AsRef());
    /// </code>
    ///
    /// <para>⚠ Do not move or rename this method without updating the corresponding
    /// Razor.Diagnostics.Analyzers\PooledHashSetAsRefAnalyzer.cs.</para>
    /// </remarks>
    /// <typeparam name="T">The type of element stored in the pooled array builder.</typeparam>
    /// <param name="set">A read-only reference to a pooled hash set which is part of a <c>using</c> statement.</param>
    /// <returns>A mutable reference to the pooled array builder.</returns>
    public static ref PooledHashSet<T> AsRef<T>(this in PooledHashSet<T> set)
#pragma warning disable RS0042
        => ref Unsafe.AsRef(in set);
#pragma warning restore RS0042
}
