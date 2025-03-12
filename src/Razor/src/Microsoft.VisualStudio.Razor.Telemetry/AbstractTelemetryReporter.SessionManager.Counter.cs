// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Threading;
using TelemetryResult = Microsoft.AspNetCore.Razor.Telemetry.TelemetryResult;

namespace Microsoft.VisualStudio.Razor.Telemetry;

internal abstract partial class AbstractTelemetryReporter
{
    private sealed partial class SessionManager
    {
        private sealed class Counter
        {
            private int _succeededCount;
            private int _failedCount;
            private int _cancelledCount;

            public int SucceededCount => _succeededCount;
            public int FailedCount => _failedCount;
            public int CancelledCount => _cancelledCount;

            public void IncrementCount(TelemetryResult result)
            {
                switch (result)
                {
                    case TelemetryResult.Succeeded:
                        Interlocked.Increment(ref _succeededCount);
                        break;
                    case TelemetryResult.Failed:
                        Interlocked.Increment(ref _failedCount);
                        break;
                    case TelemetryResult.Cancelled:
                        Interlocked.Increment(ref _cancelledCount);
                        break;
                }
            }
        }
    }
}
