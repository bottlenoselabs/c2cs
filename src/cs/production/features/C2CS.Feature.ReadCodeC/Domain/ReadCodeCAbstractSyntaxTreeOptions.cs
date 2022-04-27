// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Feature.ReadCodeC.Domain;

public sealed class ReadCodeCAbstractSyntaxTreeOptions
{
    public string OutputFilePath { get; init; } = string.Empty;

    public TargetPlatform TargetPlatform { get; init; }

    public ImmutableArray<string> SystemIncludeDirectories { get; init; }

    public ImmutableArray<string> IncludeDirectories { get; init; }

    public ImmutableArray<string> ExcludedHeaderFiles { get; init; }

    public ImmutableArray<string> OpaqueTypeNames { get; init; }

    public ImmutableArray<string> FunctionNamesWhitelist { get; init; }

    public ImmutableArray<string> ClangDefines { get; init; }

    public ImmutableArray<string> ClangArguments { get; init; }

    public bool IsEnabledLocationFullPaths { get; init; }

    public bool IsEnabledMacroObjects { get; init; }
}
