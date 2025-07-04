﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed partial class DefaultRazorTagHelperContextDiscoveryPhase : RazorEnginePhaseBase
{
    protected override void ExecuteCore(RazorCodeDocument codeDocument, CancellationToken cancellationToken)
    {
        var syntaxTree = codeDocument.GetPreTagHelperSyntaxTree() ?? codeDocument.GetSyntaxTree();
        ThrowForMissingDocumentDependency(syntaxTree);

        if (!codeDocument.TryGetTagHelpers(out var tagHelpers))
        {
            if (!Engine.TryGetFeature(out ITagHelperFeature? tagHelperFeature))
            {
                // No feature, nothing to do.
                return;
            }

            tagHelpers = tagHelperFeature.GetDescriptors();
        }

        using var _ = GetPooledVisitor(codeDocument, tagHelpers, out var visitor);

        // We need to find directives in all of the *imports* as well as in the main razor file
        //
        // The imports come logically before the main razor file and are in the order they
        // should be processed.

        if (codeDocument.TryGetImportSyntaxTrees(out var imports))
        {
            foreach (var import in imports)
            {
                visitor.Visit(import);
            }
        }

        visitor.Visit(syntaxTree);

        // This will always be null for a component document.
        var tagHelperPrefix = visitor.TagHelperPrefix;

        var context = TagHelperDocumentContext.Create(tagHelperPrefix, visitor.GetResults());
        codeDocument.SetTagHelperContext(context);
        codeDocument.SetPreTagHelperSyntaxTree(syntaxTree);
    }

    internal static ReadOnlyMemory<char> GetMemoryWithoutGlobalPrefix(string s)
    {
        const string globalPrefix = "global::";

        var mem = s.AsMemory();

        if (mem.Span.StartsWith(globalPrefix.AsSpan(), StringComparison.Ordinal))
        {
            return mem[globalPrefix.Length..];
        }

        return mem;
    }

    internal abstract class DirectiveVisitor : SyntaxWalker
    {
        private bool _isInitialized;
        private readonly HashSet<TagHelperDescriptor> _matches = [];

        protected bool IsInitialized => _isInitialized;

        public virtual string? TagHelperPrefix => null;

        public abstract void Visit(RazorSyntaxTree tree);

        public ImmutableArray<TagHelperDescriptor> GetResults() => [.. _matches];

        protected void Initialize()
        {
            _isInitialized = true;
        }

        public virtual void Reset()
        {
            _matches.Clear();
            _isInitialized = false;
        }

        public void AddMatch(TagHelperDescriptor tagHelper)
        {
            _matches.Add(tagHelper);
        }

        public void AddMatches(List<TagHelperDescriptor> tagHelpers)
        {
            foreach (var tagHelper in tagHelpers)
            {
                _matches.Add(tagHelper);
            }
        }

        public void RemoveMatch(TagHelperDescriptor tagHelper)
        {
            _matches.Remove(tagHelper);
        }

        public void RemoveMatches(List<TagHelperDescriptor> tagHelpers)
        {
            foreach (var tagHelper in tagHelpers)
            {
                _matches.Remove(tagHelper);
            }
        }
    }

    internal sealed class TagHelperDirectiveVisitor : DirectiveVisitor
    {
        /// <summary>
        /// A larger pool of <see cref="TagHelperDescriptor"/> lists to handle scenarios where tag helpers
        /// originate from a large number of assemblies.
        /// </summary>
        private static readonly ObjectPool<List<TagHelperDescriptor>> s_pool = ListPool<TagHelperDescriptor>.Create(100);

        /// <summary>
        /// A map from assembly name to list of <see cref="TagHelperDescriptor"/>. Lists are allocated from and returned to
        /// <see cref="s_pool"/>.
        /// </summary>
        private readonly Dictionary<string, List<TagHelperDescriptor>> _nonComponentTagHelperMap = new(StringComparer.Ordinal);

        private IReadOnlyList<TagHelperDescriptor>? _tagHelpers;
        private bool _nonComponentTagHelperMapComputed;
        private string? _tagHelperPrefix;

        public override string? TagHelperPrefix => _tagHelperPrefix;

        private Dictionary<string, List<TagHelperDescriptor>> NonComponentTagHelperMap
        {
            get
            {
                if (!_nonComponentTagHelperMapComputed)
                {
                    ComputeNonComponentTagHelpersMap();

                    _nonComponentTagHelperMapComputed = true;
                }

                return _nonComponentTagHelperMap;

                void ComputeNonComponentTagHelpersMap()
                {
                    var tagHelpers = _tagHelpers.AssumeNotNull();

                    string? currentAssemblyName = null;
                    List<TagHelperDescriptor>? currentTagHelpers = null;

                    // We don't want to consider components in a view document.
                    foreach (var tagHelper in tagHelpers.AsEnumerable())
                    {
                        if (!tagHelper.IsAnyComponentDocumentTagHelper())
                        {
                            if (tagHelper.AssemblyName != currentAssemblyName)
                            {
                                currentAssemblyName = tagHelper.AssemblyName;

                                if (!_nonComponentTagHelperMap.TryGetValue(currentAssemblyName, out currentTagHelpers))
                                {
                                    currentTagHelpers = s_pool.Get();
                                    _nonComponentTagHelperMap.Add(currentAssemblyName, currentTagHelpers);
                                }
                            }

                            currentTagHelpers!.Add(tagHelper);
                        }
                    }
                }
            }
        }

        public void Initialize(IReadOnlyList<TagHelperDescriptor> tagHelpers)
        {
            Debug.Assert(!IsInitialized);

            _tagHelpers = tagHelpers;

            base.Initialize();
        }

        public override void Reset()
        {
            foreach (var (_, tagHelpers) in _nonComponentTagHelperMap)
            {
                s_pool.Return(tagHelpers);
            }

            _nonComponentTagHelperMap.Clear();
            _nonComponentTagHelperMapComputed = false;
            _tagHelpers = null;
            _tagHelperPrefix = null;

            base.Reset();
        }

        public override void Visit(RazorSyntaxTree tree)
        {
            Visit(tree.Root);
        }

        public override void VisitRazorDirective(RazorDirectiveSyntax node)
        {
            foreach (var child in node.DescendantNodes())
            {
                if (child is not CSharpStatementLiteralSyntax literal)
                {
                    continue;
                }

                switch (literal.ChunkGenerator)
                {
                    case AddTagHelperChunkGenerator addTagHelper:
                        {
                            if (addTagHelper.AssemblyName == null)
                            {
                                // Skip this one, it's an error
                                continue;
                            }

                            if (!NonComponentTagHelperMap.TryGetValue(addTagHelper.AssemblyName, out var nonComponentTagHelpers))
                            {
                                // No tag helpers in the assembly.
                                continue;
                            }

                            switch (GetMemoryWithoutGlobalPrefix(addTagHelper.TypePattern).Span)
                            {
                                case ['*']:
                                    AddMatches(nonComponentTagHelpers);
                                    break;

                                case [.. var pattern, '*']:
                                    foreach (var tagHelper in nonComponentTagHelpers)
                                    {
                                        if (tagHelper.Name.AsSpan().StartsWith(pattern, StringComparison.Ordinal))
                                        {
                                            AddMatch(tagHelper);
                                        }
                                    }

                                    break;

                                case var pattern:
                                    foreach (var tagHelper in nonComponentTagHelpers)
                                    {
                                        if (tagHelper.Name.AsSpan().Equals(pattern, StringComparison.Ordinal))
                                        {
                                            AddMatch(tagHelper);
                                        }
                                    }

                                    break;
                            }
                        }

                        break;

                    case RemoveTagHelperChunkGenerator removeTagHelper:
                        {
                            if (removeTagHelper.AssemblyName == null)
                            {
                                // Skip this one, it's an error
                                continue;
                            }

                            if (!NonComponentTagHelperMap.TryGetValue(removeTagHelper.AssemblyName, out var nonComponentTagHelpers))
                            {
                                // No tag helpers in the assembly.
                                continue;
                            }

                            switch (GetMemoryWithoutGlobalPrefix(removeTagHelper.TypePattern).Span)
                            {
                                case ['*']:
                                    RemoveMatches(nonComponentTagHelpers);
                                    break;

                                case [.. var pattern, '*']:
                                    foreach (var tagHelper in nonComponentTagHelpers)
                                    {
                                        if (tagHelper.Name.AsSpan().StartsWith(pattern, StringComparison.Ordinal))
                                        {
                                            RemoveMatch(tagHelper);
                                        }
                                    }

                                    break;

                                case var pattern:
                                    foreach (var tagHelper in nonComponentTagHelpers)
                                    {
                                        if (tagHelper.Name.AsSpan().Equals(pattern, StringComparison.Ordinal))
                                        {
                                            RemoveMatch(tagHelper);
                                        }
                                    }

                                    break;
                            }
                        }

                        break;

                    case TagHelperPrefixDirectiveChunkGenerator tagHelperPrefix:
                        if (!tagHelperPrefix.DirectiveText.IsNullOrEmpty())
                        {
                            // We only expect to see a single one of these per file, but that's enforced at another level.
                            _tagHelperPrefix = tagHelperPrefix.DirectiveText;
                        }

                        break;
                }
            }
        }
    }

    internal sealed class ComponentDirectiveVisitor : DirectiveVisitor
    {
        // The list values in this dictionary are pooled
        private readonly Dictionary<ReadOnlyMemory<char>, List<TagHelperDescriptor>> _typeNamespaceToNonFullyQualifiedComponents = new Dictionary<ReadOnlyMemory<char>, List<TagHelperDescriptor>>(ReadOnlyMemoryOfCharComparer.Instance);
        private List<TagHelperDescriptor>? _nonFullyQualifiedComponentsWithEmptyTypeNamespace;

        private string? _filePath;
        private RazorSourceDocument? _source;

        private string FilePath => _filePath.AssumeNotNull();
        private RazorSourceDocument Source => _source.AssumeNotNull();

        public void Initialize(string filePath, IReadOnlyList<TagHelperDescriptor> tagHelpers, string? currentNamespace)
        {
            Debug.Assert(!IsInitialized);

            _filePath = filePath;
            _nonFullyQualifiedComponentsWithEmptyTypeNamespace = ListPool<TagHelperDescriptor>.Default.Get();

            foreach (var tagHelper in tagHelpers.AsEnumerable())
            {
                // We don't want to consider non-component tag helpers in a component document.
                if (!tagHelper.IsAnyComponentDocumentTagHelper() || IsTagHelperFromMangledClass(tagHelper))
                {
                    continue;
                }

                if (tagHelper.IsComponentFullyQualifiedNameMatch)
                {
                    // If the component descriptor matches for a fully qualified name, using directives shouldn't matter.
                    AddMatch(tagHelper);
                    continue;
                }

                var tagHelperTypeNamespace = tagHelper.GetTypeNamespace().AsMemory();

                if (tagHelperTypeNamespace.IsEmpty)
                {
                    _nonFullyQualifiedComponentsWithEmptyTypeNamespace.Add(tagHelper);
                }
                else
                {
                    if (!_typeNamespaceToNonFullyQualifiedComponents.TryGetValue(tagHelperTypeNamespace, out var tagHelpersList))
                    {
                        tagHelpersList = ListPool<TagHelperDescriptor>.Default.Get();
                        _typeNamespaceToNonFullyQualifiedComponents.Add(tagHelperTypeNamespace, tagHelpersList);
                    }

                    tagHelpersList.Add(tagHelper);
                }

                if (currentNamespace is null)
                {
                    continue;
                }

                if (IsTypeNamespaceInScope(tagHelperTypeNamespace.Span, currentNamespace))
                {
                    // If the type is already in scope of the document's namespace, using isn't necessary.
                    AddMatch(tagHelper);
                }
            }

            base.Initialize();
        }

        public override void Reset()
        {
            if (_nonFullyQualifiedComponentsWithEmptyTypeNamespace != null)
            {
                ListPool<TagHelperDescriptor>.Default.Return(_nonFullyQualifiedComponentsWithEmptyTypeNamespace);
                _nonFullyQualifiedComponentsWithEmptyTypeNamespace = null;
            }

            foreach (var (_, tagHelpers) in _typeNamespaceToNonFullyQualifiedComponents)
            {
                ListPool<TagHelperDescriptor>.Default.Return(tagHelpers);
            }

            _typeNamespaceToNonFullyQualifiedComponents.Clear();
            _filePath = null;
            _source = null;

            base.Reset();
        }

        public override void Visit(RazorSyntaxTree tree)
        {
            // Set _source based on the current tree, since this visitor is called for the document and its imports.
            _source = tree.Source;
            Visit(tree.Root);
        }

        public override void VisitRazorDirective(RazorDirectiveSyntax node)
        {
            var componentsWithEmptyTypeNamespace = _nonFullyQualifiedComponentsWithEmptyTypeNamespace.AssumeNotNull();

            var descendantLiterals = node.DescendantNodes();
            foreach (var child in descendantLiterals)
            {
                if (child is not CSharpStatementLiteralSyntax literal)
                {
                    continue;
                }

                switch (literal.ChunkGenerator)
                {
                    case AddTagHelperChunkGenerator addTagHelper:
                        if (FilePath == Source.FilePath)
                        {
                            addTagHelper.Diagnostics.Add(
                                ComponentDiagnosticFactory.Create_UnsupportedTagHelperDirective(node.GetSourceSpan(Source)));
                        }

                        break;

                    case RemoveTagHelperChunkGenerator removeTagHelper:
                        // Make sure this node exists in the file we're parsing and not in its imports.
                        if (FilePath == Source.FilePath)
                        {
                            removeTagHelper.Diagnostics.Add(
                                ComponentDiagnosticFactory.Create_UnsupportedTagHelperDirective(node.GetSourceSpan(Source)));
                        }

                        break;

                    case TagHelperPrefixDirectiveChunkGenerator tagHelperPrefix:
                        // Make sure this node exists in the file we're parsing and not in its imports.
                        if (FilePath == Source.FilePath)
                        {
                            tagHelperPrefix.Diagnostics.Add(
                                ComponentDiagnosticFactory.Create_UnsupportedTagHelperDirective(node.GetSourceSpan(Source)));
                        }

                        break;

                    case AddImportChunkGenerator { IsStatic: false } usingStatement:
                        // Get the namespace from the using statement.
                        var @namespace = usingStatement.ParsedNamespace;
                        if (@namespace.Contains('='))
                        {
                            // We don't support usings with alias.
                            continue;
                        }

                        if (_typeNamespaceToNonFullyQualifiedComponents.Count == 0 && componentsWithEmptyTypeNamespace.Count == 0)
                        {
                            // There aren't any non qualified components to add
                            continue;
                        }

                        // Add all tag helpers that have an empty type namespace
                        foreach (var tagHelper in componentsWithEmptyTypeNamespace)
                        {
                            Debug.Assert(!tagHelper.IsComponentFullyQualifiedNameMatch, "We've already processed these.");

                            AddMatch(tagHelper);
                        }

                        // Remove global:: prefix from namespace.
                        var normalizedNamespace = GetMemoryWithoutGlobalPrefix(@namespace);

                        // Add all tag helpers with a matching namespace
                        if (_typeNamespaceToNonFullyQualifiedComponents.TryGetValue(normalizedNamespace, out var tagHelpers))
                        {
                            foreach (var tagHelper in tagHelpers)
                            {
                                Debug.Assert(!tagHelper.IsComponentFullyQualifiedNameMatch, "We've already processed these.");

                                AddMatch(tagHelper);
                            }
                        }

                        break;
                }
            }
        }

        // Check if a type's namespace is already in scope given the namespace of the current document.
        // E.g,
        // If the namespace of the document is `MyComponents.Components.Shared`,
        // then the types `MyComponents.FooComponent`, `MyComponents.Components.BarComponent`, `MyComponents.Components.Shared.BazComponent` are all in scope.
        // Whereas `MyComponents.SomethingElse.OtherComponent` is not in scope.
        internal static bool IsTypeNamespaceInScope(ReadOnlySpan<char> typeNamespace, string @namespace)
        {
            if (typeNamespace.IsEmpty)
            {
                // Either the typeName is not the full type name or this type is at the top level.
                return true;
            }

            if (!@namespace.StartsWith(typeNamespace, StringComparison.Ordinal))
            {
                // typeName: MyComponents.Shared.SomeCoolNamespace
                // currentNamespace: MyComponents.Shared
                return false;
            }

            if (typeNamespace.Length > @namespace.Length && typeNamespace[@namespace.Length] != '.')
            {
                // typeName: MyComponents.SharedFoo
                // currentNamespace: MyComponent.Shared
                return false;
            }

            return true;
        }

        // We need to filter out the duplicate tag helper descriptors that come from the
        // open file in the editor. We mangle the class name for its generated code, so using that here to filter these out.
        internal static bool IsTagHelperFromMangledClass(TagHelperDescriptor tagHelper)
        {
            return ComponentMetadata.IsMangledClass(tagHelper.GetTypeNameIdentifier());
        }
    }
}
