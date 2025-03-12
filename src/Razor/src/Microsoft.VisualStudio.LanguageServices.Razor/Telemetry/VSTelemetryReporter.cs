// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.AspNetCore.Razor.Telemetry;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.VisualStudio.Telemetry;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.Razor.Telemetry;

[Export(typeof(ITelemetryReporter))]
[method: ImportingConstructor]
internal class VSTelemetryReporter(ILoggerFactory loggerFactory) : AbstractTelemetryReporter(TelemetryService.DefaultSession)
{
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<VSTelemetryReporter>();

    protected override bool HandleException(Exception exception, string? message, params ReadOnlySpan<object?> @params)
    {
        return exception is RemoteInvocationException remoteInvocationException &&
               ReportRemoteInvocationException(remoteInvocationException, @params);
    }

    private bool ReportRemoteInvocationException(RemoteInvocationException remoteInvocationException, ReadOnlySpan<object?> @params)
    {
        if (remoteInvocationException.InnerException is Exception innerException)
        {
            // innerException might be an OperationCancelled or Aggregate, use the full ReportFault to unwrap it consistently.
            ReportFault(innerException, "RIE: " + remoteInvocationException.Message);
            return true;
        }

        if (@params.Length < 2)
        {
            // RIE has '2' extra pieces of data to report via @params, if we don't have those, then we unwrap and call one more time.
            // If we have both, though, we want the core code of ReportFault to do the reporting.
            ReportFault(
                remoteInvocationException,
                remoteInvocationException.Message,
                remoteInvocationException.ErrorCode,
                remoteInvocationException.DeserializedErrorData);
            return true;
        }

        return false;
    }

    protected override void LogTrace(string message)
        => _logger.Log(LogLevel.Trace, message, exception: null);

    protected override void LogError(Exception exception, string message)
        => _logger.Log(LogLevel.Error, message, exception);
}
