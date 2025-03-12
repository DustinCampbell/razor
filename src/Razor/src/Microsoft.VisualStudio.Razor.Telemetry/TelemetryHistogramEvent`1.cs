// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Telemetry.Metrics;

namespace Microsoft.VisualStudio.Razor.Telemetry;

internal class TelemetryHistogramEvent<T>(TelemetryEvent telemetryEvent, IHistogram<T> histogram)
    : TelemetryInstrumentEvent(telemetryEvent, histogram)
    where T : struct
{
}
