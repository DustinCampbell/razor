﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using MessagePack;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.CodeAnalysis.Razor.Serialization.MessagePack.Formatters.TagHelpers;

internal sealed class DocumentationObjectFormatter : ValueFormatter<DocumentationObject>
{
    private enum DocumentationKind : byte
    {
        Descriptor,
        String
    }

    private enum ArgKind : byte
    {
        Integer,
        String,
        Boolean
    }

    public static readonly ValueFormatter<DocumentationObject> Instance = new DocumentationObjectFormatter();

    private DocumentationObjectFormatter()
    {
    }

    public override DocumentationObject Deserialize(ref MessagePackReader reader, SerializerCachingOptions options)
    {
        if (reader.TryReadNil())
        {
            return default;
        }

        var count = reader.ReadArrayHeader();

        var documentationKind = (DocumentationKind)reader.ReadByte();

        switch (documentationKind)
        {
            case DocumentationKind.Descriptor:
                var id = (DocumentationId)reader.ReadInt32();

                count -= 2;

                // Note: Each argument is stored as two values.
                var args = count > 0
                    ? ReadArgs(ref reader, count / 2, options)
                    : [];

                return DocumentationDescriptor.From(id, args);

            case DocumentationKind.String:
                if (count != 2)
                {
                    throw new MessagePackSerializationException($"Expected array of 2 elements for string documentation, but it was {count}.");
                }

                return CachedStringFormatter.Instance.Deserialize(ref reader, options).AssumeNotNull();

            default:
                throw new NotSupportedException(SR.FormatUnsupported_argument_kind(documentationKind));
        }

        static object?[] ReadArgs(ref MessagePackReader reader, int count, SerializerCachingOptions options)
        {
            using var builder = new PooledArrayBuilder<object?>(capacity: count);

            for (var i = 0; i < count; i++)
            {
                var item = ReadArg(ref reader, options);
                builder.Add(item);
            }

            return builder.ToArray();
        }

        static object? ReadArg(ref MessagePackReader reader, SerializerCachingOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var argKind = (ArgKind)reader.ReadByte();

            return argKind switch
            {
                ArgKind.String => CachedStringFormatter.Instance.Deserialize(ref reader, options),
                ArgKind.Integer => reader.ReadInt32(),
                ArgKind.Boolean => reader.ReadBoolean(),

                _ => throw new NotSupportedException(SR.FormatUnsupported_argument_kind(argKind)),
            };
        }
    }

    public override void Serialize(ref MessagePackWriter writer, DocumentationObject value, SerializerCachingOptions options)
    {
        switch (value.Object)
        {
            case DocumentationDescriptor descriptor:
                var args = descriptor.Args;
                var count = 2 + (args.Length * 2);

                writer.WriteArrayHeader(count);

                writer.Write((byte)DocumentationKind.Descriptor);
                writer.Write((int)descriptor.Id);

                foreach (var arg in args)
                {
                    WriteArg(ref writer, arg, options);
                }

                break;

            case string text:
                writer.WriteArrayHeader(2);
                writer.Write((byte)DocumentationKind.String);
                CachedStringFormatter.Instance.Serialize(ref writer, text, options);
                break;

            case null:
                writer.WriteNil();
                break;

            default:
                Debug.Fail($"Documentation objects should only be of type {nameof(DocumentationDescriptor)}, string, or null.");
                break;
        }

        static void WriteArg(ref MessagePackWriter writer, object? value, SerializerCachingOptions options)
        {
            switch (value)
            {
                case string s:
                    writer.Write((byte)ArgKind.String);
                    CachedStringFormatter.Instance.Serialize(ref writer, s, options);
                    break;

                case null:
                    writer.Write((byte)ArgKind.String);
                    writer.WriteNil();
                    break;

                case int i:
                    writer.Write((byte)ArgKind.Integer);
                    writer.Write(i);
                    break;

                case bool b:
                    writer.Write((byte)ArgKind.Boolean);
                    writer.Write(b);
                    break;

                case var arg:
                    ThrowNotSupported(arg.GetType());
                    break;
            }

            [DoesNotReturn]
            static void ThrowNotSupported(Type type)
            {
                throw new NotSupportedException(
                    SR.FormatUnsupported_argument_type(type.FullName));
            }
        }
    }

    public override void Skim(ref MessagePackReader reader, SerializerCachingOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        var count = reader.ReadArrayHeader();

        var documentationKind = (DocumentationKind)reader.ReadByte();

        switch (documentationKind)
        {
            case DocumentationKind.Descriptor:
                reader.Skip(); // Id

                count -= 2;

                // Note: Each argument is stored as two values.
                if (count > 0)
                {
                    SkimArgs(ref reader, count / 2, options);
                }

                break;

            case DocumentationKind.String:
                if (count != 2)
                {
                    throw new MessagePackSerializationException($"Expected array of 2 elements for string documentation, but it was {count}.");
                }

                CachedStringFormatter.Instance.Skim(ref reader, options);

                break;

            default:
                throw new NotSupportedException(SR.FormatUnsupported_argument_kind(documentationKind));
        }

        static void SkimArgs(ref MessagePackReader reader, int count, SerializerCachingOptions options)
        {
            for (var i = 0; i < count; i++)
            {
                SkimArg(ref reader, options);
            }
        }

        static void SkimArg(ref MessagePackReader reader, SerializerCachingOptions options)
        {
            if (reader.TryReadNil())
            {
                return;
            }

            var argKind = (ArgKind)reader.ReadByte();

            switch (argKind)
            {
                case ArgKind.String:
                    CachedStringFormatter.Instance.Skim(ref reader, options);
                    break;

                case ArgKind.Integer:
                case ArgKind.Boolean:
                    reader.Skip();
                    break;

                default:
                    throw new NotSupportedException(SR.FormatUnsupported_argument_kind(argKind));
            }
        }
    }
}
