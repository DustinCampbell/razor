// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class TagHelperBinding
{
    public string TagName { get; }
    public string? ParentTagName { get; }
    public ImmutableArray<KeyValuePair<string, string>> Attributes { get; }
    public ImmutableArray<BoundRulesInfo> BoundRules { get; }
    public string? TagHelperPrefix { get; }

    private ImmutableArray<TagHelperDescriptor> _descriptors;

    internal TagHelperBinding(
        string tagName,
        ImmutableArray<KeyValuePair<string, string>> attributes,
        string? parentTagName,
        ImmutableArray<BoundRulesInfo> boundRules,
        string? tagHelperPrefix)
    {
        TagName = tagName;
        Attributes = attributes;
        ParentTagName = parentTagName;
        BoundRules = boundRules;
        TagHelperPrefix = tagHelperPrefix;
    }

    public ImmutableArray<TagHelperDescriptor> Descriptors
    {
        get
        {
            if (_descriptors.IsDefault)
            {
                ImmutableInterlocked.InterlockedInitialize(ref _descriptors, BoundRules.SelectAsArray(static b => b.Descriptor));
            }

            return _descriptors;
        }
    }

    public ImmutableArray<TagMatchingRuleDescriptor> GetBoundRules(TagHelperDescriptor descriptor)
    {
        return BoundRules.First(descriptor, static (x, y) => x.Descriptor.Equals(y)).Rules;
    }

    /// <summary>
    /// Gets a value that indicates whether the the binding matched on attributes only.
    /// </summary>
    /// <returns><c>false</c> if the entire element should be classified as a tag helper.</returns>
    /// <remarks>
    /// If this returns <c>true</c>, use <c>TagHelperFactsService.GetBoundTagHelperAttributes</c> to find the
    /// set of attributes that should be considered part of the match.
    /// </remarks>
    public bool IsAttributeMatch
    {
        get
        {
            foreach (var boundRulesInfo in BoundRules)
            {
                var tagHelper = boundRulesInfo.Descriptor;
                if (!tagHelper.Metadata.TryGetValue(TagHelperMetadata.Common.ClassifyAttributesOnly, out var value) ||
                    !string.Equals(value, bool.TrueString, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // All the matching tag helpers want to be classified with **attributes only**.
            //
            // Ex: (components)
            //
            //      <button onclick="..." />
            return true;
        }
    }
}
