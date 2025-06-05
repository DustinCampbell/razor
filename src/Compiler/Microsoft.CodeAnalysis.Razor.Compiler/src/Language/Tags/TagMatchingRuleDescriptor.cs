// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public sealed class TagMatchingRuleDescriptor : TagHelperObject<TagMatchingRuleDescriptor>
{
    private readonly TagMatchingRuleFlags _flags;

    private TagHelperDescriptor? _parent;

    internal TagMatchingRuleFlags Flags => _flags;

    public string TagName { get; }
    public string? ParentTag { get; }
    public ImmutableArray<RequiredAttributeDescriptor> Attributes { get; }

    public bool CaseSensitive => _flags.IsFlagSet(TagMatchingRuleFlags.CaseSensitive);
    public TagStructure TagStructure => _flags.GetTagStructure();

    internal TagMatchingRuleDescriptor(
        TagMatchingRuleFlags flags,
        string tagName,
        string? parentTag,
        ImmutableArray<RequiredAttributeDescriptor> attributes,
        ImmutableArray<RazorDiagnostic> diagnostics)
        : base(diagnostics)
    {
        _flags = flags;
        TagName = tagName;
        ParentTag = parentTag;
        Attributes = attributes.NullToEmpty();

        foreach (var attribute in Attributes)
        {
            attribute.SetParent(this);
        }
    }

    private protected override void BuildChecksum(in Checksum.Builder builder)
    {
        builder.AppendData((byte)_flags);
        builder.AppendData(TagName);
        builder.AppendData(ParentTag);

        foreach (var descriptor in Attributes)
        {
            builder.AppendData(descriptor.Checksum);
        }
    }

    internal void SetParent(TagHelperDescriptor parent)
    {
        if (_parent is not null)
        {
            ThrowHelper.ThrowInvalidOperationException("Parent can only be set once.");
        }

        _parent = parent;
    }

    public TagHelperDescriptor Parent
        => _parent ?? Assumed.Unreachable<TagHelperDescriptor>($"{nameof(Parent)} not set.");

    public IEnumerable<RazorDiagnostic> GetAllDiagnostics()
    {
        foreach (var attribute in Attributes)
        {
            foreach (var diagnostic in attribute.Diagnostics)
            {
                yield return diagnostic;
            }
        }

        foreach (var diagnostic in Diagnostics)
        {
            yield return diagnostic;
        }
    }

    internal string GetDebuggerDisplay()
    {
        using var _ = StringBuilderPool.GetPooledObject(out var builder);

        builder.Append(TagName);

        if (TagStructure == TagStructure.WithoutEndTag)
        {
            builder.Append('/');
        }

        var isFirst = true;

        foreach (var attribute in Attributes)
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                builder.Append(", ");
            }

            if (attribute.NameComparison == RequiredAttributeNameComparison.PrefixMatch)
            {
                builder.Append('^');
            }

            builder.Append(attribute.Name);

            if (attribute.Value is string value)
            {
                switch (attribute.ValueComparison)
                {
                    case RequiredAttributeValueComparison.PrefixMatch:
                        builder.Append("^=");
                        break;

                    case RequiredAttributeValueComparison.SuffixMatch:
                        builder.Append("$=");
                        break;

                    default:
                        builder.Append('=');
                        break;
                }

                builder.Append(value);
            }
        }

        return builder.ToString();
    }
}
