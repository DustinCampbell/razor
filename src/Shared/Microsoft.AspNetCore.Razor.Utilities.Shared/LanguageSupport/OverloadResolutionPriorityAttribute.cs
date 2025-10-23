// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copied from https://github.com/dotnet/runtime

#if !NET9_0_OR_GREATER

namespace System.Runtime.CompilerServices;

/// <summary>
///  Specifies the priority of a member in overload resolution. When unspecified, the default priority is 0.
/// </summary>
/// <remarks>
///  Initializes a new instance of the <see cref="OverloadResolutionPriorityAttribute"/> class.
/// </remarks>
/// <param name="priority">
///  The priority of the attributed member. Higher numbers are prioritized, lower numbers are
///  deprioritized. 0 is the default if no attribute is present.
/// </param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class OverloadResolutionPriorityAttribute(int priority) : Attribute
{
    /// <summary>
    /// The priority of the member.
    /// </summary>
    public int Priority { get; } = priority;
}

#else

using System.Runtime.CompilerServices;

#pragma warning disable RS0016 // Add public types and members to the declared API (this is a supporting forwarder for an internal polyfill API)
[assembly: TypeForwardedTo(typeof(OverloadResolutionPriorityAttribute))]
#pragma warning restore RS0016 // Add public types and members to the declared API

#endif
