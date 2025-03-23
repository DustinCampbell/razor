// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// Enables retrieval of <see cref="TagHelperBinding"/>'s.
/// </summary>
internal sealed class TagHelperBinder
{
    private static readonly ObjectPool<Dictionary<string, ImmutableArray<TagHelperDescriptor>.Builder>> s_mapPool =
        DictionaryPool<string, ImmutableArray<TagHelperDescriptor>.Builder>.Create(StringComparer.OrdinalIgnoreCase);

    public string? TagHelperPrefix { get; }
    public ImmutableArray<TagHelperDescriptor> TagHelpers { get; }

    private readonly ImmutableArray<TagHelperDescriptor> _catchAllTagHelpers;
    private readonly Dictionary<string, ImmutableArray<TagHelperDescriptor>>? _tagNameToTagHelpersMap;

    /// <summary>
    /// Instantiates a new instance of the <see cref="TagHelperBinder"/>.
    /// </summary>
    /// <param name="tagHelperPrefix">The tag helper prefix being used by the document.</param>
    /// <param name="tagHelpers">The <see cref="TagHelperDescriptor"/>s that the <see cref="TagHelperBinder"/>
    /// will pull from.</param>
    public TagHelperBinder(string? tagHelperPrefix, ImmutableArray<TagHelperDescriptor> tagHelpers)
    {
        TagHelperPrefix = tagHelperPrefix;
        TagHelpers = tagHelpers;

        ProcessTagHelpers(tagHelperPrefix, tagHelpers, out _catchAllTagHelpers, out _tagNameToTagHelpersMap);
    }

    public static void ProcessTagHelpers(
        string? prefix,
        ImmutableArray<TagHelperDescriptor> descriptors,
        out ImmutableArray<TagHelperDescriptor> catchAllTagHelpers,
        out Dictionary<string, ImmutableArray<TagHelperDescriptor>>? tagNameToTagHelpersMap)
    {
        using var catchAllTagHelpersBuilder = new PooledArrayBuilder<TagHelperDescriptor>();
        using var pooledDictionary = s_mapPool.GetPooledObject(out var tagNameToTagHelpersMapBuilder);
        using var pooledSet = HashSetPool<TagHelperDescriptor>.GetPooledObject(out var processedSet);

        foreach (var descriptor in descriptors)
        {
            if (!processedSet.Add(descriptor))
            {
                continue;
            }

            foreach (var rule in descriptor.TagMatchingRules)
            {
                if (rule.TagName == TagHelperMatchingConventions.ElementCatchAllName)
                {
                    catchAllTagHelpersBuilder.Add(descriptor);
                }
                else
                {
                    var tagName = !prefix.IsNullOrEmpty()
                        ? prefix + rule.TagName
                        : rule.TagName;

                    var builder = tagNameToTagHelpersMapBuilder.GetOrAdd(tagName, _ => ImmutableArray.CreateBuilder<TagHelperDescriptor>());
                    builder.Add(descriptor);
                }
            }
        }

        catchAllTagHelpers = catchAllTagHelpersBuilder.DrainToImmutable();
        tagNameToTagHelpersMap = null;

        if (tagNameToTagHelpersMapBuilder.Count > 0)
        {
            tagNameToTagHelpersMap = new(capacity: tagNameToTagHelpersMapBuilder.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var (key, value) in tagNameToTagHelpersMapBuilder)
            {
                tagNameToTagHelpersMap.Add(key, value.DrainToImmutable());
            }
        }
    }

    private bool TryGetMatchingDescriptors(string tagName, out ImmutableArray<TagHelperDescriptor> result)
    {
        if (_tagNameToTagHelpersMap is null)
        {
            result = default;
            return false;
        }

        return _tagNameToTagHelpersMap.TryGetValue(tagName, out result);
    }

    /// <summary>
    /// Gets all tag helpers that match the given HTML tag criteria.
    /// </summary>
    /// <param name="tagName">The name of the HTML tag to match. Providing a '*' tag name
    /// retrieves catch-all <see cref="TagHelperDescriptor"/>s (descriptors that target every tag).</param>
    /// <param name="attributes">Attributes on the HTML tag.</param>
    /// <param name="parentTagName">The parent tag name of the given <paramref name="tagName"/> tag.</param>
    /// <param name="parentIsTagHelper">Is the parent tag of the given <paramref name="tagName"/> tag a tag helper.</param>
    /// <returns><see cref="TagHelperDescriptor"/>s that apply to the given HTML tag criteria.
    /// Will return <c>null</c> if no <see cref="TagHelperDescriptor"/>s are a match.</returns>
    public TagHelperBinding? GetBinding(
        string tagName,
        ImmutableArray<KeyValuePair<string, string>> attributes,
        string? parentTagName,
        bool parentIsTagHelper)
    {
        if (!TagHelperPrefix.IsNullOrEmpty() &&
            (tagName.Length <= TagHelperPrefix.Length ||
             !tagName.StartsWith(TagHelperPrefix, StringComparison.OrdinalIgnoreCase)))
        {
            // The tagName doesn't have the tag helper prefix, we can short circuit.
            return null;
        }

        var tagNameWithoutPrefix = tagName.AsSpanOrDefault();
        var parentTagNameWithoutPrefix = parentTagName.AsSpanOrDefault();

        if (TagHelperPrefix is { Length: var length and > 0 })
        {
            tagNameWithoutPrefix = tagNameWithoutPrefix[length..];

            if (parentIsTagHelper)
            {
                parentTagNameWithoutPrefix = parentTagNameWithoutPrefix[length..];
            }
        }

        using var pooledSet = HashSetPool<TagHelperDescriptor>.GetPooledObject(out var distinctSet);
        using var bindings = new PooledArrayBuilder<BoundRulesInfo>();

        // First, try any tag helpers with this tag name.
        if (TryGetMatchingDescriptors(tagName, out var matchingDescriptors))
        {
            FindApplicableDescriptors(matchingDescriptors, tagNameWithoutPrefix, parentTagNameWithoutPrefix, attributes, distinctSet, ref bindings.AsRef());
        }

        // Next, try any "catch all" descriptors.
        FindApplicableDescriptors(_catchAllTagHelpers, tagNameWithoutPrefix, parentTagNameWithoutPrefix, attributes, distinctSet, ref bindings.AsRef());

        if (bindings.Count == 0)
        {
            return null;
        }

        return new TagHelperBinding(
            tagName,
            attributes,
            parentTagName,
            bindings.DrainToImmutable(),
            TagHelperPrefix);

        static void FindApplicableDescriptors(
            ImmutableArray<TagHelperDescriptor> descriptors,
            ReadOnlySpan<char> tagNameWithoutPrefix,
            ReadOnlySpan<char> parentTagNameWithoutPrefix,
            ImmutableArray<KeyValuePair<string, string>> attributes,
            HashSet<TagHelperDescriptor> distinctSet,
            ref PooledArrayBuilder<BoundRulesInfo> bindings)
        {
            using var applicableRules = new PooledArrayBuilder<TagMatchingRuleDescriptor>();

            foreach (var descriptor in descriptors)
            {
                if (!distinctSet.Add(descriptor))
                {
                    continue;
                }

                foreach (var rule in descriptor.TagMatchingRules)
                {
                    if (TagHelperMatchingConventions.SatisfiesRule(rule, tagNameWithoutPrefix, parentTagNameWithoutPrefix, attributes))
                    {
                        applicableRules.Add(rule);
                    }
                }

                if (applicableRules.Count == descriptor.TagMatchingRules.Length)
                {
                    bindings.Add(new(descriptor, descriptor.TagMatchingRules));
                }
                else if (applicableRules.Count > 0)
                {
                    bindings.Add(new(descriptor, applicableRules.DrainToImmutable()));
                }

                applicableRules.Clear();
            }
        }
    }
}
