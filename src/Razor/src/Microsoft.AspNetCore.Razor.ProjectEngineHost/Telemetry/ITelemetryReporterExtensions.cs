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
}
