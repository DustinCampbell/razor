// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Telemetry;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.Razor.Telemetry;

internal static class TelemetryUtils
{
    public static string GetEventName(string name)
        => "dotnet/razor/" + name;

    public static string GetPropertyName(string name)
        => "dotnet.razor." + name;

    public static TelemetryEvent CreateTelemetryEvent(string name, params ReadOnlySpan<Property> properties)
        => CreateTelemetryEvent(name, Severity.Normal, properties);

    public static TelemetryEvent CreateTelemetryEvent(string name, Severity severity, params ReadOnlySpan<Property> properties)
    {
        var telemetryEvent = new TelemetryEvent(GetEventName(name), ConvertSeverity(severity));

        foreach (var property in properties)
        {
            telemetryEvent.Properties.Add(GetPropertyName(property.Name), GetPropertyValue(property.Value));
        }

        return telemetryEvent;
    }

    private static object? GetPropertyValue(object? value)
        => IsComplexValue(value)
            ? new TelemetryComplexProperty(value)
            : value;

    private static bool IsComplexValue(object? o)
        => o?.GetType() is Type type && Type.GetTypeCode(type) == TypeCode.Object;

    private static TelemetrySeverity ConvertSeverity(Severity severity)
        => severity switch
        {
            Severity.Normal => TelemetrySeverity.Normal,
            Severity.Low => TelemetrySeverity.Low,
            Severity.High => TelemetrySeverity.High,
            _ => Assumed.Unreachable<TelemetrySeverity>($"Unknown severity: {severity}")
        };
}
