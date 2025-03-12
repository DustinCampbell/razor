// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
#if !NET
using System.Collections.Generic;
#endif
using Microsoft.AspNetCore.Razor.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using TelemetryResult = Microsoft.AspNetCore.Razor.Telemetry.TelemetryResult;

namespace Microsoft.VisualStudio.Razor.Telemetry;

internal abstract partial class AbstractTelemetryReporter
{
    private sealed partial class SessionManager(AbstractTelemetryReporter reporter, TelemetrySession session) : IDisposable
    {
        /// <summary>
        /// Store request counters in a concurrent dictionary as non-mutating LSP requests can
        /// run alongside other non-mutating requests.
        /// </summary>
        private readonly ConcurrentDictionary<(string Method, string? Language), Counter> _requestCounters = new();
        private readonly AbstractTelemetryReporter _reporter = reporter;
        private readonly AggregatingTelemetryLogManager _aggregatingManager = new(reporter);

        public void Dispose()
        {
            Flush();
            Session.Dispose();
        }

        public TelemetrySession Session { get; } = session;

        private void Flush()
        {
            _aggregatingManager.Flush();
            LogRequestCounters();
        }

        public void LogRequestTelemetry(string name, string? language, TimeSpan queuedDuration, TimeSpan requestDuration, TelemetryResult result)
        {
            LogAggregated("LSP_TimeInQueue",
                "TimeInQueue",  // All time in queue events use the same histogram, no need for separate keys
                (int)queuedDuration.TotalMilliseconds,
                name);

            LogAggregated("LSP_RequestDuration",
                name, // RequestDuration requests are histogrammed by their unique name
                (int)requestDuration.TotalMilliseconds,
                name);

            _requestCounters.GetOrAdd((name, language), _ => new Counter()).IncrementCount(result);
        }

        private void LogRequestCounters()
        {
            foreach (var (key, value) in _requestCounters)
            {
                _reporter.ReportEvent("LSP_RequestCounter",
                    Severity.Low,
                    new("method", key.Method),
                    new("successful", value.SucceededCount),
                    new("failed", value.FailedCount),
                    new("cancelled", value.CancelledCount));
            }

            _requestCounters.Clear();
        }

        private void LogAggregated(string managerKey, string histogramKey, int value, string method)
        {
            var aggregatingLog = _aggregatingManager.GetLog(managerKey);
            aggregatingLog?.Log(histogramKey, value, method);
        }
    }
}
