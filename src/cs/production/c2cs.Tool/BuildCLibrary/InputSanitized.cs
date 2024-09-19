// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.BuildCLibrary;

public class InputSanitized
{
    public string CMakeDirectoryPath { get; init; } = string.Empty;

    public string OutputDirectoryPath { get; init; } = string.Empty;

    public ImmutableArray<string> CMakeArguments { get; init; } = ImmutableArray<string>.Empty;

    public bool IsEnabledDeleteBuildFiles { get; init; } = true;

    public bool IsEnabledDebugBuild { get; init; }
}
