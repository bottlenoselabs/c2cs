// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.SourceGenerator;

public class BindgenAttributes
{
    public BindgenAttribute Bindgen { get; set; } = null!;

    public ImmutableArray<BindgenTargetPlatformAttribute> TargetPlatforms { get; set; } = ImmutableArray<BindgenTargetPlatformAttribute>.Empty;

    public ImmutableArray<BindgenFunctionAttribute> Functions { get; set; } = ImmutableArray<BindgenFunctionAttribute>.Empty;
}
