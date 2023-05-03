// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Features.BuildCLibrary.Input.Sanitized;

public class BuildCLibraryInput
{
    public string CMakeDirectoryPath { get; init; } = string.Empty;

    public string OutputDirectoryPath { get; init; } = string.Empty;

    public bool DeleteBuildFiles { get; init; } = true;
}
