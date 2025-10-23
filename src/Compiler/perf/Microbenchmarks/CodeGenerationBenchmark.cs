// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class CodeGenerationBenchmark
{
    public CodeGenerationBenchmark()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "MSN.cshtml")))
        {
            current = current.Parent;
        }

        var root = current;
        var fileSystem = RazorProjectFileSystem.Create(root.FullName);

        ProjectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, fileSystem, b => RazorExtensions.Register(b)); ;

        MSN = fileSystem.GetItem(Path.Combine(root.FullName, "MSN.cshtml"), RazorFileKind.Legacy);
    }

    public RazorProjectEngine ProjectEngine { get; }

    public RazorProjectItem MSN { get; }

    [Benchmark(Description = "Razor Design Time Code Generation of MSN.com", OperationsPerInvoke = 2)]
    public void CodeGeneration_DesignTime_LargeStaticFile()
    {
        // Process 2 times to reach ~130ms (2 × 65ms)
        for (var i = 0; i < 2; i++)
        {
            var codeDocument = ProjectEngine.ProcessDesignTime(MSN);
            var generated = codeDocument.GetRequiredCSharpDocument();

            if (generated.Diagnostics.Length > 0)
            {
                throw new Exception("Error!" + Environment.NewLine + string.Join(Environment.NewLine, generated.Diagnostics));
            }
        }
    }

    [Benchmark(Description = "Razor Runtime Code Generation of MSN.com", OperationsPerInvoke = 2)]
    public void CodeGeneration_Runtime_LargeStaticFile()
    {
        // Process 2 times to reach ~150ms (2 × 76ms)
        for (var i = 0; i < 2; i++)
        {
            var codeDocument = ProjectEngine.Process(MSN);
            var generated = codeDocument.GetRequiredCSharpDocument();

            if (generated.Diagnostics.Length > 0)
            {
                throw new Exception("Error!" + Environment.NewLine + string.Join(Environment.NewLine, generated.Diagnostics));
            }
        }
    }
}
