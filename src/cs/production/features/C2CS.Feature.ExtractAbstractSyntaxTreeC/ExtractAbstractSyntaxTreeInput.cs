// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC;

public sealed class ExtractAbstractSyntaxTreeInput
{
    public string InputFilePath { get; init; } = string.Empty;

    public string OutputFilePath { get; init; } = string.Empty;

    public bool IsEnabledFindSdk { get; init; }

    public RuntimePlatform TargetPlatform { get; init; }

    public ImmutableArray<string> IncludeDirectories { get; init; }

    public ImmutableArray<string> ExcludedHeaderFiles { get; init; }

    public ImmutableArray<string> OpaqueTypeNames { get; init; }

    public ImmutableArray<string> FunctionNamesWhitelist { get; init; }

    public ImmutableArray<string> ClangDefines { get; init; }

    public ImmutableArray<string> ClangArguments { get; init; }
}
