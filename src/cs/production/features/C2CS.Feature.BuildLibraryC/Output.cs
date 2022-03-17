// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.BuildLibraryC.Data.Model;

namespace C2CS.Feature.BuildLibraryC;

public class Output : UseCaseOutput
{
    public ImmutableArray<BuildTargetResult> BuildTargetResults { get; }
}
