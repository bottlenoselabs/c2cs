// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Contexts.ReadCodeC.Domain.Explore;

public class ExplorerOptions
{
    public ImmutableArray<string> HeaderFilesBlocked { get; init; } = ImmutableArray<string>.Empty;

    public ImmutableArray<string> OpaqueTypesNames { get; init; } = ImmutableArray<string>.Empty;

    public ImmutableArray<string> FunctionNamesAllowed { get; init; } = ImmutableArray<string>.Empty;

    public ImmutableArray<string> EnumConstantNamesAllowed { get; init; } = ImmutableArray<string>.Empty;

    public bool IsEnabledLocationFullPaths { get; init; }

    public bool IsEnabledFunctions { get; init; }

    public bool IsEnabledEnumConstants { get; set; }

    public bool IsEnabledVariables { get; init; }

    public bool IsEnabledEnumsDangling { get; init; }

    public bool IsEnabledAllowNamesWithPrefixedUnderscore { get; init; }

    public bool IsEnabledSystemDeclarations { get; init; }
}
