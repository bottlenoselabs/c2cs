// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Contexts.ReadCodeC.Parse;

public class ClangArgumentsBuilderResult
{
    public readonly ImmutableArray<string> Arguments;
    public readonly ImmutableDictionary<string, string> LinkedPaths;

    public ClangArgumentsBuilderResult(
        ImmutableArray<string> arguments,
        ImmutableDictionary<string, string> linkedPaths)
    {
        Arguments = arguments;
        LinkedPaths = linkedPaths;
    }
}
