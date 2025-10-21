// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

public readonly partial struct Content
{
    /// <summary>
    ///  Determines whether the content contains the specified character.
    /// </summary>
    /// <param name="value">The character to search for.</param>
    /// <returns>
    ///  <see langword="true"/> if the character is found; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Contains(char value)
        => IndexOf(value) >= 0;

    /// <summary>
    ///  Determines whether the content contains the specified substring.
    /// </summary>
    /// <param name="value">The substring to search for.</param>
    /// <param name="comparison">The string comparison type to use.</param>
    /// <returns>
    ///  <see langword="true"/> if the substring is found; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Contains(ReadOnlySpan<char> value, StringComparison comparison)
        => IndexOf(value, comparison) >= 0;

    /// <summary>
    ///  Determines whether the content contains any of the two specified characters.
    /// </summary>
    /// <param name="value0">The first character to search for.</param>
    /// <param name="value1">The second character to search for.</param>
    /// <returns>
    ///  <see langword="true"/> if any of the characters are found; otherwise, <see langword="false"/>.
    /// </returns>
    public bool ContainsAny(char value0, char value1)
        => IndexOfAny(value0, value1) >= 0;

    /// <summary>
    ///  Determines whether the content contains any of the three specified characters.
    /// </summary>
    /// <param name="value0">The first character to search for.</param>
    /// <param name="value1">The second character to search for.</param>
    /// <param name="value2">The third character to search for.</param>
    /// <returns>
    ///  <see langword="true"/> if any of the characters are found; otherwise, <see langword="false"/>.
    /// </returns>
    public bool ContainsAny(char value0, char value1, char value2)
        => IndexOfAny(value0, value1, value2) >= 0;

    /// <summary>
    ///  Determines whether the content contains any of the specified characters.
    /// </summary>
    /// <param name="values">The characters to search for.</param>
    /// <returns>
    ///  <see langword="true"/> if any of the characters are found; otherwise, <see langword="false"/>.
    /// </returns>
    public bool ContainsAny(ReadOnlySpan<char> values)
        => IndexOfAny(values) >= 0;

    /// <summary>
    ///  Determines whether the content contains any character other than the specified character.
    /// </summary>
    /// <param name="value">The character to exclude from the search.</param>
    /// <returns>
    ///  <see langword="true"/> if any character other than the specified character is found;
    ///  otherwise, <see langword="false"/>.
    /// </returns>
    public bool ContainsAnyExcept(char value)
        => IndexOfAnyExcept(value) >= 0;

    /// <summary>
    ///  Determines whether the content contains any character other than the two specified characters.
    /// </summary>
    /// <param name="value0">The first character to exclude from the search.</param>
    /// <param name="value1">The second character to exclude from the search.</param>
    /// <returns>
    ///  <see langword="true"/> if any character other than the specified characters is found;
    ///  otherwise, <see langword="false"/>.
    /// </returns>
    public bool ContainsAnyExcept(char value0, char value1)
        => IndexOfAnyExcept(value0, value1) >= 0;

    /// <summary>
    ///  Determines whether the content contains any character other than the three specified characters.
    /// </summary>
    /// <param name="value0">The first character to exclude from the search.</param>
    /// <param name="value1">The second character to exclude from the search.</param>
    /// <param name="value2">The third character to exclude from the search.</param>
    /// <returns>
    ///  <see langword="true"/> if any character other than the specified characters is found;
    ///  otherwise, <see langword="false"/>.
    /// </returns>
    public bool ContainsAnyExcept(char value0, char value1, char value2)
        => IndexOfAnyExcept(value0, value1, value2) >= 0;

    /// <summary>
    ///  Determines whether the content contains any character other than the specified characters.
    /// </summary>
    /// <param name="values">The characters to exclude from the search.</param>
    /// <returns>
    ///  <see langword="true"/> if any character other than the specified characters is found;
    ///  otherwise, <see langword="false"/>.
    /// </returns>
    public bool ContainsAnyExcept(ReadOnlySpan<char> values)
        => IndexOfAnyExcept(values) >= 0;
}
