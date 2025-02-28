// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial class RazorCodeGenerationOptions
{
    [Flags]
    private enum Flags
    {
        DesignTime = 1 << 0,
        IndentWithTabs = 1 << 1,
        SuppressChecksum = 1 << 2,
        SuppressMetadataAttributes = 1 << 3,
        SuppressMetadataSourceChecksumAttributes = 1 << 4,
        SuppressPrimaryMethodBody = 1 << 5,
        SuppressNullabilityEnforcement = 1 << 6,
        OmitMinimizedComponentAttributeValues = 1 << 7,
        SupportLocalizedComponentNames = 1 << 8,
        UseEnhancedLinePragma = 1 << 9,
        SuppressAddComponentParameter = 1 << 10,
        RemapLinePragmaPathsOnWindows = 1 << 11,

        DefaultFlags = UseEnhancedLinePragma,
        DefaultDesignTimeFlags = DesignTime | SuppressMetadataAttributes | UseEnhancedLinePragma | RemapLinePragmaPathsOnWindows
    }

    private static Flags GetDefaultFlags(RazorLanguageVersion languageVersion, LanguageVersion csharpLanguageVersion)
    {
        // This converts any "latest", "default" or "LatestMajor" LanguageVersions into their numerical equivalent.
        csharpLanguageVersion = LanguageVersionFacts.MapSpecifiedToEffectiveVersion(csharpLanguageVersion);

        Flags result = 0;

        if (languageVersion.Major is < 3 || csharpLanguageVersion < LanguageVersion.CSharp8)
        {
            // Prior to Razor 3.0 there were no C# version specific controlled features
            // and nullability was introduced in C# 8.0.
            result.SetFlag(Flags.SuppressNullabilityEnforcement);
        }

        if (languageVersion.Major is >= 5)
        {
            // This is a useful optimization but isn't supported by older framework versions
            result.SetFlag(Flags.OmitMinimizedComponentAttributeValues);
        }

        if (csharpLanguageVersion >= LanguageVersion.CSharp10)
        {
            result.SetFlag(Flags.UseEnhancedLinePragma);
        }

        return result;
    }
}
