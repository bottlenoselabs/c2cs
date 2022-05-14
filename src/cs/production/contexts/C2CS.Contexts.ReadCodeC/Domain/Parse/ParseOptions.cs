// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Contexts.ReadCodeC.Domain.Parse;

public class ParseOptions
{
    public ImmutableArray<string> UserIncludeDirectories { get; set; }

    public ImmutableArray<string> SystemIncludeDirectories { get; set; }

    public ImmutableArray<string> MacroObjectsDefines { get; set; }

    public ImmutableArray<string> AdditionalArguments { get; set; }

    public bool IsEnabledFindSystemHeaders { get; set; }

    public ImmutableArray<string> Frameworks { get; set; }
}
