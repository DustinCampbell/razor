﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

public class BreakpointSpanTests(ITestOutputHelper testOutputHelper) : AbstractRazorEditorTest(testOutputHelper)
{
    [IdeFact]
    public async Task SetBreakpoint_FirstCharacter_SpanAdjusts()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, ControlledHangMitigatingCancellationToken);

        // Wait for classifications to indicate Razor LSP is up and running
        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.RazorProjectSystem.WaitForCSharpVirtualDocumentUpdateAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, async () =>
        {
            await TestServices.Editor.SetTextAsync("<p>@{ var abc = 123; }</p>", ControlledHangMitigatingCancellationToken);
        }, ControlledHangMitigatingCancellationToken);

        Assert.True(await TestServices.Debugger.SetBreakpointAsync(RazorProjectConstants.CounterRazorFile, line: 1, character: 1, ControlledHangMitigatingCancellationToken));

        Assert.True(await TestServices.Debugger.VerifyBreakpointAsync(RazorProjectConstants.CounterRazorFile, line: 1, character: 7, ControlledHangMitigatingCancellationToken));
    }

    [IdeFact]
    public async Task SetBreakpoint_FirstCharacter_InvalidLine()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, ControlledHangMitigatingCancellationToken);

        // Wait for classifications to indicate Razor LSP is up and running
        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.RazorProjectSystem.WaitForCSharpVirtualDocumentUpdateAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, async () =>
        {
            await TestServices.Editor.SetTextAsync("""
                <p>@{
                    var abc = 123;
                }</p>
                """, ControlledHangMitigatingCancellationToken);
        }, ControlledHangMitigatingCancellationToken);

        Assert.False(await TestServices.Debugger.SetBreakpointAsync(RazorProjectConstants.CounterRazorFile, line: 1, character: 1, ControlledHangMitigatingCancellationToken));
    }

    [IdeFact]
    public async Task SetBreakpoint_FirstCharacter_ValidLine()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, ControlledHangMitigatingCancellationToken);

        // Wait for classifications to indicate Razor LSP is up and running
        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        await TestServices.RazorProjectSystem.WaitForCSharpVirtualDocumentUpdateAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, async () =>
        {
            await TestServices.Editor.SetTextAsync("""
                <p>@{
                    var abc = 123;
                }</p>
                """, ControlledHangMitigatingCancellationToken);
        }, ControlledHangMitigatingCancellationToken);

        Assert.True(await TestServices.Debugger.SetBreakpointAsync(RazorProjectConstants.CounterRazorFile, line: 2, character: 1, ControlledHangMitigatingCancellationToken));

        Assert.True(await TestServices.Debugger.VerifyBreakpointAsync(RazorProjectConstants.CounterRazorFile, line: 2, character: 5, ControlledHangMitigatingCancellationToken));
    }
}
