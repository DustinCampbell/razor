﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators
{
    public partial class RazorSourceGenerator
    {
        internal static string GetIdentifierFromPath(string filePath)
        {
            var builder = new StringBuilder(filePath.Length);

            for (var i = 0; i < filePath.Length; i++)
            {
                switch (filePath[i])
                {
                    case ':' or '\\' or '/':
                    case char ch when !char.IsLetterOrDigit(ch):
                        builder.Append('_');
                        break;
                    default:
                        builder.Append(filePath[i]);
                        break;
                }
            }

            builder.Append(".g.cs");
            return builder.ToString();
        }

        private static RazorProjectEngine GetDeclarationProjectEngine(
            SourceGeneratorProjectItem item,
            IEnumerable<SourceGeneratorProjectItem> imports,
            RazorSourceGenerationOptions razorSourceGeneratorOptions)
        {
            var fileSystem = new VirtualRazorProjectFileSystem();
            fileSystem.Add(item);
            foreach (var import in imports)
            {
                fileSystem.Add(import);
            }

            var discoveryProjectEngine = RazorProjectEngine.Create(razorSourceGeneratorOptions.Configuration, fileSystem, b =>
            {
                b.ConfigureCodeGenerationOptions(builder =>
                {
                    builder.SuppressPrimaryMethodBody = true;
                    builder.SuppressChecksum = true;
                    builder.SupportLocalizedComponentNames = razorSourceGeneratorOptions.SupportLocalizedComponentNames;
                });

                b.ConfigureParserOptions(builder =>
                {
                    builder.UseRoslynTokenizer = razorSourceGeneratorOptions.UseRoslynTokenizer;
                    builder.CSharpParseOptions = razorSourceGeneratorOptions.CSharpParseOptions;
                });

                b.SetRootNamespace(razorSourceGeneratorOptions.RootNamespace);

                CompilerFeatures.Register(b);
                RazorExtensions.Register(b);

                b.SetCSharpLanguageVersion(razorSourceGeneratorOptions.CSharpParseOptions.LanguageVersion);
            });

            return discoveryProjectEngine;
        }

        private static StaticCompilationTagHelperFeature GetStaticTagHelperFeature(Compilation compilation)
        {
            var tagHelperFeature = new StaticCompilationTagHelperFeature(compilation);

            // the tagHelperFeature will have its Engine property set as part of adding it to the engine, which is used later when doing the actual discovery
            var discoveryProjectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, new VirtualRazorProjectFileSystem(), b =>
            {
                b.Features.Add(tagHelperFeature);
                b.Features.Add(new DefaultTagHelperDescriptorProvider());

                CompilerFeatures.Register(b);
                RazorExtensions.Register(b);
            });

            return tagHelperFeature;
        }

        private static SourceGeneratorProjectEngine GetGenerationProjectEngine(
            SourceGeneratorProjectItem item,
            IEnumerable<SourceGeneratorProjectItem> imports,
            RazorSourceGenerationOptions razorSourceGeneratorOptions)
        {
            var fileSystem = new VirtualRazorProjectFileSystem();
            fileSystem.Add(item);
            foreach (var import in imports)
            {
                fileSystem.Add(import);
            }

            var projectEngine = RazorProjectEngine.Create(razorSourceGeneratorOptions.Configuration, fileSystem, b =>
            {
                b.SetRootNamespace(razorSourceGeneratorOptions.RootNamespace);

                b.ConfigureCodeGenerationOptions(builder =>
                {
                    builder.SuppressMetadataSourceChecksumAttributes = !razorSourceGeneratorOptions.GenerateMetadataSourceChecksumAttributes;
                    builder.SupportLocalizedComponentNames = razorSourceGeneratorOptions.SupportLocalizedComponentNames;
                    builder.SuppressUniqueIds = razorSourceGeneratorOptions.TestSuppressUniqueIds;
                    builder.SuppressAddComponentParameter = razorSourceGeneratorOptions.Configuration.SuppressAddComponentParameter;
                });

                b.ConfigureParserOptions(builder =>
                {
                    builder.UseRoslynTokenizer = razorSourceGeneratorOptions.UseRoslynTokenizer;
                    builder.CSharpParseOptions = razorSourceGeneratorOptions.CSharpParseOptions;
                });

                CompilerFeatures.Register(b);
                RazorExtensions.Register(b);

                b.SetCSharpLanguageVersion(razorSourceGeneratorOptions.CSharpParseOptions.LanguageVersion);
            });

            return new SourceGeneratorProjectEngine(projectEngine);
        }
    }
}
