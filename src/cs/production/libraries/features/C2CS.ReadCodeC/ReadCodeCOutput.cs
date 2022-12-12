// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Foundation.UseCases;

namespace C2CS.ReadCodeC;

public sealed class ReadCodeCOutput : UseCaseOutput<ReadCodeCInput>
{
    public ImmutableArray<ReadCodeCAbstractSyntaxTreeInput> AbstractSyntaxTreesOptions { get; set; }
}
