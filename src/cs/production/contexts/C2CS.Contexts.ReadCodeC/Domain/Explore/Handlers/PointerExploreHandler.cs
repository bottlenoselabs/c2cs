// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Contexts.ReadCodeC.Data.Model;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.Contexts.ReadCodeC.Domain.Explore.Handlers;

[UsedImplicitly]
public sealed class PointerExploreHandler : ExploreHandler<CPointer>
{
    protected override ExploreKindCursors ExpectedCursors => ExploreKindCursors.Any;

    protected override ExploreKindTypes ExpectedTypes { get; } = ExploreKindTypes.Is(CXTypeKind.CXType_Pointer);

    public PointerExploreHandler(ILogger<PointerExploreHandler> logger)
        : base(logger, false)
    {
    }

    public override CNode Explore(ExploreContext context, ExploreInfoNode info)
    {
        var pointer = Pointer(context, info);
        return pointer;
    }

    private static CPointer Pointer(ExploreContext context, ExploreInfoNode info)
    {
        var type = clang_getPointeeType(info.Type);
        var typeInfo = context.VisitType(type, info.Parent)!;

        var result = new CPointer
        {
            Name = info.Name,
            TypeInfo = typeInfo
        };
        return result;
    }
}
