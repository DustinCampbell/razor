// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Telemetry.Metrics;
using Microsoft.VisualStudio.Telemetry.Metrics.Events;

namespace Microsoft.VisualStudio.Razor.Telemetry;

internal class TelemetryInstrumentEvent(TelemetryEvent telemetryEvent, IInstrument instrument)
    : TelemetryMetricEvent(telemetryEvent, instrument)
{
    public TelemetryEvent Event { get; } = telemetryEvent;
    public IInstrument Instrument { get; } = instrument;
}
