// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC;

public sealed class ExtractInput
{
    public string InputFilePath { get; init; } = string.Empty;

    public ImmutableArray<ExtractInputAbstractSyntaxTree> InputAbstractSyntaxTrees { get; set; }
}
