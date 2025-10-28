// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

internal static class TagHelperCollectionHelpers
{
    public static TagHelperDescriptor[] CreateTagHelpers(int count)
    {
        var result = new TagHelperDescriptor[count];

        for (var i = 0; i < count; i++)
        {
            var builder = TagHelperDescriptorBuilder.Create($"TestTagHelper{i}", "TestAssembly");
            builder.TypeName = $"TestTagHelper{i}";
            builder.TagMatchingRule(rule => rule.TagName = $"test{i}");

            result[i] = builder.Build();
        }

        return result;
    }

    public static TagHelperDescriptor[] CreateTagHelpersWithDuplicates(int count)
    {
        var uniqueHelpers = CreateTagHelpers(count / 2);
        var result = new TagHelperDescriptor[count];

        for (var i = 0; i < uniqueHelpers.Length; i++)
        {
            result[i] = uniqueHelpers[i];
        }

        for (var i = uniqueHelpers.Length; i < count; i++)
        {
            result[i] = uniqueHelpers[i % uniqueHelpers.Length];
        }

        return result;
    }

    public static TagHelperCollection[] CreateTagHelperCollections(int collectionCount, int helpersPerCollection)
    {
        var result = new TagHelperCollection[collectionCount];

        for (var i = 0; i < collectionCount; i++)
        {
            var helpers = CreateTagHelpers(helpersPerCollection);

            for (var j = 0; j < helpers.Length; j++)
            {
                var builder = TagHelperDescriptorBuilder.Create($"Collection{i}TagHelper{j}", "TestAssembly");
                builder.TypeName = $"Collection{i}TagHelper{j}";
                builder.TagMatchingRule(rule => rule.TagName = $"collection{i}test{j}");

                helpers[j] = builder.Build();
            }

            result[i] = TagHelperCollection.Create(helpers);
        }

        return result;
    }

}
