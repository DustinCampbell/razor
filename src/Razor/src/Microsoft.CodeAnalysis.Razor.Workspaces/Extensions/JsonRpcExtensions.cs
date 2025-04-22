// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace StreamJsonRpc;

internal static class JsonRpcExtensions
{
    public static RpcMethod CreateMethod(this JsonRpc jsonRpc, string methodName)
        => new(jsonRpc, methodName);

    public static RpcMethod<TParams> CreateMethod<TParams>(this JsonRpc jsonRpc, string methodName)
        where TParams : notnull
        => new(jsonRpc, methodName);

    public static Task InvokeAsync<TParams>(this JsonRpc jsonRpc, string methodName, TParams @params, CancellationToken cancellationToken)
        where TParams : notnull
        => jsonRpc.CreateMethod<TParams>(methodName).InvokeAsync(@params, cancellationToken);

    public static RpcInvocation<TParams> Invoke<TParams>(this JsonRpc jsonRpc, string methodName, TParams @params, CancellationToken cancellationToken)
        where TParams : notnull
        => jsonRpc.CreateMethod<TParams>(methodName).Invoke(@params, cancellationToken);
}

internal readonly struct RpcMethod(JsonRpc jsonRpc, string methodName)
{
    public Task InvokeAsync<TParams>(TParams @params, CancellationToken cancellationToken)
        where TParams : notnull
        => jsonRpc.InvokeWithParameterObjectAsync(methodName, @params, cancellationToken);

    public RpcInvocation<TParams> Invoke<TParams>(TParams @params, CancellationToken cancellationToken)
        where TParams : notnull
        => new(jsonRpc, methodName, @params, cancellationToken);
}

internal readonly struct RpcMethod<TParams>(JsonRpc jsonRpc, string methodName)
    where TParams : notnull
{
    public Task InvokeAsync(TParams @params, CancellationToken cancellationToken)
        => jsonRpc.InvokeWithParameterObjectAsync(methodName, @params, cancellationToken);

    public Task<TResult?> InvokeAsync<TResult>(TParams @params, CancellationToken cancellationToken)
        where TResult : class
        => Invoke(@params, cancellationToken).GetResultAsync<TResult>();

    public RpcInvocation<TParams> Invoke(TParams @params, CancellationToken cancellationToken)
        => new(jsonRpc, methodName, @params, cancellationToken);
}

internal readonly struct RpcInvocation<TParams>(
    JsonRpc jsonRpc,
    string methodName,
    TParams request,
    CancellationToken cancellationToken)
    where TParams : notnull
{
    public Task<object?> GetResultAsync()
        => jsonRpc.InvokeWithParameterObjectAsync<object?>(methodName, request, cancellationToken);

    public Task<TResult?> GetResultAsync<TResult>()
        where TResult : class
        => jsonRpc.InvokeWithParameterObjectAsync<TResult?>(methodName, request, cancellationToken);

    public Task<TResult?> GetSumResultAsync<TResult>()
        where TResult : struct, ISumType
        => jsonRpc.InvokeWithParameterObjectAsync<TResult?>(methodName, request, cancellationToken);

    public Task<SumType<T1, T2>?> GetSumResultAsync<T1, T2>()
        where T1 : notnull
        where T2 : notnull
        => GetSumResultAsync<SumType<T1, T2>>();

    public Task<SumType<T1, T2, T3>?> GetSumResultAsync<T1, T2, T3>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        => GetSumResultAsync<SumType<T1, T2, T3>>();

    public Task<SumType<T1, T2, T3, T4>?> GetSumResultAsync<T1, T2, T3, T4>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        => GetSumResultAsync<SumType<T1, T2, T3, T4>>();
}
