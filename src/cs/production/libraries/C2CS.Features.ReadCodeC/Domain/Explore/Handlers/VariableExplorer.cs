// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Data.C.Model;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.ReadCodeC.Domain.Explore.Handlers;

[UsedImplicitly]
public sealed class VariableExplorer : ExploreNodeHandler<CVariable>
{
    public VariableExplorer(ILogger<VariableExplorer> logger)
        : base(logger, false)
    {
    }

    protected override ExploreKindCursors ExpectedCursors { get; } =
        ExploreKindCursors.Is(CXCursorKind.CXCursor_VarDecl);

    protected override ExploreKindTypes ExpectedTypes => ExploreKindTypes.Any;

    protected override CVariable Explore(ExploreContext context, ExploreInfoNode info)
    {
        var variable = Variable(info);
        return variable;
    }

    private static CVariable Variable(ExploreInfoNode info)
    {
        var result = new CVariable
        {
            Location = info.Location,
            Name = info.Name,
            Type = info.TypeName
        };
        return result;
    }
}
