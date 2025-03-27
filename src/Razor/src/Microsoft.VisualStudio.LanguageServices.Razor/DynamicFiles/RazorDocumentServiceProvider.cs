// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;

namespace Microsoft.VisualStudio.Razor.DynamicFiles;

internal class RazorDocumentServiceProvider(IDynamicDocumentContainer? documentContainer) : IRazorDocumentServiceProvider
{
    private readonly IDynamicDocumentContainer? _documentContainer = documentContainer;
    private readonly object _lock = new();

    private Optional<IRazorDocumentExcerptServiceImplementation?> _documentExcerptService;
    private Optional<IRazorDocumentPropertiesService?> _documentPropertiesService;
    private Optional<IRazorMappingService?> _mappingService;

    public RazorDocumentServiceProvider()
        : this(null)
    {
    }

    public bool CanApplyChange => false;

    public bool SupportDiagnostics => _documentContainer?.SupportsDiagnostics ?? false;

    public TService? GetService<TService>() where TService : class
    {
        if (_documentContainer is null)
        {
            return this as TService;
        }

        var serviceType = typeof(TService);

        if (serviceType == typeof(IRazorDocumentExcerptServiceImplementation))
        {
            if (!_documentExcerptService.HasValue)
            {
                lock (_lock)
                {
                    _documentExcerptService = new(_documentContainer.GetExcerptService());
                }
            }

            return (TService?)_documentExcerptService.Value;
        }

        if (serviceType == typeof(IRazorDocumentPropertiesService))
        {
            if (!_documentPropertiesService.HasValue)
            {
                lock (_lock)
                {
                    _documentPropertiesService = new(_documentContainer.GetDocumentPropertiesService());
                }
            }

            return (TService?)_documentPropertiesService.Value;
        }

        if (serviceType == typeof(IRazorMappingService))
        {
            if (!_mappingService.HasValue)
            {
                lock (_lock)
                {
                    _mappingService = new(_documentContainer.GetMappingService());
                }
            }

            return (TService?)_mappingService.Value;
        }

        return this as TService;
    }
}
