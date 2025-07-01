// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal sealed class RazorSourceChecksumAttributeIntermediateNode(
    ImmutableArray<byte> checksum, SourceHashAlgorithm checksumAlgorithm, string identifier)
    : ExtensionIntermediateNode
{
    public const string AttributeName = "global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute";

    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public ImmutableArray<byte> Checksum { get; } = checksum;
    public SourceHashAlgorithm ChecksumAlgorithm { get; } = checksumAlgorithm;
    public string Identifier { get; } = identifier;

    public override void Accept(IntermediateNodeVisitor visitor)
        => AcceptExtensionNode(this, visitor);

    public override void WriteNode(CodeTarget target, CodeRenderingContext context)
    {
        // [global::...RazorSourceChecksum(@"{ChecksumAlgorithm}", @"{Checksum}", @"{Identifier}")]
        context.CodeWriter.WriteLine($"""
            [{AttributeName}(@"{ChecksumAlgorithm.ToString()}", @"{ChecksumUtilities.BytesToString(Checksum)}", @"{Identifier}")]
            """);
    }
}
