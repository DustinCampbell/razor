﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Endpoints;

internal partial class RazorCustomMessageTarget
{
    // Called by the Razor Language Server to retrieve the user's latest settings.
    // NOTE: This method is a poly-fill for VS. We only intend to do it this way until VS formally
    // supports sending workspace configuration requests.
    [JsonRpcMethod(Methods.WorkspaceConfigurationName, UseSingleObjectParameterDeserialization = true)]
    public Task<object[]> WorkspaceConfigurationAsync(ConfigurationParams configParams, CancellationToken _)
    {
        if (configParams is null)
        {
            throw new ArgumentNullException(nameof(configParams));
        }

        var result = new List<object>();
        foreach (var item in configParams.Items)
        {
            // Right now in VS we only care about editor settings, but we should update this logic later if
            // we want to support Razor and HTML settings as well.
            var setting = item.Section switch
            {
                "vs.editor.razor" => _editorSettingsManager.GetClientSettings(),
                _ => new object()
            };

            result.Add(setting);
        }

        return Task.FromResult(result.ToArray());
    }
}
