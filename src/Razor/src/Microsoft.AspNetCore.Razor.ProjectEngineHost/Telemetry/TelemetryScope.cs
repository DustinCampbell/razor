// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Telemetry;

internal sealed class TelemetryScope : IDisposable
{
    public static readonly TelemetryScope Null = new();

    private readonly ITelemetryReporter? _reporter;
    private readonly string _name;
    private readonly Severity _severity;
    private readonly Property[] _properties;
    private readonly Stopwatch _stopwatch;
    private readonly TimeSpan _minTimeToReport;
    private bool _disposed;

    private TelemetryScope()
    {
        // This constructor is only called to initialize the Null instance
        // above. Rather than make _name, _properties, and _stopwatch
        // nullable, we use a ! to initialize them to null for this case only.
        _reporter = null;
        _name = null!;
        _properties = null!;
        _stopwatch = null!;
    }

    private TelemetryScope(
        ITelemetryReporter reporter,
        string name,
        TimeSpan minTimeToReport,
        Severity severity,
        Property[] properties)
    {
        _reporter = reporter;
        _name = name;
        _severity = severity;

        // Note: The builder that is passed in always has its capacity set to at least
        // 1 larger than the number of properties to allow the final "ellapsedms"
        // property to be added in Dispose below.
        _properties = properties;

        _stopwatch = StopwatchPool.Default.Get();
        _minTimeToReport = minTimeToReport;
        _stopwatch.Restart();
    }

    public void Dispose()
    {
        if (_reporter is null || _disposed)
        {
            return;
        }

        _disposed = true;

        _stopwatch.Stop();

        var elapsed = _stopwatch.Elapsed;
        if (elapsed >= _minTimeToReport)
        {
            // We know that we were created with an array of at least length one.
            _properties[^1] = new("eventscope.ellapsedms", _stopwatch.ElapsedMilliseconds);

            _reporter.ReportEvent(_name, _severity, _properties);
        }

        StopwatchPool.Default.Return(_stopwatch);
    }

    public static TelemetryScope Create(
        ITelemetryReporter reporter,
        string name,
        Severity severity,
        TimeSpan minTimeToReport,
        ReadOnlySpan<Property> properties)
    {
        var array = new Property[properties.Length + 1];
        properties.CopyTo(array);

        return new(reporter, name, minTimeToReport, severity, array);
    }
}
