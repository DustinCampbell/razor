// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

// Optimization pass is the best choice for this class. It's not an optimization, but it also doesn't add semantically
// meaningful information.
internal class MetadataAttributePass : IntermediateNodePassBase, IRazorOptimizationPass
{
    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        if (documentNode.Options == null || documentNode.Options.SuppressMetadataAttributes)
        {
            // Metadata attributes are turned off (or options not populated), nothing to do.
            return;
        }

        if (string.Equals(documentNode.DocumentKind, ComponentDocumentClassifierPass.ComponentDocumentKind, StringComparison.Ordinal))
        {
            // Metadata attributes are not used for components.
            return;
        }

        // We need to be able to compute the data we need for the [RazorCompiledItem] attribute - that includes
        // a full type name, and a document kind, and optionally an identifier.
        //
        // If we can't use [RazorCompiledItem] then we don't care about the rest of the attributes.
        var @namespace = documentNode.FindPrimaryNamespace();
        if (@namespace == null)
        {
            // No namespace node. Skip.
            return;
        }

        var @class = documentNode.FindPrimaryClass();
        if (@class == null || string.IsNullOrEmpty(@class.ClassName))
        {
            // No class node or it's incomplete. Skip.
            return;
        }

        if (documentNode.DocumentKind == null)
        {
            // No document kind. Skip.
            return;
        }

        var sourceDocument = codeDocument.Source;
        var identifier = sourceDocument.GetIdentifier();
        if (identifier == null)
        {
            // No identifier. Skip
            return;
        }

        // [RazorCompiledItem] is an [assembly: ... ] attribute, so it needs to be applied at the global scope.
        var compiledItemAttributeNode = new RazorCompiledItemAttributeIntermediateNode(
            typeName: !@namespace.Content.IsNullOrEmpty()
                ? $"{@namespace.Content}.{@class.ClassName}"
                : @class.ClassName,
            kind: documentNode.DocumentKind,
            identifier);

        documentNode.Children.Insert(0, compiledItemAttributeNode);

        // Now we need to add a [RazorSourceChecksum] for the source and for each import
        // these are class attributes, so we need to find the insertion point to put them
        // right before the class.
        var insert = -1;
        for (var j = 0; j < @namespace.Children.Count; j++)
        {
            if (ReferenceEquals(@namespace.Children[j], @class))
            {
                insert = j;
                break;
            }
        }

        if (insert < 0)
        {
            // Can't find a place to put the attributes, just bail.
            return;
        }

        if (documentNode.Options.SuppressMetadataSourceChecksumAttributes)
        {
            // Checksum attributes are turned off (or options not populated), nothing to do.
            return;
        }

        // Checksum of the main source
        var checksum = sourceDocument.Text.GetChecksum();
        var checksumAlgorithm = sourceDocument.Text.ChecksumAlgorithm;
        if (checksum.IsEmpty || checksumAlgorithm is SourceHashAlgorithm.None)
        {
            // Don't generate anything unless we have all of the required information.
            return;
        }

        var sourceChecksumAttributeNode = new RazorSourceChecksumAttributeIntermediateNode(checksum, checksumAlgorithm, identifier);
        @namespace.Children.Insert(insert++, sourceChecksumAttributeNode);

        // Now process the checksums of the imports

        foreach (var import in codeDocument.Imports)
        {
            checksum = import.Text.GetChecksum();
            checksumAlgorithm = import.Text.ChecksumAlgorithm;
            identifier = import.GetIdentifier();

            if (checksum.IsEmpty || checksumAlgorithm is SourceHashAlgorithm.None || identifier == null)
            {
                // It's ok to skip an import if we don't have all of the required information.
                continue;
            }

            var importSourceChecksumAttributeNode = new RazorSourceChecksumAttributeIntermediateNode(checksum, checksumAlgorithm, identifier);
            @namespace.Children.Insert(insert++, importSourceChecksumAttributeNode);
        }
    }
}
