// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Contexts.ReadCodeC.Data.Model;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.Contexts.ReadCodeC.Domain.Explore.Handlers;

[UsedImplicitly]
public sealed class VariableExploreHandler : ExploreHandler<CVariable>
{
    public VariableExploreHandler(ILogger<VariableExploreHandler> logger)
        : base(logger)
    {
    }

    protected override ExploreKindCursors ExpectedCursors { get; } = ExploreKindCursors.Is(CXCursorKind.CXCursor_VarDecl);

    protected override ExploreKindTypes ExpectedTypes => ExploreKindTypes.Any;

    public override CVariable? Explore(ExploreContext context, ExploreInfoNode info)
    {
        if (info.Parent != null)
        {
            LogFailureUnexpectedParent(info.Parent.Name);
            return null;
        }

        if (!IsAllowed(context))
        {
            return null;
        }

        var variable = Variable(info);
        return variable;
    }

    private static bool IsAllowed(ExploreContext context)
    {
        if (!context.Options.IsEnabledVariables)
        {
            return false;
        }

        return true;
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
