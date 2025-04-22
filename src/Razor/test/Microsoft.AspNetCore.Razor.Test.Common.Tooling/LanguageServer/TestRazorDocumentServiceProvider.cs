// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.ExternalAccess.Razor;

namespace Microsoft.AspNetCore.Razor.Test.Common.LanguageServer;

internal sealed class TestRazorDocumentServiceProvider(IRazorMappingService? mappingService) : IRazorDocumentServiceProvider
{
    private readonly IRazorMappingService? _mappingService = mappingService;

    public bool CanApplyChange => true;

    public bool SupportDiagnostics => true;

    TService? IRazorDocumentServiceProvider.GetService<TService>()
        where TService : class
    {
        var serviceType = typeof(TService);

        if (serviceType == typeof(IRazorMappingService))
        {
            return (TService?)_mappingService;
        }

        if (serviceType == typeof(IRazorDocumentPropertiesService))
        {
            return (TService?)TestRazorDocumentPropertiesService.Instance;
        }

        return (this as TService).AssumeNotNull();
    }

    private sealed class TestRazorDocumentPropertiesService : IRazorDocumentPropertiesService
    {
        public static IRazorDocumentPropertiesService Instance { get; } = new TestRazorDocumentPropertiesService();

        private TestRazorDocumentPropertiesService()
        {
        }

        public bool DesignTimeOnly => false;

        public string DiagnosticsLspClientName => "RazorCSharp";
    }
}
