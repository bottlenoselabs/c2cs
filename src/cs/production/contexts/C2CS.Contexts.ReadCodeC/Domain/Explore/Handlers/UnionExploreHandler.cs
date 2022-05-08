// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Contexts.ReadCodeC.Data.Model;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.Contexts.ReadCodeC.Domain.Explore.Handlers;

[UsedImplicitly]
public sealed class UnionExploreHandler : RecordExploreHandler
{
    public UnionExploreHandler(ILogger<UnionExploreHandler> logger)
        : base(logger)
    {
    }

    protected override ExploreKindCursors ExpectedCursors { get; } = ExploreKindCursors.Is(CXCursorKind.CXCursor_UnionDecl);

    protected override bool CanVisit(ExploreContext context, ExploreInfoNode info)
    {
        return true;
    }

    public override CRecord Explore(ExploreContext context, ExploreInfoNode info)
    {
        var union = Union(context, info);
        return union;
    }

    private CRecord Union(ExploreContext context, ExploreInfoNode info)
    {
        var fields = UnionFields(context, info.Cursor, info);

        var result = new CRecord
        {
            RecordKind = CRecordKind.Union,
            Location = info.Location,
            Name = info.TypeName,
            Fields = fields,
            SizeOf = info.SizeOf,
            AlignOf = info.AlignOf
        };
        return result;
    }

    private ImmutableArray<CRecordField> UnionFields(
        ExploreContext context,
        CXCursor cursor,
        ExploreInfoNode parentInfo)
    {
        var builder = ImmutableArray.CreateBuilder<CRecordField>();

        var fieldCursors = RecordFieldCursors(cursor);
        for (var i = 0; i < fieldCursors.Length; i++)
        {
            var fieldCursor = fieldCursors[i];
            var nextRecordField = UnionField(context, fieldCursor, parentInfo, i);
            builder.Add(nextRecordField);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CRecordField UnionField(
        ExploreContext context,
        CXCursor cursor,
        ExploreInfoNode parentInfo,
        int fieldIndex)
    {
        var name = context.CursorName(cursor);
        var type = clang_getCursorType(cursor);
        var location = context.Location(cursor, type);
        var typeInfo = context.VisitType(type, parentInfo, fieldIndex);

        var result = new CRecordField
        {
            Name = name,
            Location = location,
            TypeInfo = typeInfo
        };

        return result;
    }
}
