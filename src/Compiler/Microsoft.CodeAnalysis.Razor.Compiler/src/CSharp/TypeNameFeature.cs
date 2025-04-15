// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor;

internal abstract class TypeNameFeature : RazorEngineFeatureBase
{
    public abstract IReadOnlyList<string> ParseTypeParameters(string typeName);

    public abstract TypeNameRewriter CreateGenericTypeRewriter(Dictionary<string, string> bindings);

    public abstract TypeNameRewriter CreateGlobalQualifiedTypeNameRewriter(ICollection<string> ignore);

    public abstract bool IsLambda(string expression);
}
