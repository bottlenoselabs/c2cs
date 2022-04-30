// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Feature.ReadCodeC.Domain.ExploreCode;
using C2CS.Feature.ReadCodeC.Domain.ParseCode;

namespace C2CS.Feature.ReadCodeC.Domain;

public sealed class ReadCodeCAbstractSyntaxTreeOptions
{
    public string OutputFilePath { get; init; } = string.Empty;

    public TargetPlatform TargetPlatform { get; init; }

    public ExploreOptions ExploreOptions { get; init; } = null!;

    public ParseOptions ParseOptions { get; init; } = null!;
}
