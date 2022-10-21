// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Data.C.Model;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace C2CS.ReadCodeC.Explore.Handlers;

[UsedImplicitly]
public sealed class OpaqueTypeExplorer : ExploreHandler<COpaqueType>
{
    public OpaqueTypeExplorer(ILogger<OpaqueTypeExplorer> logger)
        : base(logger, false)
    {
    }

    protected override ExploreKindCursors ExpectedCursors => ExploreKindCursors.Any;

    protected override ExploreKindTypes ExpectedTypes => ExploreKindTypes.Any;

    public override COpaqueType Explore(ExploreContext context, ExploreInfoNode info)
    {
        var opaqueDataType = OpaqueDataType(info);
        return opaqueDataType;
    }

    private static COpaqueType OpaqueDataType(ExploreInfoNode info)
    {
        var result = new COpaqueType
        {
            Name = info.Name,
            Location = info.Location,
            SizeOf = info.SizeOf
        };

        return result;
    }
}
