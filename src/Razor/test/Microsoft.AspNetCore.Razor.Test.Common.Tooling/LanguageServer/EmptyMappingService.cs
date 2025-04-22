// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Test.Common.LanguageServer;

internal sealed class EmptyMappingService : IRazorMappingService
{
    public static IRazorMappingService Instance { get; } = new EmptyMappingService();

    private EmptyMappingService()
    {
    }

    public Task<ImmutableArray<RazorMappedSpanResult>> MapSpansAsync(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken)
        => SpecializedTasks.EmptyImmutableArray<RazorMappedSpanResult>();

    public Task<ImmutableArray<RazorMappedEditResult>> MapTextChangesAsync(Document oldDocument, Document newDocument, CancellationToken cancellationToken)
        => SpecializedTasks.EmptyImmutableArray<RazorMappedEditResult>();
}
