// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.BuildLibraryC.Data;

public class BuildTargetResult
{
    public BuildTarget BuildTarget { get; }

    public string LibraryFilePath { get; }

    public BuildTargetResult(
        BuildTarget buildTarget,
        string libraryFilePath)
    {
        BuildTarget = buildTarget;
        LibraryFilePath = libraryFilePath;
    }
}
