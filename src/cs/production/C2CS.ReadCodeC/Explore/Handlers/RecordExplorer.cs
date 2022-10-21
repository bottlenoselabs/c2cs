// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Data.C.Model;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.ReadCodeC.Explore.Handlers;

public abstract class RecordExplorer : ExploreHandler<CRecord>
{
    protected RecordExplorer(ILogger<RecordExplorer> logger, bool logAlreadyExplored = true)
        : base(logger, logAlreadyExplored)
    {
    }

    protected override ExploreKindTypes ExpectedTypes { get; } = ExploreKindTypes.Is(CXTypeKind.CXType_Record);

    public static ImmutableArray<CXCursor> FieldCursorsFromType(CXType type)
    {
        var cursors = type.GetFields();
        if (cursors.IsDefaultOrEmpty)
        {
            return ImmutableArray<CXCursor>.Empty;
        }

        var result = FieldCursors(cursors);
        return result;
    }

    private static ImmutableArray<CXCursor> FieldCursors(ImmutableArray<CXCursor> fieldCursors)
    {
        var filteredFieldCursors = ImmutableArray.CreateBuilder<CXCursor>();
        filteredFieldCursors.Add(fieldCursors[^1]);

        for (var index = fieldCursors.Length - 2; index >= 0; index--)
        {
            var current = fieldCursors[index];
            var next = fieldCursors[index + 1];

            if (current.kind == CXCursorKind.CXCursor_UnionDecl && next.kind == CXCursorKind.CXCursor_FieldDecl)
            {
                var typeNext = clang_getCursorType(next);
                var typeCurrent = clang_getCursorType(current);

                var typeNextCursor = clang_getTypeDeclaration(typeNext);
                var typeCurrentCursor = clang_getTypeDeclaration(typeCurrent);

                var cursorsAreEqual = clang_equalCursors(typeNextCursor, typeCurrentCursor) > 0;
                if (cursorsAreEqual)
                {
                    // union has a tag and a member name
                    continue;
                }
            }

            filteredFieldCursors.Add(current);
        }

        if (filteredFieldCursors.Count > 1)
        {
            filteredFieldCursors.Reverse();
        }

        var result = filteredFieldCursors.ToImmutableArray();
        return result;
    }
}
