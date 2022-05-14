// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Contexts.ReadCodeC.Domain.Explore;

public class ExplorerOptions
{
    public ImmutableArray<string> HeaderFilesBlocked { get; set; }

    public ImmutableArray<string> OpaqueTypesNames { get; set; }

    public ImmutableArray<string> FunctionNamesAllowed { get; set; }

    public bool IsEnabledLocationFullPaths { get; set; }

    public bool IsEnabledMacroObjects { get; set; }

    public bool IsEnabledFunctions { get; set; }

    public bool IsEnabledVariables { get; set; }

    public bool IsEnabledEnumsDangling { get; set; }

    public bool IsEnabledAllowNamesWithPrefixedUnderscore { get; set; }

    public bool IsEnabledSystemDeclarations { get; set; }
}
