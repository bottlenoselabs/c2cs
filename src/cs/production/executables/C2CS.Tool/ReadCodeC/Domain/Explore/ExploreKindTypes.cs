// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using static bottlenoselabs.clang;

namespace C2CS.ReadCodeC.Domain.Explore;

public class ExploreKindTypes
{
    private readonly ImmutableArray<CXTypeKind>? _expectedTypeKinds;

    private ExploreKindTypes(ImmutableArray<CXTypeKind>? kinds)
    {
        _expectedTypeKinds = kinds;
    }

    public static ExploreKindTypes Is(CXTypeKind kind)
    {
        var typeKinds = ImmutableArray.Create(kind);
        return new ExploreKindTypes(typeKinds);
    }

    public static ExploreKindTypes Either(params CXTypeKind[] kinds)
    {
        var typeKinds = kinds.ToImmutableArray();
        return new ExploreKindTypes(typeKinds);
    }

    public bool Matches(CXTypeKind cursorKind)
    {
        if (_expectedTypeKinds == null)
        {
            return false;
        }

        var expectedCursorKinds = _expectedTypeKinds.Value;
        if (expectedCursorKinds.IsDefaultOrEmpty)
        {
            return true;
        }

        var isAsExpected = false;
        foreach (var expectedCursorKind in expectedCursorKinds)
        {
            if (cursorKind != expectedCursorKind)
            {
                continue;
            }

            isAsExpected = true;
            break;
        }

        return isAsExpected;
    }

#pragma warning disable CA2211
    public static readonly ExploreKindTypes Any = new(ImmutableArray<CXTypeKind>.Empty);

    public static readonly ExploreKindTypes None = new(null);

#pragma warning restore CA2211
}
