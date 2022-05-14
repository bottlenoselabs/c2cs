// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Contexts.ReadCodeC.Domain.Explore;
using C2CS.Contexts.ReadCodeC.Domain.Parse;

namespace C2CS.Contexts.ReadCodeC.Domain;

public sealed class ReadCodeCAbstractSyntaxTreeOptions
{
    public string OutputFilePath { get; set; } = string.Empty;

    public TargetPlatform TargetPlatform { get; set; }

    public ExplorerOptions ExplorerOptions { get; set; } = null!;

    public ParseOptions ParseOptions { get; set; } = null!;
}
