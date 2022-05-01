// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Feature.ReadCodeC.Domain.ExploreCode;

public class ExploreOptions
{
    public ImmutableArray<string> HeaderFilesBlocked { get; init; }

    public ImmutableArray<string> OpaqueTypesNames { get; init; }

    public ImmutableArray<string> FunctionNamesAllowed { get; init; }

    public bool IsEnabledLocationFullPaths { get; init; }

    public bool IsEnabledMacroObjects { get; init; }

    public bool IsEnabledVariables{ get; init; }

    public bool IsEnabledAllowNamesWithPrefixedUnderscore { get; init; }

    public bool IsEnabledSystemDeclarations { get; init; }
}