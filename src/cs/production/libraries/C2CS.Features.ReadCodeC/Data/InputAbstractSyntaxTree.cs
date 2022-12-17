// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.ReadCodeC.Domain.Explore;
using C2CS.ReadCodeC.Domain.Parse;

namespace C2CS.ReadCodeC.Data;

public sealed class InputAbstractSyntaxTree
{
    public string OutputFilePath { get; init; } = string.Empty;

    public TargetPlatform TargetPlatform { get; init; }

    public ExploreOptions ExplorerOptions { get; init; } = null!;

    public ParseOptions ParseOptions { get; init; } = null!;
}
