// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

namespace C2CS.Internal;

public static class StringExtensions
{
    public static string ReplaceFirst(
        this string text, string search, string replace, StringComparison stringComparison)
    {
        var indexOf = text.IndexOf(search, stringComparison);
        if (indexOf < 0)
        {
            return text;
        }

        var before = text.AsSpan(0, indexOf);
        var after = text.AsSpan(indexOf + search.Length);
        var result = string.Concat(before, replace, after);
        return result;
    }
}
