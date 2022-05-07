// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Contexts.ReadCodeC.Data.Model;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.Contexts.ReadCodeC.Domain.Explore.Handlers;

[UsedImplicitly]
public sealed class StructExploreHandler : RecordExploreHandler
{
    protected override ExploreKindCursors ExpectedCursors { get; } = ExploreKindCursors.Is(CXCursorKind.CXCursor_StructDecl);

    public StructExploreHandler(ILogger<StructExploreHandler> logger)
        : base(logger)
    {
    }

    public override CRecord Explore(ExploreContext context, ExploreInfoNode info)
    {
        var @struct = Struct(context, info);
        return @struct;
    }

    private CRecord Struct(ExploreContext context, ExploreInfoNode info)
    {
        var parentName = info.Parent?.Name ?? string.Empty;
        var fields = StructFields(context, info);
        var record = new CRecord
        {
            RecordKind = CRecordKind.Struct,
            Location = info.Location,
            Name = info.Name,
            ParentName = parentName,
            Fields = fields,
            SizeOf = info.SizeOf,
            AlignOf = info.AlignOf
        };
        return record;
    }

    private ImmutableArray<CRecordField> StructFields(
        ExploreContext context,
        ExploreInfoNode structInfo)
    {
        var builder = ImmutableArray.CreateBuilder<CRecordField>();

        var fieldCursors = RecordFieldCursors(structInfo.Cursor);
        var fieldCursorsLength = fieldCursors.Length;
        if (fieldCursorsLength > 0)
        {
            // Clang does not provide a way to get the padding of a field; we need to do it ourselves.
            // To calculate the padding of a field, work backwards from the last field to the first field using the offsets and sizes reported by Clang.

            var lastFieldCursor = fieldCursors[^1];
            var lastRecordField = StructField(
                context, structInfo, lastFieldCursor, fieldCursorsLength - 1, null);
            builder.Add(lastRecordField);

            for (var i = fieldCursors.Length - 2; i >= 0; i--)
            {
                var nextField = builder[^1];
                var fieldCursor = fieldCursors[i];
                var field = StructField(
                    context, structInfo, fieldCursor, i, nextField);
                builder.Add(field);
            }
        }

        builder.Reverse();
        var result = builder.ToImmutable();
        return result;
    }

    private CRecordField StructField(
        ExploreContext context,
        ExploreInfoNode parentInfo,
        CXCursor fieldCursor,
        int fieldIndex,
        CRecordField? nextField)
    {
        var fieldName = context.CursorName(fieldCursor);
        var type = clang_getCursorType(fieldCursor);
        var location = context.Location(fieldCursor, type);
        var typeInfo = context.VisitType(type, parentInfo, fieldIndex);
        var (offsetOf, paddingOf) = FieldLayout(
            fieldCursor, fieldIndex, nextField?.OffsetOf, parentInfo.SizeOf, typeInfo.SizeOf);

        return new CRecordField
        {
            Name = fieldName,
            Location = location,
            TypeInfo = typeInfo,
            OffsetOf = offsetOf,
            PaddingOf = paddingOf
        };
    }

    private static (int OffsetOf, int PaddingOf) FieldLayout(
        CXCursor fieldCursor,
        int fieldIndex,
        int? nextFieldOffsetOf,
        int parentSizeOf,
        int fieldTypeSizeOf)
    {
        var fieldOffsetOf = (int)clang_Cursor_getOffsetOfField(fieldCursor) / 8;
        int offsetOf;
        if (fieldCursor.kind == CXCursorKind.CXCursor_UnionDecl)
        {
            offsetOf = 0;
        }
        else
        {
            if (fieldOffsetOf < 0 || (fieldIndex != 0 && fieldOffsetOf == 0))
            {
                if (nextFieldOffsetOf == null)
                {
                    offsetOf = 0;
                }
                else
                {
                    offsetOf = nextFieldOffsetOf.Value - fieldTypeSizeOf;
                }
            }
            else
            {
                offsetOf = fieldOffsetOf;
            }
        }

        int paddingOf;
        if (nextFieldOffsetOf == null)
        {
            paddingOf = parentSizeOf - offsetOf - fieldTypeSizeOf;
        }
        else
        {
            paddingOf = nextFieldOffsetOf.Value - offsetOf - fieldTypeSizeOf;
        }

        return (offsetOf, paddingOf);
    }
}
