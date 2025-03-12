// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Telemetry;

internal static class ITelemetryReporterExtensions
{
    // This extension effectively make TimeSpan an optional parameter on CreateScope
    public static TelemetryScope CreateScope(
        this ITelemetryReporter reporter,
        string name,
        Severity severity,
        params ReadOnlySpan<Property> properties)
        => reporter.CreateScope(name, severity, minTimeToReport: TimeSpan.Zero, properties);

    public static TelemetryScope CreateScope(
        this ITelemetryReporter reporter,
        string name,
        Severity severity,
        TimeSpan minTimeToReport,
        params ReadOnlySpan<Property> properties)
        => new(reporter, name, severity, minTimeToReport, properties);

    public static TelemetryScope TrackLspRequest(
        this ITelemetryReporter reporter,
        string lspMethodName,
        string lspServerName,
        TimeSpan minTimeToReport,
        Guid correlationId)
    {
        if (correlationId == Guid.Empty)
        {
            return default;
        }

        return reporter.CreateScope("TrackLspRequest",
            Severity.Normal,
            minTimeToReport,
            new("eventscope.method", lspMethodName),
            new("eventscope.languageservername", lspServerName),
            new("eventscope.correlationid", correlationId));

    }
}
