// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperObjectBuilder<T> : IPoolableObject
    where T : TagHelperObject<T>
{
    private ImmutableArray<RazorDiagnostic>.Builder? _diagnostics;
    private bool _isBuilt;

    public ImmutableArray<RazorDiagnostic>.Builder Diagnostics
        => _diagnostics ??= ImmutableArray.CreateBuilder<RazorDiagnostic>();

    private protected TagHelperObjectBuilder()
    {
    }

    public T Build()
    {
        if (_isBuilt)
        {
            throw new InvalidOperationException();
        }

        _isBuilt = true;

        var diagnostics = ComputeDiagnostics();

        return BuildCore(diagnostics);
    }

    private protected abstract T BuildCore(ImmutableArray<RazorDiagnostic> diagnostics);

    protected ImmutableArray<RazorDiagnostic> ComputeDiagnostics()
    {
        using var collector = new PooledArrayBuilder<RazorDiagnostic>();
        CollectDiagnostics(ref collector.AsRef());

        var totalCount = collector.Count;

        if (_diagnostics is { Count: var count })
        {
            totalCount += count;
        }

        if (totalCount == 0)
        {
            return [];
        }

        using var _ = HashSetPool<Checksum>.GetPooledObject(out var checksums);

#if NET
        checksums.EnsureCapacity(totalCount);
#endif

        using var builder = new PooledArrayBuilder<RazorDiagnostic>();

        foreach (var item in collector)
        {
            if (checksums.Add(item.Checksum))
            {
                builder.Add(item);
            }
        }

        if (_diagnostics is { } diagnostics)
        {
            foreach (var item in diagnostics)
            {
                if (checksums.Add(item.Checksum))
                {
                    builder.Add(item);
                }
            }
        }

        return builder.ToImmutableAndClear();
    }

    private protected virtual void CollectDiagnostics(ref PooledArrayBuilder<RazorDiagnostic> diagnostics)
    {
    }

    private protected abstract void Reset();

    void IPoolableObject.Reset()
    {
        _isBuilt = false;

        const int MaxSize = 32;

        if (_diagnostics is { } diagnostics)
        {
            diagnostics.Clear();

            if (diagnostics.Capacity > MaxSize)
            {
                diagnostics.Capacity = MaxSize;
            }
        }

        Reset();
    }
}
