// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.Razor.Telemetry;

/// <summary>
/// Manages creation and obtaining aggregated telemetry logs.
/// </summary>
internal sealed class AggregatingTelemetryLogManager(TelemetryReporter reporter)
{
    private readonly TelemetryReporter _reporter = reporter;
    private ImmutableDictionary<string, AggregatingTelemetryLog> _aggregatingLogs = ImmutableDictionary<string, AggregatingTelemetryLog>.Empty;

    public AggregatingTelemetryLog? GetLog(string name, double[]? bucketBoundaries = null)
    {
        if (!_reporter.IsEnabled)
        {
            return null;
        }

        return ImmutableInterlocked.GetOrAdd(
            ref _aggregatingLogs,
            name,
            static (functionId, arg) => new AggregatingTelemetryLog(arg._reporter, functionId, arg.bucketBoundaries),
            factoryArgument: (_reporter, bucketBoundaries));
    }

    public void Flush()
    {
        if (!_reporter.IsEnabled)
            return;

        foreach (var log in _aggregatingLogs.Values)
        {
            log.Flush();
        }
    }
}
