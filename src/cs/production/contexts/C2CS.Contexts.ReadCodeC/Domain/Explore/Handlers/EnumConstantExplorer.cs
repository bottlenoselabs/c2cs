// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Globalization;
using C2CS.Contexts.ReadCodeC.Data.Model;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.Contexts.ReadCodeC.Domain.Explore.Handlers;

public class EnumConstantExplorer : ExploreHandler<CEnumConstant>
{
    protected override ExploreKindCursors ExpectedCursors { get; } =
        ExploreKindCursors.Is(CXCursorKind.CXCursor_EnumConstantDecl);

    protected override ExploreKindTypes ExpectedTypes { get; } =
        ExploreKindTypes.Either(CXTypeKind.CXType_Int, CXTypeKind.CXType_UInt, CXTypeKind.CXType_ULong);

    public EnumConstantExplorer(
        ILogger<EnumConstantExplorer> logger)
        : base(logger, false)
    {
    }

    protected override bool CanVisit(ExploreContext context, string name, ExploreInfoNode? parentInfo)
    {
        if (!context.ExploreOptions.IsEnabledEnumConstants)
        {
            return false;
        }

        var namedAllowed = context.ExploreOptions.EnumConstantNamesAllowed;
        return namedAllowed.IsDefaultOrEmpty || namedAllowed.Contains(name);
    }

    public override CEnumConstant Explore(ExploreContext context, ExploreInfoNode info)
    {
        var enumConstant = EnumConstant(context, info);
        return enumConstant;
    }

    private CEnumConstant EnumConstant(ExploreContext context, ExploreInfoNode info)
    {
        var typeInfo = context.VisitType(info.Type, info.Parent)!;
        var value = clang_getEnumConstantDeclValue(info.Cursor).ToString(CultureInfo.InvariantCulture);

        var result = new CEnumConstant
        {
            Name = info.Name,
            Location = info.Location == CLocation.NoLocation ? info.Parent!.Location : info.Location,
            Type = typeInfo,
            Value = value
        };
        return result;
    }
}
