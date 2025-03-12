// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Telemetry;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.AspNetCore.Razor.LanguageServer;

internal sealed class CLaSPTelemetryService(ITelemetryReporter telemetryReporter) : AbstractTelemetryService
{
    public override AbstractRequestScope CreateRequestScope(string lspMethodName)
        => new RequestTelemetryScope(lspMethodName, telemetryReporter);

    private sealed class RequestTelemetryScope(string lspMethodName, ITelemetryReporter telemetryReporter)
        : AbstractRequestScope(lspMethodName)
    {
        private readonly Stopwatch _stopWatch = Stopwatch.StartNew();
        private readonly ITelemetryReporter _telemetryReporter = telemetryReporter;
        private TelemetryResult _result = TelemetryResult.Succeeded;
        private TimeSpan _queuedDuration;

        public override void Dispose()
        {
            var requestDuration = _stopWatch.Elapsed;
            _telemetryReporter.ReportRequestTiming(Name, Language, _queuedDuration, requestDuration, _result);
        }

        public override void RecordCancellation()
        {
            _result = TelemetryResult.Cancelled;
        }

        public override void RecordException(Exception exception)
        {
            _result = TelemetryResult.Failed;
        }

        public override void RecordExecutionStart()
        {
            _queuedDuration = _stopWatch.Elapsed;
        }

        public override void RecordWarning(string message)
        {
            _result = TelemetryResult.Failed;
        }
    }
}
