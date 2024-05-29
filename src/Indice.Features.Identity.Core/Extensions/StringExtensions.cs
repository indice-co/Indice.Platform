// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Diagnostics;
using System.Text;

namespace IdentityServer4.Internal.Extensions;

internal static class StringExtensions
{
    [DebuggerStepThrough]
    public static string ToSpaceSeparatedString(this IEnumerable<string> list) {
        if (list is null) {
            return string.Empty;
        }
        var stringBuilder = new StringBuilder(100);
        foreach (var element in list) {
            stringBuilder.Append(element + " ");
        }
        return stringBuilder.ToString().Trim();
    }

    [DebuggerStepThrough]
    public static bool IsPresent(this string value) {
        return !string.IsNullOrWhiteSpace(value);
    }
}
