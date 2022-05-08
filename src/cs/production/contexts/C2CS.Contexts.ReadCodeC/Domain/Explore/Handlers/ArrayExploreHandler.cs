// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Contexts.ReadCodeC.Data.Model;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.Contexts.ReadCodeC.Domain.Explore.Handlers;

public sealed class ArrayExploreHandler : ExploreHandler<CArray>
{
    protected override ExploreKindCursors ExpectedCursors => ExploreKindCursors.Any;

    protected override ExploreKindTypes ExpectedTypes { get; } = ExploreKindTypes.Either(
        CXTypeKind.CXType_ConstantArray, CXTypeKind.CXType_IncompleteArray);

    public ArrayExploreHandler(ILogger<ArrayExploreHandler> logger)
        : base(logger)
    {
    }

    protected override bool CanVisit(ExploreContext context, ExploreInfoNode info)
    {
        return true;
    }

    public override CArray Explore(ExploreContext context, ExploreInfoNode info)
    {
        var array = Array(context, info);
        return array;
    }

    private static CArray Array(ExploreContext context, ExploreInfoNode info)
    {
        var type = clang_getElementType(info.Type);
        var typeInfo = context.VisitType(type, info.Parent);

        var result = new CArray
        {
            Name = info.Name,
            TypeInfo = typeInfo
        };
        return result;
    }
}
