// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Reports;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

internal static class SummaryExtensions
{
    public static int ToExitCode(this IEnumerable<Summary> summaries)
    {
        // an empty summary means that initial filtering and validation did not allow
        // any benchmarks to run.
        if (!summaries.Any())
        {
            return 1;
        }

        // If anything has failed, it's an error.
        if (summaries.Any(summary => summary.HasAnyErrors()))
        {
            return 1;
        }

        return 0;
    }

    public static bool HasAnyErrors(this Summary summary)
        => summary.HasCriticalValidationErrors ||
           summary.Reports.Any(report => report.HasAnyErrors());

    public static bool HasAnyErrors(this BenchmarkReport report)
        => !report.BuildResult.IsBuildSuccess ||
           !report.AllMeasurements.Any();

    internal static TagHelperDescriptor WithName(this TagHelperDescriptor value, string name)
    {
        return new(
            value.Flags, value.Kind, value.RuntimeKind, name, value.AssemblyName, value.DisplayName,
            value.TypeNameObject, value.DocumentationObject, value.TagOutputHint,
            value.AllowedChildTags, value.BoundAttributes, value.TagMatchingRules,
            value.Metadata, value.Checksum, value.Diagnostics);
    }
}
