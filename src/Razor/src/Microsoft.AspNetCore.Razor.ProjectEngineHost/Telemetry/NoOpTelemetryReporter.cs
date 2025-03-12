// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Telemetry;

internal sealed class NoOpTelemetryReporter : ITelemetryReporter
{
    public static readonly NoOpTelemetryReporter Instance = new();

    private NoOpTelemetryReporter()
    {
    }

    public void ReportEvent(string name, Severity severity, params ReadOnlySpan<Property> properties)
    {
    }

    public void ReportFault(Exception exception, string? message, params ReadOnlySpan<object?> args)
    {
    }

    public void ReportRequestTiming(string name, string? language, TimeSpan queuedDuration, TimeSpan requestDuration, TelemetryResult result)
    {
    }
}
