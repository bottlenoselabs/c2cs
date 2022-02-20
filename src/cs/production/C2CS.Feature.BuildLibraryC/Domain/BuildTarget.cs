// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Feature.BuildLibraryC.Domain;

public record BuildTarget
{
    public RuntimeOperatingSystem OperatingSystem { get; init; }

    public ImmutableArray<RuntimeArchitecture> TargetArchitectures { get; init; }

    public bool IsEnabledCombineTargetArchitectures { get; init; }
}
