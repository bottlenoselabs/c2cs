// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Contexts.ReadCodeC.Data.Model;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.Contexts.ReadCodeC.Domain.Explore.Handlers;

[UsedImplicitly]
public sealed class StructExplorer : RecordExplorer
{
    protected override ExploreKindCursors ExpectedCursors { get; } = ExploreKindCursors.Is(CXCursorKind.CXCursor_StructDecl);

    public StructExplorer(ILogger<StructExplorer> logger)
        : base(logger, false)
    {
    }

    public override CRecord Explore(ExploreContext context, ExploreInfoNode info)
    {
        var @struct = Struct(context, info);
        return @struct;
    }

    private CRecord Struct(ExploreContext context, ExploreInfoNode info)
    {
        var fields = StructFields(context, info);
        var record = new CRecord
        {
            RecordKind = CRecordKind.Struct,
            Location = info.Location,
            Name = info.Name,
            Fields = fields,
            SizeOf = info.SizeOf,
            AlignOf = info.AlignOf!.Value
        };
        return record;
    }

    private ImmutableArray<CRecordField> StructFields(
        ExploreContext context,
        ExploreInfoNode structInfo)
    {
        var builder = ImmutableArray.CreateBuilder<CRecordField>();
        var fieldCursors = FieldCursorsFromType(structInfo.Type);
        var fieldCursorsLength = fieldCursors.Length;
        if (fieldCursorsLength > 0)
        {
            for (var i = 0; i < fieldCursors.Length; i++)
            {
                var fieldCursor = fieldCursors[i];
                var field = StructField(context, structInfo, fieldCursor, i);
                builder.Add(field);
            }
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CRecordField StructField(
        ExploreContext context,
        ExploreInfoNode structInfo,
        CXCursor fieldCursor,
        int fieldIndex)
    {
        var fieldName = context.CursorName(fieldCursor);
        var type = clang_getCursorType(fieldCursor);
        var location = context.Location(fieldCursor, type);
        var typeInfo = context.VisitType(type, structInfo, fieldIndex)!;
        var offsetOf = (int)clang_Cursor_getOffsetOfField(fieldCursor) / 8;

        return new CRecordField
        {
            Name = fieldName,
            Location = location,
            TypeInfo = typeInfo,
            OffsetOf = offsetOf,
        };
    }
}
