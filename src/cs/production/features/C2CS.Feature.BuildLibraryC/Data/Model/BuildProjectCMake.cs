// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.BuildLibraryC.Data;

// NOTE: Properties are required for System.Text.Json serialization
// NOTE: This class is considered un-sanitized input; all strings and other types could be null.
public class BuildProjectCMake : BuildProject
{
    public string CMakeListsDirectoryPath { get; set; } = string.Empty;

    public BuildProjectCMake()
    {
        Type = BuildProjectType.CMake;
    }
}
