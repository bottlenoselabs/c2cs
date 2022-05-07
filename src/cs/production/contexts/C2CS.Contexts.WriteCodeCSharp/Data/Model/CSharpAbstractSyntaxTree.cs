// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Contexts.WriteCodeCSharp.Data.Model;

public sealed record CSharpAbstractSyntaxTree
{
    public CSharpNodes PlatformAgnosticNodes { get; init; } = null!;

    public ImmutableArray<(TargetPlatform Platform, CSharpNodes Nodes)> PlatformSpecificNodes { get; init; } =
        ImmutableArray<(TargetPlatform Platform, CSharpNodes Nodes)>.Empty;
}
