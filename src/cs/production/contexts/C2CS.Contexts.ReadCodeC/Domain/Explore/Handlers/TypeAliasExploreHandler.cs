// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Contexts.ReadCodeC.Data.Model;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.Contexts.ReadCodeC.Domain.Explore.Handlers;

[UsedImplicitly]
public class TypeAliasExploreHandler : ExploreHandler<CTypeAlias>
{
    protected override ExploreKindCursors ExpectedCursors { get; } = ExploreKindCursors.Is(CXCursorKind.CXCursor_TypedefDecl);

    protected override ExploreKindTypes ExpectedTypes { get; } = ExploreKindTypes.Is(CXTypeKind.CXType_Typedef);

    public TypeAliasExploreHandler(ILogger<TypeAliasExploreHandler> logger)
        : base(logger)
    {
    }

    protected override bool CanVisit(ExploreContext context, ExploreInfoNode info)
    {
        return true;
    }

    public override CTypeAlias Explore(ExploreContext context, ExploreInfoNode info)
    {
        var typeAlias = TypeAlias(context, info);
        return typeAlias;
    }

    private static CTypeAlias TypeAlias(ExploreContext context, ExploreInfoNode info)
    {
        var aliasType = clang_getTypedefDeclUnderlyingType(info.Cursor);
        var aliasTypeInfo = context.VisitType(aliasType, info);

        var typedef = new CTypeAlias
        {
            Name = info.Name,
            Location = info.Location,
            UnderlyingTypeInfo = aliasTypeInfo
        };
        return typedef;
    }
}
