// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Contexts.ReadCodeC.Domain;

public sealed class ReadCodeCInput
{
    public string InputFilePath { get; init; } = string.Empty;

    public ImmutableArray<ReadCodeCAbstractSyntaxTreeOptions> AbstractSyntaxTreesOptionsList { get; set; }
}
