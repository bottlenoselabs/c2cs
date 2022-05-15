// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Contexts.ReadCodeC.Data.Model;
using Microsoft.Extensions.Logging;

namespace C2CS.Contexts.ReadCodeC.Domain.Explore.Handlers;
using static bottlenoselabs.clang;

public abstract class RecordExploreHandler : ExploreHandler<CRecord>
{
    protected override ExploreKindTypes ExpectedTypes { get; } = ExploreKindTypes.Is(CXTypeKind.CXType_Record);

    protected RecordExploreHandler(ILogger<RecordExploreHandler> logger, bool logAlreadyExplored = true)
        : base(logger, logAlreadyExplored)
    {
    }

    protected ImmutableArray<CXCursor> RecordFieldCursors(CXCursor recordCursor)
    {
        // We need to consider unions because they could be anonymous.
        //  Case 1: If the union has no tag (identifier) and has no member name (field name), the union should be promoted to an anonymous field.
        //  Case 2: If the union has no tag (identifier) and has a member name (field name), it should be included as a normal field.
        //  Case 3: If the union has a tag (identifier) and has no member name (field name), it should not be included at all as a field. (Dangling union.)
        //  Case 4: If the union has a tag (identifier) and has a member name (field name), it should be included as a normal field.
        // The problem is that C allows unions or structs to be declared inside the body of the union or struct.
        // This makes matching type identifiers to field names slightly difficult as Clang reports back the fields, unions, and structs for a given struct or union.
        // However, the unions and structs reported are always before the field for the matching union or struct, if there is one.
        // Thus, the solution here is to filter out the unions or structs that match to a field, leaving behind the anonymous structs or unions that need to get promoted.
        //  I.e. return only cursors which are fields, except for case 1.

        var fieldCursors = recordCursor.GetDescendents((child, _) =>
        {
            var isField = child.kind == CXCursorKind.CXCursor_FieldDecl;
            var isUnion = child.kind == CXCursorKind.CXCursor_UnionDecl;
            return isField || isUnion;
        });

        if (fieldCursors.IsDefaultOrEmpty)
        {
            return ImmutableArray<CXCursor>.Empty;
        }

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

        return filteredFieldCursors.ToImmutableArray();
    }
}
