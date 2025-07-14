// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

internal static partial class CodeWriterExtensions
{
    public static bool IsAtBeginningOfLine(this CodeWriter writer)
    {
        return writer.LastChar is '\n';
    }

    public static void EnsureNewLine(this CodeWriter writer)
    {
        if (!IsAtBeginningOfLine(writer))
        {
            writer.WriteLine();
        }
    }

    public static CodeWriter WriteVariableDeclaration(this CodeWriter writer, Content typeName, Content name, Content value = default)
    {
        return !value.IsEmpty
            ? writer.WriteLine($"{typeName} {name} = {value};")
            : writer.WriteLine($"{typeName} {name} = null;");
    }

    public static CodeWriter WriteStartAssignment(this CodeWriter writer, Content name)
        => writer.Write($"{name} = ");

    public static CodeWriter WriteParameterSeparator(this CodeWriter writer)
    {
        return writer.Write(", ");
    }

    public static CodeWriter WriteStartNewObject(this CodeWriter writer, Content typeName)
    {
        return writer.Write($"new {typeName}(");
    }

    public static CodeWriter WriteUsing(this CodeWriter writer, Content name, bool endLine = true)
    {
        writer.Write($"using {name}");

        if (endLine)
        {
            writer.WriteLine(";");
        }

        return writer;
    }

    public static CodeWriter WriteEnhancedLineNumberDirective(this CodeWriter writer, SourceSpan span, int characterOffset, bool ensurePathBackSlashes)
    {
        // All values here need to be offset by 1 since #line uses a 1-indexed numbering system.
        var lineNumberAsString = (span.LineIndex + 1).ToString(CultureInfo.InvariantCulture);
        var characterStartAsString = (span.CharacterIndex + 1).ToString(CultureInfo.InvariantCulture);
        var lineEndAsString = (span.LineIndex + 1 + span.LineCount).ToString(CultureInfo.InvariantCulture);
        var characterEndAsString = (span.EndCharacterIndex + 1).ToString(CultureInfo.InvariantCulture);
        writer.Write("#line (")
            .Write(lineNumberAsString)
            .Write(",")
            .Write(characterStartAsString)
            .Write(")-(")
            .Write(lineEndAsString)
            .Write(",")
            .Write(characterEndAsString)
            .Write(") ");

        // an offset of zero is indicated by its absence.
        if (characterOffset != 0)
        {
            var characterOffsetAsString = characterOffset.ToString(CultureInfo.InvariantCulture);
            writer.Write(characterOffsetAsString).Write(" ");
        }

        return writer.Write("\"").WriteFilePath(span.FilePath, ensurePathBackSlashes).WriteLine("\"");
    }

    public static CodeWriter WriteLineNumberDirective(this CodeWriter writer, SourceSpan span, bool ensurePathBackslashes)
    {
        if (writer.Length >= writer.NewLine.Length && !IsAtBeginningOfLine(writer))
        {
            writer.WriteLine();
        }

        var lineNumberAsString = (span.LineIndex + 1).ToString(CultureInfo.InvariantCulture);
        return writer.Write("#line ").Write(lineNumberAsString).Write(" \"").WriteFilePath(span.FilePath, ensurePathBackslashes).WriteLine("\"");
    }

    private static CodeWriter WriteFilePath(this CodeWriter writer, string filePath, bool ensurePathBackSlashes)
    {
        if (!ensurePathBackSlashes)
        {
            return writer.Write(filePath);
        }

        // ISSUE: https://github.com/dotnet/razor/issues/9108
        // The razor tooling normalizes paths to be forward slash based, regardless of OS.
        // If you try and use the line pragma in the design time docs to map back to the original file it will fail,
        // as the path isn't actually valid on windows. As a workaround we apply a simple heuristic to switch the
        // paths back when writing out the design time paths.
        var filePathMemory = filePath.AsMemory();
        var forwardSlashIndex = filePathMemory.Span.IndexOf('/');
        while (forwardSlashIndex >= 0)
        {
            writer.Write(filePathMemory[..forwardSlashIndex]);
            writer.Write("\\");

            filePathMemory = filePathMemory[(forwardSlashIndex + 1)..];
            forwardSlashIndex = filePathMemory.Span.IndexOf('/');
        }

        writer.Write(filePathMemory);

        return writer;
    }

    public static CodeWriter WriteStartMethodInvocation(this CodeWriter writer, Content methodName)
    {
        return writer.Write($"{methodName}(");
    }

    public static CodeWriter WriteEndMethodInvocation(this CodeWriter writer, bool endLine = true)
    {
        writer.Write(")");

        if (endLine)
        {
            writer.WriteLine(";");
        }

        return writer;
    }

    // Writes a method invocation for the given instance name.
    public static CodeWriter WriteInstanceMethodInvocation(
        this CodeWriter writer,
        Content instanceName,
        Content methodName,
        params ImmutableArray<Content> parameters)
    {
        return writer.WriteInstanceMethodInvocation(instanceName, methodName, endLine: true, parameters);
    }

    // Writes a method invocation for the given instance name.
    public static CodeWriter WriteInstanceMethodInvocation(
        this CodeWriter writer,
        Content instanceName,
        Content methodName,
        bool endLine,
        params ImmutableArray<Content> parameters)
    {
        return writer.WriteMethodInvocation(
            new($"{instanceName}.{methodName}"),
            endLine,
            parameters);
    }

    public static CodeWriter WriteStartInstanceMethodInvocation(this CodeWriter writer, Content instanceName, Content methodName)
    {
        return writer.WriteStartMethodInvocation(new($"{instanceName}.{methodName}"));
    }

    public static CodeWriter WriteField(this CodeWriter writer, IList<string> suppressWarnings, IList<string> modifiers, string typeName, string fieldName)
    {
        if (suppressWarnings == null)
        {
            throw new ArgumentNullException(nameof(suppressWarnings));
        }

        if (modifiers == null)
        {
            throw new ArgumentNullException(nameof(modifiers));
        }

        if (typeName == null)
        {
            throw new ArgumentNullException(nameof(typeName));
        }

        if (fieldName == null)
        {
            throw new ArgumentNullException(nameof(fieldName));
        }

        for (var i = 0; i < suppressWarnings.Count; i++)
        {
            writer.Write("#pragma warning disable ");
            writer.WriteLine(suppressWarnings[i]);
        }

        for (var i = 0; i < modifiers.Count; i++)
        {
            writer.Write(modifiers[i]);
            writer.Write(" ");
        }

        writer.Write(typeName);
        writer.Write(" ");
        writer.Write(fieldName);
        writer.Write(";");
        writer.WriteLine();

        for (var i = suppressWarnings.Count - 1; i >= 0; i--)
        {
            writer.Write("#pragma warning restore ");
            writer.WriteLine(suppressWarnings[i]);
        }

        return writer;
    }

    public static CodeWriter WriteMethodInvocation(this CodeWriter writer, Content methodName, params ImmutableArray<Content> parameters)
    {
        return WriteMethodInvocation(writer, methodName, endLine: true, parameters);
    }

    public static CodeWriter WriteMethodInvocation(this CodeWriter writer, Content methodName, bool endLine, params ImmutableArray<Content> parameters)
    {
        return
            WriteStartMethodInvocation(writer, methodName)
            .Write(Content.Join(", ", parameters))
            .WriteEndMethodInvocation(endLine);
    }

    public static CodeWriter WritePropertyDeclaration(this CodeWriter writer, IList<string> modifiers, IntermediateToken type, string propertyName, string propertyExpression, CodeRenderingContext context)
    {
        WritePropertyDeclarationPreamble(writer, modifiers, type.Content, propertyName, type.Source, propertySpan: null, context);
        writer.Write(" => ");
        writer.Write(propertyExpression);
        writer.WriteLine(";");
        return writer;
    }

    public static CodeWriter WriteAutoPropertyDeclaration(this CodeWriter writer, IList<string> modifiers, string typeName, string propertyName, SourceSpan? typeSpan = null, SourceSpan? propertySpan = null, CodeRenderingContext context = null, bool privateSetter = false, bool defaultValue = false)
    {
        ArgHelper.ThrowIfNull(modifiers);
        ArgHelper.ThrowIfNull(typeName);
        ArgHelper.ThrowIfNull(propertyName);

        WritePropertyDeclarationPreamble(writer, modifiers, typeName, propertyName, typeSpan, propertySpan, context);

        writer.Write(" { get;");
        if (privateSetter)
        {
            writer.Write(" private");
        }
        writer.Write(" set; }");
        writer.WriteLine();

        if (defaultValue && context?.Options.SuppressNullabilityEnforcement == false && context?.Options.DesignTime == false)
        {
            writer.WriteLine(" = default!;");
        }

        return writer;
    }

    private static void WritePropertyDeclarationPreamble(CodeWriter writer, IList<string> modifiers, string typeName, string propertyName, SourceSpan? typeSpan, SourceSpan? propertySpan, CodeRenderingContext context)
    {
        for (var i = 0; i < modifiers.Count; i++)
        {
            writer.Write(modifiers[i]);
            writer.Write(" ");
        }

        WriteToken(writer, typeName, typeSpan, context);
        writer.Write(" ");
        WriteToken(writer, propertyName, propertySpan, context);

        static void WriteToken(CodeWriter writer, string content, SourceSpan? span, CodeRenderingContext context)
        {
            if (span is not null && context?.Options.DesignTime == false)
            {
                using (context.BuildEnhancedLinePragma(span))
                {
                    writer.Write(content);
                }
            }
            else
            {
                writer.Write(content);
            }
        }
    }

    /// <summary>
    /// Writes an "@" character if the provided identifier needs escaping in c#
    /// </summary>
    public static CodeWriter WriteIdentifierEscapeIfNeeded(this CodeWriter writer, string identifier)
    {
        if (IdentifierRequiresEscaping(identifier))
        {
            writer.Write("@");
        }
        return writer;
    }

    public static bool IdentifierRequiresEscaping(this string identifier)
    {
        return CodeAnalysis.CSharp.SyntaxFacts.GetKeywordKind(identifier) != CodeAnalysis.CSharp.SyntaxKind.None ||
            CodeAnalysis.CSharp.SyntaxFacts.GetContextualKeywordKind(identifier) != CodeAnalysis.CSharp.SyntaxKind.None;
    }

    public static CSharpCodeWritingScope BuildScope(this CodeWriter writer)
    {
        return new CSharpCodeWritingScope(writer);
    }

    public static CSharpCodeWritingScope BuildLambda(this CodeWriter writer, params string[] parameterNames)
    {
        return BuildLambda(writer, async: false, parameterNames: parameterNames);
    }

    public static CSharpCodeWritingScope BuildAsyncLambda(this CodeWriter writer, params string[] parameterNames)
    {
        return BuildLambda(writer, async: true, parameterNames: parameterNames);
    }

    private static CSharpCodeWritingScope BuildLambda(CodeWriter writer, bool async, string[] parameterNames)
    {
        if (async)
        {
            writer.Write("async");
        }

        writer.Write("(").Write(string.Join(", ", parameterNames)).Write(") => ");

        var scope = new CSharpCodeWritingScope(writer);

        return scope;
    }

#nullable enable
    public static CSharpCodeWritingScope BuildNamespace(this CodeWriter writer, Content name, SourceSpan? span, CodeRenderingContext context)
    {
        if (name.IsEmpty)
        {
            return new CSharpCodeWritingScope(writer, writeBraces: false);
        }

        writer.Write("namespace ");

        if (context.Options.DesignTime || span is null)
        {
            writer.WriteLine(name);
        }
        else
        {
            writer.WriteLine();
            using (context.BuildEnhancedLinePragma(span))
            {
                writer.WriteLine(name);
            }
        }
        return new CSharpCodeWritingScope(writer);
    }
#nullable disable

    public static CSharpCodeWritingScope BuildClassDeclaration(
        this CodeWriter writer,
        ImmutableArray<Content> modifiers,
        Content name,
        BaseTypeWithModel baseType,
        ImmutableArray<IntermediateToken> interfaces,
        ImmutableArray<TypeParameter> typeParameters,
        CodeRenderingContext context,
        bool useNullableContext = false)
    {
        Debug.Assert(context == null || context.CodeWriter == writer);

        if (useNullableContext)
        {
            writer.WriteLine("#nullable restore");
        }

        if (!modifiers.IsDefaultOrEmpty)
        {
            foreach (var modifier in modifiers)
            {
                writer.Write($"{modifier} ");
            }
        }

        writer.Write($"class {name}");

        if (!typeParameters.IsDefaultOrEmpty)
        {
            writer.Write("<");

            var first = true;

            foreach (var typeParameter in typeParameters)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    writer.Write(",");
                }

                if (typeParameter.NameSource is { } source)
                {
                    WriteWithPragma(writer, typeParameter.Name, context, source);
                }
                else
                {
                    writer.Write(typeParameter.Name);
                }
            }

            writer.Write(">");
        }

        var hasBaseType = !string.IsNullOrWhiteSpace(baseType?.BaseType.Content);
        var hasInterfaces = !interfaces.IsDefaultOrEmpty;

        if (hasBaseType || hasInterfaces)
        {
            writer.Write(" : ");

            if (hasBaseType)
            {
                WriteToken(baseType.BaseType);
                WriteOptionalToken(baseType.GreaterThan);
                WriteOptionalToken(baseType.ModelType);
                WriteOptionalToken(baseType.LessThan);

                if (hasInterfaces)
                {
                    writer.WriteParameterSeparator();
                }
            }

            if (hasInterfaces)
            {
                WriteToken(interfaces[0]);

                for (var i = 1; i < interfaces.Length; i++)
                {
                    writer.WriteParameterSeparator();
                    WriteToken(interfaces[i]);
                }
            }
        }

        writer.WriteLine();
        if (typeParameters != null)
        {
            foreach (var typeParameter in typeParameters)
            {
                var constraint = typeParameter.Constraints;
                if (constraint != null)
                {
                    if (typeParameter.ConstraintsSource is { } source)
                    {
                        Debug.Assert(context != null);
                        WriteWithPragma(writer, constraint, context, source);
                    }
                    else
                    {
                        writer.Write(constraint);
                        writer.WriteLine();
                    }
                }
            }
        }

        if (useNullableContext)
        {
            writer.WriteLine("#nullable disable");
        }

        return new CSharpCodeWritingScope(writer);

        void WriteOptionalToken(IntermediateToken token)
        {
            if (token is not null)
            {
                WriteToken(token);
            }
        }

        void WriteToken(IntermediateToken token)
        {
            if (token.Source is { } source)
            {
                WriteWithPragma(writer, token.Content, context, source);
            }
            else
            {
                writer.Write(token.Content);
            }
        }

        static void WriteWithPragma(CodeWriter writer, Content content, CodeRenderingContext context, SourceSpan source)
        {
            if (context.Options.DesignTime)
            {
                using (context.BuildLinePragma(source))
                {
                    context.AddSourceMappingFor(source);
                    writer.Write(content);
                }
            }
            else
            {
                using (context.BuildEnhancedLinePragma(source))
                {
                    writer.Write(content);
                }
            }
        }
    }

    public static CodeWriter WriteCommaSeparatedList(this CodeWriter writer, ImmutableArray<Content> list)
    {
        if (list.IsEmpty)
        {
            return writer;
        }

        writer.Write(list[0]);

        if (list.Length > 1)
        {
            for (var i = 1; i < list.Length; i++)
            {
                writer.Write($", {list[i]}");
            }
        }

        return writer;
    }

    public static CodeWriter WriteCommaSeparatedList<T>(this CodeWriter writer, ImmutableArray<T> list, Func<T, Content> contentSelector)
    {
        if (list.IsEmpty)
        {
            return writer;
        }

        writer.Write(contentSelector(list[0]));

        if (list.Length > 1)
        {
            for (var i = 1; i < list.Length; i++)
            {
                writer.Write($", {contentSelector(list[i])}");
            }
        }

        return writer;
    }

    public static CSharpCodeWritingScope BuildMethodDeclaration(
        this CodeWriter writer,
        Content accessibility,
        Content returnType,
        Content name,
        params ImmutableArray<(Content type, Content name)> parameters)
    {
        writer
            .Write($"{accessibility} {returnType} {name}(")
            .WriteCommaSeparatedList(parameters, p => new($"{p.type} {p.name}"))
            .WriteLine(")");

        return new CSharpCodeWritingScope(writer);
    }

    public struct CSharpCodeWritingScope : IDisposable
    {
        private readonly CodeWriter _writer;
        private readonly bool _autoSpace;
        private readonly bool _writeBraces;
        private readonly int _tabSize;
        private int _startIndent;

        public CSharpCodeWritingScope(CodeWriter writer, bool autoSpace = true, bool writeBraces = true)
        {
            _writer = writer;
            _autoSpace = autoSpace;
            _writeBraces = writeBraces;
            _tabSize = writer.TabSize;
            _startIndent = -1; // Set in WriteStartScope

            WriteStartScope();
        }

        public void Dispose()
        {
            WriteEndScope();
        }

        private void WriteStartScope()
        {
            TryAutoSpace(" ");

            if (_writeBraces)
            {
                _writer.WriteLine("{");
            }
            else
            {
                _writer.WriteLine();
            }

            _writer.CurrentIndent += _tabSize;
            _startIndent = _writer.CurrentIndent;
        }

        private void WriteEndScope()
        {
            TryAutoSpace(_writer.NewLine);

            // Ensure the scope hasn't been modified
            if (_writer.CurrentIndent == _startIndent)
            {
                _writer.CurrentIndent -= _tabSize;
            }

            if (_writeBraces)
            {
                _writer.WriteLine("}");
            }
            else
            {
                _writer.WriteLine();
            }
        }

        private void TryAutoSpace(string spaceCharacter)
        {
            if (_autoSpace &&
                _writer.LastChar is char ch &&
                !char.IsWhiteSpace(ch))
            {
                _writer.Write(spaceCharacter);
            }
        }
    }
}
