﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Razor.Serialization.MessagePack;

internal enum ArgKind
{
    Integer,
    String,
    Null,
    True,
    False,
    Boolean
}
