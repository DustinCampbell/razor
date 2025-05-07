// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public static class SyntaxNodeSerializer
{
    internal static string Serialize(SyntaxNode node, bool validateSpanEditHandlers)
    {
        using (var writer = new StringWriter())
        {
            var walker = new Walker(writer, validateSpanEditHandlers);
            walker.Visit(node);

            return writer.ToString();
        }
    }

    private sealed class Walker(TextWriter writer, bool validateSpanEditHandlers) : SyntaxNodeWriter(writer, validateSpanEditHandlers)
    {
        private readonly SyntaxNodeWriter _visitor = new(writer, validateSpanEditHandlers);
        private readonly TextWriter _writer = writer;

        public override void Visit(SyntaxNode? node)
        {
            if (node == null)
            {
                return;
            }

            if (node.IsList)
            {
                base.DefaultVisit(node);
                return;
            }

            _visitor.Visit(node);
            _writer.WriteLine();

            _visitor.Depth++;
            base.DefaultVisit(node);
            _visitor.Depth--;
        }

        public override void VisitToken(SyntaxToken token)
        {
            _visitor.VisitToken(token);
            _writer.WriteLine();
        }
    }
}
