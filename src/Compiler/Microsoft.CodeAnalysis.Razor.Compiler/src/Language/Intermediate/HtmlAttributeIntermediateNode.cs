// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class HtmlAttributeIntermediateNode : IntermediateNode
{
    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public CSharpExpressionIntermediateNode AttributeNameExpression { get; set; }

    public string AttributeName { get; set; }

    public string Prefix { get; set; }

    public string Suffix { get; set; }

    public string EventUpdatesAttributeName { get; set; }

    public string OriginalAttributeName { get; set; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        if (visitor == null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        visitor.VisitHtmlAttribute(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(AttributeName);

        formatter.WriteProperty(nameof(AttributeName), AttributeName);

        string attributeName;

        if (AttributeNameExpression is { } attributeNameExpression)
        {
            using var _ = ArrayBuilderPool<ReadOnlyMemory<char>>.GetPooledObject(out var builder);

            foreach (var token in attributeNameExpression.FindDescendantNodes<IntermediateToken>())
            {
                foreach (var part in token.Content.AllParts)
                {
                    builder.Add(part);
                }
            }

            attributeName = builder.Join();
        }
        else
        {
            attributeName = string.Empty;
        }

        formatter.WriteProperty(nameof(AttributeNameExpression), attributeName);
        formatter.WriteProperty(nameof(Prefix), Prefix);
        formatter.WriteProperty(nameof(Suffix), Suffix);
        formatter.WriteProperty(nameof(EventUpdatesAttributeName), EventUpdatesAttributeName);
    }
}
