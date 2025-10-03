﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class TagHelperBinding
{
    public ImmutableArray<TagHelperBoundRulesInfo> AllBoundRules { get; }
    public string? TagNamePrefix { get; }
    public string TagName { get; }
    public string? ParentTagName { get; }
    public ImmutableArray<KeyValuePair<string, string>> Attributes { get; }

    private TagHelperCollection? _tagHelpers;
    private bool? _isAttributeMatch;

    internal TagHelperBinding(
        ImmutableArray<TagHelperBoundRulesInfo> allBoundRules,
        string tagName,
        string? parentTagName,
        ImmutableArray<KeyValuePair<string, string>> attributes,
        string? tagNamePrefix)
    {
        AllBoundRules = allBoundRules;
        TagName = tagName;
        ParentTagName = parentTagName;
        Attributes = attributes;
        TagNamePrefix = tagNamePrefix;
    }

    public TagHelperCollection TagHelpers
    {
        get
        {
            return _tagHelpers ?? InterlockedOperations.Initialize(ref _tagHelpers, CreateTagHelpers(AllBoundRules));

            static TagHelperCollection CreateTagHelpers(ImmutableArray<TagHelperBoundRulesInfo> boundRules)
            {
                return TagHelperCollection.Build(boundRules, capacity: boundRules.Length, (ref builder, boundRules) =>
                {
                    foreach (var boundRule in boundRules)
                    {
                        builder.Add(boundRule.Descriptor);
                    }
                });
            }
        }
    }

    public ImmutableArray<TagMatchingRuleDescriptor> GetBoundRules(TagHelperDescriptor descriptor)
        => AllBoundRules.First(descriptor, static (info, d) => info.Descriptor.Equals(d)).Rules;

    /// <summary>
    ///  Gets a value that indicates whether the the binding matched on attributes only.
    /// </summary>
    /// <returns>
    ///  Returns <see langword="false"/> if the entire element should be classified as a tag helper.
    /// </returns>
    /// <remarks>
    ///  If this returns <see langword="true"/>, use <c>TagHelperFactsService.GetBoundTagHelperAttributes</c> to find the
    ///  set of attributes that should be considered part of the match.
    /// </remarks>
    public bool IsAttributeMatch
    {
        get
        {
            return _isAttributeMatch ??= ComputeIsAttributeMatch(TagHelpers);

            static bool ComputeIsAttributeMatch(TagHelperCollection tagHelpers)
            {
                foreach (var tagHelper in tagHelpers)
                {
                    if (!tagHelper.ClassifyAttributesOnly)
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
}
