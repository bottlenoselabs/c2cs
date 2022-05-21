// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using static bottlenoselabs.clang;

namespace C2CS.Contexts.ReadCodeC.Domain.Explore;

public class ExploreKindCursors
{
    private readonly ImmutableArray<CXCursorKind>? _expectedCursorKinds;

#pragma warning disable CA2211
    public static readonly ExploreKindCursors Any = new(ImmutableArray<CXCursorKind>.Empty);

    public static readonly ExploreKindCursors None = new(null);

#pragma warning restore CA2211

    private ExploreKindCursors(ImmutableArray<CXCursorKind>? kinds)
    {
        _expectedCursorKinds = kinds;
    }

    public static ExploreKindCursors Is(CXCursorKind kind)
    {
        var cursorKinds = ImmutableArray.Create(kind);
        return new ExploreKindCursors(cursorKinds);
    }

    public static ExploreKindCursors Either(params CXCursorKind[] kinds)
    {
        var cursorKinds = kinds.ToImmutableArray();
        return new ExploreKindCursors(cursorKinds);
    }

    public bool Matches(CXCursorKind cursorKind)
    {
        if (_expectedCursorKinds == null)
        {
            return false;
        }

        var expectedCursorKinds = _expectedCursorKinds.Value;
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
}
