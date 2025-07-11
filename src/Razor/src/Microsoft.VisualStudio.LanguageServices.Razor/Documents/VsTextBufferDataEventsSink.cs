﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.Razor.Documents;

internal class VsTextBufferDataEventsSink : IVsTextBufferDataEvents
{
    private readonly Action _action;
    private readonly IConnectionPoint _connectionPoint;
    private uint _cookie;

    public static void Subscribe(IVsTextBuffer vsTextBuffer, Action action)
    {
        if (vsTextBuffer is null)
        {
            throw new ArgumentNullException(nameof(vsTextBuffer));
        }

        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        var connectionPointContainer = (IConnectionPointContainer)vsTextBuffer;

        var guid = typeof(IVsTextBufferDataEvents).GUID;
        connectionPointContainer.FindConnectionPoint(ref guid, out var connectionPoint);

        var sink = new VsTextBufferDataEventsSink(connectionPoint, action);
        connectionPoint.Advise(sink, out sink._cookie);
    }

    private VsTextBufferDataEventsSink(IConnectionPoint connectionPoint, Action action)
    {
        _connectionPoint = connectionPoint;
        _action = action;
    }

    public void OnFileChanged(uint grfChange, uint dwFileAttrs)
    {
        // ignore
    }

    public int OnLoadCompleted(int fReload)
    {
        _connectionPoint.Unadvise(_cookie);
        _action();

        return VSConstants.S_OK;
    }
}
