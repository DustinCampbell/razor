// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Telemetry;

internal static class ITelemetryReporterExtensions
{
    // These extensions effectively make TimeSpan an optional parameter on BeginBlock
    public static TelemetryScope BeginBlock(
        this ITelemetryReporter reporter,
        string name,
        Severity severity,
        params ReadOnlySpan<Property> properties)
        => reporter.BeginBlock(name, severity, minTimeToReport: TimeSpan.Zero, properties);
}
