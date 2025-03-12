// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Telemetry;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Hosting;

/// <summary>
/// Providers a bridge from CLaSP, which uses ILspLogger, to our logging infrastructure, which uses ILogger
/// </summary>
internal sealed class ClaspLoggingBridge(ILoggerFactory loggerFactory, ITelemetryReporter telemetryReporter) : ILspLogger
{
    public const string LogStartContextMarker = "[StartContext]";
    public const string LogEndContextMarker = "[EndContext]";

    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger("CLaSP");
    private readonly ITelemetryReporter _telemetryReporter = telemetryReporter;

    public void LogStartContext(string message, params object[] @params)
    {
        // This is a special log message formatted so that the LogHub logger can detect it, and trigger the right trace event
        _logger.LogInformation($"{LogStartContextMarker} {message}");
    }

    public void LogEndContext(string message, params object[] @params)
    {
        // This is a special log message formatted so that the LogHub logger can detect it, and trigger the right trace event
        _logger.LogInformation($"{LogEndContextMarker} {message}");
    }

    public void LogError(string message, params object[] @params)
    {
        _logger.LogError($"{message}: {string.Join(",", @params)}");

        var properties = new Property[@params.Length + 1];

        for (var i = 0; i < @params.Length; i++)
        {
            properties[i] = new("param" + i, @params[i]);
        }

        properties[^1] = new("message", message);

        _telemetryReporter.ReportEvent("lsperror", Severity.High, properties);
    }

    public void LogException(Exception exception, string? message = null, params object[] @params)
    {
        _logger.LogError(exception, $"{message}: {string.Join(",", @params)}");

        _telemetryReporter?.ReportFault(exception, message, @params);
    }

    public void LogInformation(string message, params object[] @params)
    {
        _logger.LogInformation($"{message}: {string.Join(",", @params)}");
    }

    public void LogDebug(string message, params object[] @params)
    {
        _logger.LogDebug($"{message}: {string.Join(",", @params)}");
    }

    public void LogWarning(string message, params object[] @params)
    {
        _logger.LogWarning($"{message}: {string.Join(",", @params)}");
    }
}
