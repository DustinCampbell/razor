// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Threading;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Telemetry;

[NonCopyable]
internal struct TelemetryScope : IDisposable
{
    private ITelemetryReporter? _reporter;
    private readonly string _name;
    private readonly Severity _severity;
    private readonly Property[] _properties;
    private readonly int _propertyCount;
    private readonly Stopwatch _stopwatch;
    private readonly TimeSpan _minTimeToReport;

    private readonly Span<Property> GetPropertiesSpan()
        => _properties.AsSpan()[.._propertyCount];

    public TelemetryScope(
        ITelemetryReporter reporter,
        string name,
        Severity severity,
        TimeSpan minTimeToReport,
        scoped ReadOnlySpan<Property> properties)
    {
        Debug.Assert(reporter is not null);

        _reporter = reporter;
        _name = name;
        _severity = severity;

        // Note: We create an array that is 1 larger than the number of properties
        // to allow the final "ellapsedms" property to be added in Dispose below.
        _propertyCount = properties.Length + 1;
        _properties = ArrayPool<Property>.Shared.Rent(_propertyCount);
        properties.CopyTo(_properties);

        _minTimeToReport = minTimeToReport;

        _stopwatch = StopwatchPool.Default.Get();
        _stopwatch.Restart();
    }

    public void Dispose()
    {
        var reporter = Interlocked.Exchange(ref _reporter, null);
        if (reporter is null)
        {
            return;
        }

        _stopwatch.Stop();

        var elapsed = _stopwatch.Elapsed;
        if (elapsed >= _minTimeToReport)
        {
            // We always have at least one slot for the elapsed milliseconds.
            var properties = GetPropertiesSpan();
            properties[^1] = new("eventscope.ellapsedms", elapsed.Milliseconds);

            reporter.ReportEvent(_name, _severity, properties);
        }

        // Clear array since it might contain PII.
        ArrayPool<Property>.Shared.Return(_properties, clearArray: true);

        StopwatchPool.Default.Return(_stopwatch);
    }
}
