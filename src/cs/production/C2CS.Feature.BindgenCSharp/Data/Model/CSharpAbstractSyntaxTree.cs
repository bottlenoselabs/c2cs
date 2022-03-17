// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Feature.BindgenCSharp.Data.Model;

public record CSharpAbstractSyntaxTree
{
    public CSharpNodes PlatformAgnosticNodes { get; init; } = null!;

    public ImmutableArray<(RuntimePlatform Platform, CSharpNodes Nodes)> PlatformSpecificNodes { get; init; } =
        ImmutableArray<(RuntimePlatform Platform, CSharpNodes Nodes)>.Empty;
}
