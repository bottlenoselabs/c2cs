// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Contexts.ReadCodeC.Data.Model;
using C2CS.Contexts.ReadCodeC.Domain.Explore.Diagnostics;
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

            CalculatePaddingOf(context, structInfo, builder);
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

        var isBitField = clang_Cursor_isBitField(fieldCursor) > 0;
        var bitWidthOf = clang_getFieldDeclBitWidth(fieldCursor);
        var byteWidthOf = isBitField ? bitWidthOf / 8 : typeInfo.SizeOf;

        return new CRecordField
        {
            Name = fieldName,
            Location = location,
            TypeInfo = typeInfo,
            OffsetOf = offsetOf,
            ByteWidthOf = byteWidthOf,
            PaddingOf = 0 // Set later
        };
    }

    private void CalculatePaddingOf(
        ExploreContext context,
        ExploreInfoNode structInfo,
        ImmutableArray<CRecordField>.Builder fields)
    {
        var sizeSoFar = 0;
        var packedSoFar = 0;

        CRecordField? lastHoleField = null;
        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];

            var nextField = i + 1 >= fields.Count ? null : fields[i + 1];

            sizeSoFar = packedSoFar + field.TypeInfo.SizeOf;
            if (nextField != null)
            {
                packedSoFar = nextField.OffsetOf; // Use Clang's reported
            }
            else
            {
                packedSoFar = field.OffsetOf + field.ByteWidthOf;
            }

            var canTightlyPack = lastHoleField != null && lastHoleField.PaddingOf >= field.ByteWidthOf;
            if (canTightlyPack)
            {
                lastHoleField!.PaddingOf -= field.ByteWidthOf;
            }
            else
            {
                var potentialPaddingOf = Math.Abs(sizeSoFar - packedSoFar);
                if (potentialPaddingOf == 0)
                {
                    lastHoleField = null;
                }
                else
                {
                    lastHoleField = field;
                    lastHoleField.PaddingOf = potentialPaddingOf;
                }
            }
        }

        var lastField = fields.Count == 0 ? null : fields[^1];
        if (lastField != null)
        {
            var paddingOf = structInfo.SizeOf - sizeSoFar;
            if (paddingOf > 0)
            {
                if (lastHoleField == null || paddingOf < lastField.ByteWidthOf)
                {
                    lastField.PaddingOf = paddingOf;
                }
            }
        }

        foreach (var field in fields)
        {
            if (field.PaddingOf < 0)
            {
                var diagnostic =
                    new StructFieldNegativePaddingOfDiagnostic(field.Name, field.Location, field.PaddingOf!.Value);
                context.Diagnostics.Add(diagnostic);
            }

            if (field.PaddingOf == 0)
            {
                field.PaddingOf = null;
            }
        }
    }
}
