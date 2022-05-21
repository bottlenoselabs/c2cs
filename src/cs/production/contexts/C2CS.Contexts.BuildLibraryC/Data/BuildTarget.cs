// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Contexts.BuildLibraryC.Data;

// NOTE: Properties are required for System.Text.Json serialization
// NOTE: This class is considered un-sanitized input; all strings and other types could be null.
public class BuildTarget
{
    public NativeOperatingSystem OperatingSystem { get; set; } = NativeOperatingSystem.Unknown;

    public ImmutableArray<NativeArchitecture> TargetArchitectures { get; set; } = ImmutableArray<NativeArchitecture>.Empty;

    public bool IsEnabledCombineArchitectures { get; set; }

    public string OutputDirectoryPath { get; set; } = string.Empty;
}
