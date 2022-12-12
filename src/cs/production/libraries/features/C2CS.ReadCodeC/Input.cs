// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.ReadCodeC;

public sealed class Input
{
    public string InputFilePath { get; init; } = string.Empty;

    public ImmutableArray<InputAbstractSyntaxTree> AbstractSyntaxTreesOptionsList { get; set; }
}
