// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Data.C.Model;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.Contexts.ReadCodeC.Explore.Handlers;

public sealed class ArrayExplorer : ExploreHandler<CArray>
{
    public ArrayExplorer(ILogger<ArrayExplorer> logger)
        : base(logger, false)
    {
    }

    protected override ExploreKindCursors ExpectedCursors => ExploreKindCursors.Any;

    protected override ExploreKindTypes ExpectedTypes { get; } = ExploreKindTypes.Either(
        CXTypeKind.CXType_ConstantArray, CXTypeKind.CXType_IncompleteArray);

    public override CArray Explore(ExploreContext context, ExploreInfoNode info)
    {
        var array = Array(context, info);
        return array;
    }

    private static CArray Array(ExploreContext context, ExploreInfoNode info)
    {
        var type = clang_getElementType(info.Type);
        var typeInfo = context.VisitType(type, info)!;

        var result = new CArray
        {
            Name = info.Name,
            TypeInfo = typeInfo
        };
        return result;
    }
}
