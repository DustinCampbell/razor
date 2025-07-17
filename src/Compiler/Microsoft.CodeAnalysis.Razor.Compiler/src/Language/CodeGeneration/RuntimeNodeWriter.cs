// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public class RuntimeNodeWriter : IntermediateNodeWriter
{
    public virtual string WriteCSharpExpressionMethod { get; set; } = "Write";

    public virtual string WriteHtmlContentMethod { get; set; } = "WriteLiteral";

    public virtual string BeginWriteAttributeMethod { get; set; } = "BeginWriteAttribute";

    public virtual string EndWriteAttributeMethod { get; set; } = "EndWriteAttribute";

    public virtual string WriteAttributeValueMethod { get; set; } = "WriteAttributeValue";

    public virtual string PushWriterMethod { get; set; } = "PushWriter";

    public virtual string PopWriterMethod { get; set; } = "PopWriter";

    public string TemplateTypeName { get; set; } = "Microsoft.AspNetCore.Mvc.Razor.HelperResult";

    public override void WriteUsingDirective(CodeRenderingContext context, UsingDirectiveIntermediateNode node)
    {
        if (node.Source is { FilePath: not null } sourceSpan)
        {
            using (context.BuildEnhancedLinePragma(sourceSpan, suppressLineDefaultAndHidden: true))
            {
                context.CodeWriter.WriteUsing(node.Content, endLine: node.HasExplicitSemicolon);
            }
            if (!node.HasExplicitSemicolon)
            {
                context.CodeWriter.WriteLine(";");
            }
            if (node.AppendLineDefaultAndHidden)
            {
                context.CodeWriter.WriteLine("#line default");
                context.CodeWriter.WriteLine("#line hidden");
            }
        }
        else
        {
            context.CodeWriter.WriteUsing(node.Content);

            if (node.AppendLineDefaultAndHidden)
            {
                context.CodeWriter.WriteLine("#line default");
                context.CodeWriter.WriteLine("#line hidden");
            }
        }
    }

    public override void WriteCSharpExpression(CodeRenderingContext context, CSharpExpressionIntermediateNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        context.CodeWriter.WriteStartMethodInvocation(WriteCSharpExpressionMethod);
        context.CodeWriter.WriteLine();
        WriteCSharpChildren(node.Children, context);
        context.CodeWriter.WriteEndMethodInvocation();
    }

    public override void WriteCSharpCode(CodeRenderingContext context, CSharpCodeIntermediateNode node)
    {
        var isWhitespaceStatement = true;
        for (var i = 0; i < node.Children.Count; i++)
        {
            var token = node.Children[i] as IntermediateToken;
            if (token == null || !string.IsNullOrWhiteSpace(token.Content))
            {
                isWhitespaceStatement = false;
                break;
            }
        }

        if (isWhitespaceStatement)
        {
            return;
        }

        WriteCSharpChildren(node.Children, context);
        context.CodeWriter.WriteLine();
    }

    private static void WriteCSharpChildren(IntermediateNodeCollection children, CodeRenderingContext context)
    {
        for (var i = 0; i < children.Count; i++)
        {
            if (children[i] is CSharpIntermediateToken token)
            {
                using (context.BuildEnhancedLinePragma(token.Source))
                {
                    context.CodeWriter.Write(token.Content);
                }
            }
            else
            {
                // There may be something else inside the statement like an extension node.
                context.RenderNode(children[i]);
            }
        }
    }

    public override void WriteHtmlAttribute(CodeRenderingContext context, HtmlAttributeIntermediateNode node)
    {
        var valuePieceCount = node
            .Children
            .Count(child =>
                child is HtmlAttributeValueIntermediateNode ||
                child is CSharpExpressionAttributeValueIntermediateNode ||
                child is CSharpCodeAttributeValueIntermediateNode ||
                child is ExtensionIntermediateNode);

        var prefixLocation = node.Source.Value.AbsoluteIndex;
        var suffixLocation = node.Source.Value.AbsoluteIndex + node.Source.Value.Length - node.Suffix.Length;
        context.CodeWriter
            .WriteStartMethodInvocation(BeginWriteAttributeMethod)
            .WriteStringLiteral(node.AttributeName)
            .WriteParameterSeparator()
            .WriteStringLiteral(node.Prefix)
            .WriteParameterSeparator()
            .Write(prefixLocation.ToString(CultureInfo.InvariantCulture))
            .WriteParameterSeparator()
            .WriteStringLiteral(node.Suffix)
            .WriteParameterSeparator()
            .Write(suffixLocation.ToString(CultureInfo.InvariantCulture))
            .WriteParameterSeparator()
            .Write(valuePieceCount.ToString(CultureInfo.InvariantCulture))
            .WriteEndMethodInvocation();

        context.RenderChildren(node);

        context.CodeWriter
            .WriteStartMethodInvocation(EndWriteAttributeMethod)
            .WriteEndMethodInvocation();
    }

    public override void WriteHtmlAttributeValue(CodeRenderingContext context, HtmlAttributeValueIntermediateNode node)
    {
        var prefixLocation = node.Source.Value.AbsoluteIndex;
        var valueLocation = node.Source.Value.AbsoluteIndex + node.Prefix.Length;
        var valueLength = node.Source.Value.Length;
        context.CodeWriter
            .WriteStartMethodInvocation(WriteAttributeValueMethod)
            .WriteStringLiteral(node.Prefix)
            .WriteParameterSeparator()
            .Write(prefixLocation.ToString(CultureInfo.InvariantCulture))
            .WriteParameterSeparator();

        // Write content
        for (var i = 0; i < node.Children.Count; i++)
        {
            if (node.Children[i] is HtmlIntermediateToken token)
            {
                context.CodeWriter.WriteStringLiteral(token.Content);
            }
            else
            {
                // There may be something else inside the attribute value like an extension node.
                context.RenderNode(node.Children[i]);
            }
        }

        context.CodeWriter
            .WriteParameterSeparator()
            .Write(valueLocation.ToString(CultureInfo.InvariantCulture))
            .WriteParameterSeparator()
            .Write(valueLength.ToString(CultureInfo.InvariantCulture))
            .WriteParameterSeparator()
            .WriteBooleanLiteral(true)
            .WriteEndMethodInvocation();
    }

    public override void WriteCSharpExpressionAttributeValue(CodeRenderingContext context, CSharpExpressionAttributeValueIntermediateNode node)
    {
        var prefixLocation = node.Source.Value.AbsoluteIndex.ToString(CultureInfo.InvariantCulture);
        context.CodeWriter
            .WriteStartMethodInvocation(WriteAttributeValueMethod)
            .WriteStringLiteral(node.Prefix)
            .WriteParameterSeparator()
            .Write(prefixLocation)
            .WriteParameterSeparator();

        WriteCSharpChildren(node.Children, context);

        var valueLocation = node.Source.Value.AbsoluteIndex + node.Prefix.Length;
        var valueLength = node.Source.Value.Length - node.Prefix.Length;
        context.CodeWriter
            .WriteParameterSeparator()
            .Write(valueLocation.ToString(CultureInfo.InvariantCulture))
            .WriteParameterSeparator()
            .Write(valueLength.ToString(CultureInfo.InvariantCulture))
            .WriteParameterSeparator()
            .WriteBooleanLiteral(false)
            .WriteEndMethodInvocation();
    }

    public override void WriteCSharpCodeAttributeValue(CodeRenderingContext context, CSharpCodeAttributeValueIntermediateNode node)
    {
        const string ValueWriterName = "__razor_attribute_value_writer";

        var prefixLocation = node.Source.Value.AbsoluteIndex;
        var valueLocation = node.Source.Value.AbsoluteIndex + node.Prefix.Length;
        var valueLength = node.Source.Value.Length - node.Prefix.Length;
        context.CodeWriter
            .WriteStartMethodInvocation(WriteAttributeValueMethod)
            .WriteStringLiteral(node.Prefix)
            .WriteParameterSeparator()
            .Write(prefixLocation.ToString(CultureInfo.InvariantCulture))
            .WriteParameterSeparator();

        context.CodeWriter.WriteStartNewObject(TemplateTypeName);

        using (context.CodeWriter.BuildAsyncLambda(ValueWriterName))
        {
            BeginWriterScope(context, ValueWriterName);
            WriteCSharpChildren(node.Children, context);
            EndWriterScope(context);
        }

        context.CodeWriter.WriteEndMethodInvocation(false);

        context.CodeWriter
            .WriteParameterSeparator()
            .Write(valueLocation.ToString(CultureInfo.InvariantCulture))
            .WriteParameterSeparator()
            .Write(valueLength.ToString(CultureInfo.InvariantCulture))
            .WriteParameterSeparator()
            .WriteBooleanLiteral(false)
            .WriteEndMethodInvocation();
    }

    public override void WriteHtmlContent(CodeRenderingContext context, HtmlContentIntermediateNode node)
    {
        const int MaxStringLiteralLength = 1024;

        using var contentParts = new PooledArrayBuilder<ReadOnlyMemory<char>>();

        var contentLength = 0;

        foreach (var child in node.Children)
        {
            if (child is HtmlIntermediateToken token)
            {
                var content = token.Content.AsMemory();

                contentParts.Add(content);
                contentLength += content.Length;
            }
        }

        WriteHtmlLiteral(context, MaxStringLiteralLength, contentLength, ref contentParts.AsRef());
    }

    // Internal for testing
    internal void WriteHtmlLiteral(CodeRenderingContext context, int maxStringLiteralLength, Content literal)
    {
        if (literal.Length == 0)
        {
            return;
        }

        using var parts = new PooledArrayBuilder<ReadOnlyMemory<char>>();
        literal.CollectAllParts(ref parts.AsRef());

        WriteHtmlLiteral(context, maxStringLiteralLength, literal.Length, ref parts.AsRef());
    }

    private void WriteHtmlLiteral(
        CodeRenderingContext context,
        int maxStringLiteralLength,
        int contentLength,
        ref readonly PooledArrayBuilder<ReadOnlyMemory<char>> contentParts)
    {
        Debug.Assert(maxStringLiteralLength > 0);
        Debug.Assert(contentLength == contentParts.Sum(static p => p.Length), "Length must match the sum of the parts' lengths.");

        if (contentLength == 0)
        {
            return;
        }

        if (contentLength <= maxStringLiteralLength)
        {
            WriteLiteral(context, in contentParts);
            return;
        }

        // Content is too large. Render it in pieces to avoid Roslyn OOM exceptions
        // at compile time: https://github.com/aspnet/External/issues/54

        using var chunkParts = new PooledArrayBuilder<ReadOnlyMemory<char>>();
        ref var chunkPartsRef = ref chunkParts.AsRef();

        var remainingPart = ReadOnlyMemory<char>.Empty;
        var enumerator = contentParts.GetEnumerator();

        while (contentLength > 0)
        {
            var chunkSize = Math.Min(contentLength, maxStringLiteralLength);
            var charsToWrite = chunkSize;

            // First copy any chars remaining from last time to this chunk.
            if (!remainingPart.IsEmpty)
            {
                var toCopy = Math.Min(remainingPart.Length, charsToWrite);
                chunkParts.Add(remainingPart[..toCopy]);
                remainingPart = remainingPart[toCopy..];

                charsToWrite -= toCopy;
            }

            // If there isn't anything more else left over, add parts until there
            // are no more chars to write.
            if (remainingPart.IsEmpty)
            {
                while (charsToWrite > 0 && enumerator.MoveNext())
                {
                    var part = enumerator.Current;

                    if (part.IsEmpty)
                    {
                        continue;
                    }

                    if (part.Length <= charsToWrite)
                    {
                        chunkParts.Add(part);
                        charsToWrite -= part.Length;
                    }
                    else
                    {
                        // If the part is larger than the remaining chars to write, split it.
                        var partialPart = part[..charsToWrite];
                        chunkParts.Add(part[..charsToWrite]);
                        remainingPart = part[charsToWrite..];

                        // Note: This should set charsToWrite to zero.
                        charsToWrite -= partialPart.Length;
                    }
                }
            }

            Debug.Assert(charsToWrite == 0, "Ran out of content parts but expected to write more characters.");
            Debug.Assert(chunkParts.Count > 0);

            var lastChar = chunkParts[^1].Span[^1];

            if (char.IsHighSurrogate(lastChar))
            {
                // If character at splitting point is a high surrogate, take one less character this iteration
                // as we're attempting to split a surrogate pair. This can happen when something like an
                // emoji sits on the barrier between splits; if we were to split the emoji we'd end up with
                // invalid bytes in our output.

                var lastPart = chunkParts[^1];
                chunkPartsRef[^1] = lastPart[..^1];

                WriteLiteral(context, in chunkParts);
                chunkParts.Clear();

                chunkParts.Add(lastPart[^1..]);
            }
            else
            {
                WriteLiteral(context, in chunkParts);
                chunkParts.Clear();
            }

            contentLength -= chunkSize;
        }

        Debug.Assert(
            contentLength == 0 && remainingPart.IsEmpty && enumerator.MoveNext() == false,
            "More content remains to be written.");

        if (chunkParts.Count > 0)
        {
            // This would only occur if the very last character was a high surrogate.
            // That's a little weird, but we should handle it.
            Debug.Assert(chunkParts.Count == 1 && chunkParts[0].Length == 1);
            WriteLiteral(context, in chunkParts);
        }

        void WriteLiteral(CodeRenderingContext context, ref readonly PooledArrayBuilder<ReadOnlyMemory<char>> parts)
        {
            context.CodeWriter
                .WriteStartMethodInvocation(WriteHtmlContentMethod)
                .WriteStringLiteral(in parts)
                .WriteEndMethodInvocation();
        }
    }

    public override void BeginWriterScope(CodeRenderingContext context, string writer)
    {
        context.CodeWriter.WriteMethodInvocation(PushWriterMethod, writer);
    }

    public override void EndWriterScope(CodeRenderingContext context)
    {
        context.CodeWriter.WriteMethodInvocation(PopWriterMethod);
    }
}
