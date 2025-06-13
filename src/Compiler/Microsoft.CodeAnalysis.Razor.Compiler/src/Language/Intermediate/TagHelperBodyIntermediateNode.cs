// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class TagHelperBodyIntermediateNode : IntermediateNode
{
    public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitTagHelperBody(this);
}
