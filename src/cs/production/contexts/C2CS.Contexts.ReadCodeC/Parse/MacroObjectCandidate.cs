// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Data.C.Model;

namespace C2CS.Contexts.ReadCodeC.Parse;

public class MacroObjectCandidate
{
    public string Name { get; init; } = string.Empty;

    public CLocation Location { get; init; } = CLocation.NoLocation;

    public ImmutableArray<string> Tokens { get; init; } = ImmutableArray<string>.Empty;
}
