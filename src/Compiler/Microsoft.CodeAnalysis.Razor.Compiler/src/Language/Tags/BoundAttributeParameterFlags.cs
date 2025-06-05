// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

[Flags]
internal enum BoundAttributeParameterFlags : byte
{
    IsEnum = 1 << 0,
    IsStringProperty = 1 << 1,
    IsBooleanProperty = 1 << 2,
    IsBindAttributeGetSet = 1 << 3,
}
