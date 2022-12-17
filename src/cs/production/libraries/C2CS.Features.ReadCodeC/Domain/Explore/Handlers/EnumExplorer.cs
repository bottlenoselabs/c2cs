// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Data.C.Model;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.ReadCodeC.Domain.Explore.Handlers;

[UsedImplicitly]
public sealed class EnumExplorer : ExploreNodeHandler<CEnum>
{
    public EnumExplorer(ILogger<EnumExplorer> logger)
        : base(logger, false)
    {
    }

    protected override ExploreKindCursors ExpectedCursors { get; } =
        ExploreKindCursors.Is(CXCursorKind.CXCursor_EnumDecl);

    protected override ExploreKindTypes ExpectedTypes { get; } = ExploreKindTypes.Is(CXTypeKind.CXType_Enum);

    public override CEnum Explore(ExploreContext context, ExploreInfoNode info)
    {
        var @enum = Enum(context, info);
        return @enum;
    }

    private CEnum Enum(ExploreContext context, ExploreInfoNode info)
    {
        var integerTypeInfo = IntegerTypeInfo(context, info);
        var enumValues = CreateEnumValues(info.Cursor);

        var result = new CEnum
        {
            Name = info.Name,
            Location = info.Location,
            IntegerTypeInfo = integerTypeInfo,
            Values = enumValues
        };
        return result;
    }

    private static CTypeInfo IntegerTypeInfo(ExploreContext context, ExploreInfoNode info)
    {
        var integerType = clang_getEnumDeclIntegerType(info.Cursor);
        var typeInfo = context.VisitType(integerType, info)!;
        return typeInfo;
    }

    private ImmutableArray<CEnumValue> CreateEnumValues(CXCursor cursor)
    {
        var builder = ImmutableArray.CreateBuilder<CEnumValue>();

        var enumValuesCursors = cursor.GetDescendents(
            (child, _) => child.kind == CXCursorKind.CXCursor_EnumConstantDecl,
            false);

        foreach (var enumValueCursor in enumValuesCursors)
        {
            var enumValue = CreateEnumValue(enumValueCursor);
            builder.Add(enumValue);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CEnumValue CreateEnumValue(CXCursor cursor, string? name = null)
    {
        var value = clang_getEnumConstantDeclValue(cursor);
        name ??= cursor.Name();

        var result = new CEnumValue
        {
            Name = name,
            Value = value
        };

        return result;
    }
}
