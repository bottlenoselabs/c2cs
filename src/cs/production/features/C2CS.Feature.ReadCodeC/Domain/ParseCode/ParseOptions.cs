// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Feature.ReadCodeC.Domain.ParseCode;

public class ParseOptions
{
    public ImmutableArray<string> UserIncludeDirectories { get; init; }

    public ImmutableArray<string> SystemIncludeDirectories { get; init; }

    public ImmutableArray<string> MacroObjectsDefines { get; init; }

    public ImmutableArray<string> AdditionalArguments { get; init; }

    public bool IsEnabledFindSystemHeaders { get; init; }

    public ImmutableArray<string> Frameworks { get; init; }
}
