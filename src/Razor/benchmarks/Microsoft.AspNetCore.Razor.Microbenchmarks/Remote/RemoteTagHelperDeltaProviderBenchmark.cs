// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote.Razor;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

public class RemoteTagHelperDeltaProviderBenchmark
{
    public RemoteTagHelperDeltaProviderBenchmark()
    {
        DefaultTagHelperSet = CommonResources.LegacyTagHelpers;

        Added50PercentMoreDefaultTagHelpers = TagHelperCollection.Create(
            DefaultTagHelperSet
                .Take(DefaultTagHelperSet.Count / 2)
                .Select(th => th.WithName(th.Name + "Added"))
                .Concat(DefaultTagHelperSet));

        RemovedHalfOfDefaultTagHelpers = TagHelperCollection.Create(
            DefaultTagHelperSet.Take(CommonResources.LegacyTagHelpers.Count / 2));

        var tagHelpersToMutate = DefaultTagHelperSet
            .Take(2)
            .Select(th => th.WithName(th.Name + "Mutated"));
        MutatedTwoDefaultTagHelpers = TagHelperCollection.Create(
            DefaultTagHelperSet
                .Skip(2)
                .Concat(tagHelpersToMutate));

        DefaultTagHelperChecksumsSet = DefaultTagHelperSet.SelectAsArray(t => t.Checksum);
        Added50PercentMoreDefaultTagHelpersChecksums = Added50PercentMoreDefaultTagHelpers.SelectAsArray(t => t.Checksum);
        RemovedHalfOfDefaultTagHelpersChecksums = RemovedHalfOfDefaultTagHelpers.SelectAsArray(t => t.Checksum);
        MutatedTwoDefaultTagHelpersChecksums = MutatedTwoDefaultTagHelpers.SelectAsArray(t => t.Checksum);

        ProjectId = ProjectId.CreateNewId();
    }

    private TagHelperCollection DefaultTagHelperSet { get; }
    private ImmutableArray<Checksum> DefaultTagHelperChecksumsSet { get; }
    private TagHelperCollection Added50PercentMoreDefaultTagHelpers { get; }
    private ImmutableArray<Checksum> Added50PercentMoreDefaultTagHelpersChecksums { get; }
    private TagHelperCollection RemovedHalfOfDefaultTagHelpers { get; }
    private ImmutableArray<Checksum> RemovedHalfOfDefaultTagHelpersChecksums { get; }
    private TagHelperCollection MutatedTwoDefaultTagHelpers { get; }
    private ImmutableArray<Checksum> MutatedTwoDefaultTagHelpersChecksums { get; }
    private ProjectId ProjectId { get; }

    [AllowNull]
    private RemoteTagHelperDeltaProvider Provider { get; set; }

    private int LastResultId { get; set; }

    [IterationSetup]
    public void IterationSetup()
    {
        Provider = new RemoteTagHelperDeltaProvider();
        var delta = Provider.GetTagHelpersDelta(ProjectId, lastResultId: -1, DefaultTagHelperChecksumsSet);
        LastResultId = delta.ResultId;
    }

    [Benchmark(Description = "Calculate Delta - New project")]
    public void TagHelper_GetTagHelpersDelta_NewProject()
    {
        var projectId = ProjectId.CreateNewId();
        _ = Provider.GetTagHelpersDelta(projectId, lastResultId: -1, DefaultTagHelperChecksumsSet);
    }

    [Benchmark(Description = "Calculate Delta - Remove project")]
    public void TagHelper_GetTagHelpersDelta_RemoveProject()
    {
        _ = Provider.GetTagHelpersDelta(ProjectId, LastResultId, ImmutableArray<Checksum>.Empty);
    }

    [Benchmark(Description = "Calculate Delta - Add lots of TagHelpers")]
    public void TagHelper_GetTagHelpersDelta_AddLots()
    {
        _ = Provider.GetTagHelpersDelta(ProjectId, LastResultId, Added50PercentMoreDefaultTagHelpersChecksums);
    }

    [Benchmark(Description = "Calculate Delta - Remove lots of TagHelpers")]
    public void TagHelper_GetTagHelpersDelta_RemoveLots()
    {
        _ = Provider.GetTagHelpersDelta(ProjectId, LastResultId, RemovedHalfOfDefaultTagHelpersChecksums);
    }

    [Benchmark(Description = "Calculate Delta - Mutate two TagHelpers")]
    public void TagHelper_GetTagHelpersDelta_Mutate2()
    {
        _ = Provider.GetTagHelpersDelta(ProjectId, LastResultId, MutatedTwoDefaultTagHelpersChecksums);
    }

    [Benchmark(Description = "Calculate Delta - No change")]
    public void TagHelper_GetTagHelpersDelta_NoChange()
    {
        _ = Provider.GetTagHelpersDelta(ProjectId, LastResultId, DefaultTagHelperChecksumsSet);
    }
}
