// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

internal static class ContentBuilderExtensions
{
    private static readonly ReadOnlyMemory<char> s_zeroes = "0000000000".AsMemory(); // 10 zeros

    // This table contains string representations of numbers from 0 to 999.
    private static readonly ImmutableArray<ReadOnlyMemory<char>> s_integerTable = InitializeIntegerTable();

    private static ImmutableArray<ReadOnlyMemory<char>> InitializeIntegerTable()
    {
        var array = new ReadOnlyMemory<char>[1000];

        // Fill entries 100 to 999.
        for (var i = 100; i < 1000; i++)
        {
            array[i] = i.ToString(CultureInfo.InvariantCulture).AsMemory();
        }

        // Fill entries 10 to 99 with two-digit strings sliced from entries 110 to 199.
        for (var i = 10; i < 100; i++)
        {
            array[i] = array[i + 100][^2..];
        }

        // Fill 1 to 9 with slices of the last character from entries 11 to 19.
        for (var i = 1; i < 10; i++)
        {
            array[i] = array[i + 10][^1..];
        }

        // Finally, fill the entry for 0 with a slice from s_zeroes.
        array[0] = s_zeroes[..1];

        return ImmutableCollectionsMarshal.AsImmutableArray(array);
    }

    public static void AppendIntegerLiteral(this ref ContentBuilder builder, int value)
    {
        // Handle zero as a special case
        if (value == 0)
        {
            builder.Append(s_integerTable[0]);
            return;
        }

        var isNegative = value < 0;
        if (isNegative)
        {
            // For negative numbers, write the minus sign first
            builder.Append("-");
        }

        // Fast path: For small numbers (-999 to 999), use the precomputed lookup table directly
        if (value is > -1000 and < 1000)
        {
            var index = isNegative ? -value : value;
            builder.Append(s_integerTable[index]);

            return;
        }

        // Slow path: For larger numbers, decompose into groups of three digits using the precomputed table.
        // This approach avoids string formatting while maintaining readability of the output.

        // Extract digits and write groups from most significant to least significant.
        // Note: Use long to handle int.MinValue correctly. Math.Abs(int.MinValue) would throw.
        var remaining = isNegative ? -(long)value : value;
        long divisor = 1;

        // Find the highest power of 1000 needed (1, 1000, 1000000, 1000000000)
        // This determines how many 3-digit groups we need
        while (remaining >= divisor * 1000)
        {
            divisor *= 1000;
        }

        // Process each group of 3 digits from most significant to least significant
        var first = true;
        while (divisor > 0)
        {
            var group = (int)(remaining / divisor);
            remaining %= divisor;
            divisor /= 1000;

            Debug.Assert(group >= 0 && group < 1000, "Digit group should be in the range [0, 999]");

            if (group == 0)
            {
                Debug.Assert(!first, "The first group should never be 0.");

                // Entire group is zero: add "000" for proper place value
                builder.Append(s_zeroes[..3]);
                continue;
            }

            if (first)
            {
                // First group: no leading zeros needed (e.g., "123" not "0123")
                builder.Append(s_integerTable[group]);
                first = false;
                continue;
            }

            // Groups after the first one with values 1-99 need leading zeros for proper formatting
            // Example: 1234567 becomes "1" + "234" + "567", but 1000067 becomes "1" + "000" + "067"
            var leadingZeros = group switch
            {
                < 10 => 2,  // 1-9: needs "00" prefix (e.g., "007")
                < 100 => 1, // 10-99: needs "0" prefix (e.g., "067") 
                _ => 0      // 100-999: no leading zeros needed
            };

            if (leadingZeros > 0)
            {
                builder.Append(s_zeroes[..leadingZeros]); // Add "00" or "0"
            }

            builder.Append(s_integerTable[group]); // Add the actual digit group
        }
    }
}
