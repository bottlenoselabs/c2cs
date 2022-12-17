// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Foundation.Executors;

namespace C2CS.ReadCodeC.Data;

public sealed class Output : ExecutorOutput<Input>
{
    public ImmutableArray<InputAbstractSyntaxTree> AbstractSyntaxTrees { get; set; }
}
