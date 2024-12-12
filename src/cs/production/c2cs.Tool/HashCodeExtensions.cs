// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace C2CS;

public static class HashCodeExtensions
{
    public static int GetHashCodeMembers<T>(
        this ImmutableArray<T> immutableArray,
        IEqualityComparer<T>? comparer = null)
    {
        var nestedStructsHashCode = 0;

        if (immutableArray.IsDefaultOrEmpty)
        {
            return nestedStructsHashCode;
        }

        var comparer2 = comparer ?? EqualityComparer<T>.Default;

        foreach (var item in immutableArray)
        {
            nestedStructsHashCode ^= comparer2.GetHashCode(item!);
        }

        return nestedStructsHashCode;
    }
}
