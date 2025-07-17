// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public class RuntimeNodeWriter : IntermediateNodeWriter
{
    private const string WriteCSharpExpressionMethod = "Write";
    private const string WriteHtmlContentMethod = "WriteLiteral";
    private const string BeginWriteAttributeMethod = "BeginWriteAttribute";
    private const string EndWriteAttributeMethod = "EndWriteAttribute";
    private const string WriteAttributeValueMethod = "WriteAttributeValue";
    private const string PushWriterMethod = "PushWriter";
    private const string PopWriterMethod = "PopWriter";
    private const string TemplateTypeName = "Microsoft.AspNetCore.Mvc.Razor.HelperResult";

    private readonly string _writeCSharpExpressionMethod;
    private readonly string _writeAttributeValueMethod;

    public RuntimeNodeWriter()
        : this(WriteCSharpExpressionMethod, WriteAttributeValueMethod)
    {
    }

    protected RuntimeNodeWriter(string? writeCSharpExpressionMethod = null, string? writeAttributeValueMethod = null)
    {
        _writeCSharpExpressionMethod = writeCSharpExpressionMethod ?? WriteCSharpExpressionMethod;
        _writeAttributeValueMethod = writeAttributeValueMethod ?? WriteAttributeValueMethod;
    }

    public override void WriteUsingDirective(CodeRenderingContext context, UsingDirectiveIntermediateNode node)
    {
        var writer = context.CodeWriter;

        if (node.Source is { FilePath: not null } nodeSource)
        {
            using (context.BuildEnhancedLinePragma(nodeSource, suppressLineDefaultAndHidden: true))
            {
                writer.WriteUsing(node.Content, endLine: node.HasExplicitSemicolon);
            }

            if (!node.HasExplicitSemicolon)
            {
                writer.WriteLine(";");
            }

            if (node.AppendLineDefaultAndHidden)
            {
                writer.WriteLine("#line default");
                writer.WriteLine("#line hidden");
            }
        }
        else
        {
            writer.WriteUsing(node.Content);

            if (node.AppendLineDefaultAndHidden)
            {
                writer.WriteLine("#line default");
                writer.WriteLine("#line hidden");
            }
        }
    }

    public override void WriteCSharpExpression(CodeRenderingContext context, CSharpExpressionIntermediateNode node)
    {
        var writer = context.CodeWriter;

        writer.WriteStartMethodInvocation(_writeCSharpExpressionMethod);
        writer.WriteLine();
        WriteCSharpChildren(node.Children, context);
        writer.WriteEndMethodInvocation();
    }

    public override void WriteCSharpCode(CodeRenderingContext context, CSharpCodeIntermediateNode node)
    {
        var isWhitespaceStatement = true;

        foreach (var child in node.Children)
        {
            if (child is not CSharpIntermediateToken token ||
                !token.Content.IsNullOrWhiteSpace())
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
        var writer = context.CodeWriter;

        foreach (var child in children)
        {
            if (child is CSharpIntermediateToken token)
            {
                using (context.BuildEnhancedLinePragma(token.Source))
                {
                    writer.Write(token.Content);
                }
            }
            else
            {
                // There may be something else inside the statement like an extension node.
                context.RenderNode(child);
            }
        }
    }

    public override void WriteHtmlAttribute(CodeRenderingContext context, HtmlAttributeIntermediateNode node)
    {
        var valuePieceCount = node
            .Children
            .Count(static child => child is HtmlAttributeValueIntermediateNode or
                                            CSharpExpressionAttributeValueIntermediateNode or
                                            CSharpCodeAttributeValueIntermediateNode or
                                            ExtensionIntermediateNode);

        var writer = context.CodeWriter;
        var nodeSource = node.Source.AssumeNotNull();

        var prefixLocation = nodeSource.AbsoluteIndex;
        var suffixLocation = nodeSource.AbsoluteIndex + nodeSource.Length - node.Suffix.Length;

        writer
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

        writer
            .WriteStartMethodInvocation(EndWriteAttributeMethod)
            .WriteEndMethodInvocation();
    }

    public override void WriteHtmlAttributeValue(CodeRenderingContext context, HtmlAttributeValueIntermediateNode node)
    {
        var writer = context.CodeWriter;
        var nodeSource = node.Source.AssumeNotNull();

        var prefixLocation = nodeSource.AbsoluteIndex;
        var valueLocation = nodeSource.AbsoluteIndex + node.Prefix.Length;
        var valueLength = nodeSource.Length;

        writer
            .WriteStartMethodInvocation(_writeAttributeValueMethod)
            .WriteStringLiteral(node.Prefix)
            .WriteParameterSeparator()
            .Write(prefixLocation.ToString(CultureInfo.InvariantCulture))
            .WriteParameterSeparator();

        // Write content
        foreach (var child in node.Children)
        {
            if (child is HtmlIntermediateToken token)
            {
                writer.WriteStringLiteral(token.Content);
            }
            else
            {
                // There may be something else inside the attribute value like an extension node.
                context.RenderNode(child);
            }
        }

        writer
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
        var writer = context.CodeWriter;
        var nodeSource = node.Source.AssumeNotNull();

        var prefixLocation = nodeSource.AbsoluteIndex.ToString(CultureInfo.InvariantCulture);

        writer
            .WriteStartMethodInvocation(_writeAttributeValueMethod)
            .WriteStringLiteral(node.Prefix)
            .WriteParameterSeparator()
            .Write(prefixLocation)
            .WriteParameterSeparator();

        WriteCSharpChildren(node.Children, context);

        var valueLocation = node.Source.Value.AbsoluteIndex + node.Prefix.Length;
        var valueLength = node.Source.Value.Length - node.Prefix.Length;
        writer
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

        var writer = context.CodeWriter;
        var nodeSource = node.Source.AssumeNotNull();

        var prefixLocation = nodeSource.AbsoluteIndex;
        var valueLocation = nodeSource.AbsoluteIndex + node.Prefix.Length;
        var valueLength = nodeSource.Length - node.Prefix.Length;

        writer
            .WriteStartMethodInvocation(_writeAttributeValueMethod)
            .WriteStringLiteral(node.Prefix)
            .WriteParameterSeparator()
            .Write(prefixLocation.ToString(CultureInfo.InvariantCulture))
            .WriteParameterSeparator();

        writer.WriteStartNewObject(TemplateTypeName);

        using (writer.BuildAsyncLambda(ValueWriterName))
        {
            BeginWriterScope(context, ValueWriterName);
            WriteCSharpChildren(node.Children, context);
            EndWriterScope(context);
        }

        writer.WriteEndMethodInvocation(false);

        writer
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
    internal static void WriteHtmlLiteral(CodeRenderingContext context, int maxStringLiteralLength, Content literal)
    {
        if (literal.Length == 0)
        {
            return;
        }

        using var parts = new PooledArrayBuilder<ReadOnlyMemory<char>>();
        literal.CollectAllParts(ref parts.AsRef());

        WriteHtmlLiteral(context, maxStringLiteralLength, literal.Length, ref parts.AsRef());
    }

    private static void WriteHtmlLiteral(
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

        static void WriteLiteral(CodeRenderingContext context, ref readonly PooledArrayBuilder<ReadOnlyMemory<char>> parts)
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
