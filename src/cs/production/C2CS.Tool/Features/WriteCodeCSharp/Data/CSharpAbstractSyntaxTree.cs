// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using bottlenoselabs.Common;

namespace C2CS.Features.WriteCodeCSharp.Data;

public sealed record CSharpAbstractSyntaxTree
{
    public ImmutableArray<TargetPlatform> Platforms { get; init; }

    public ImmutableArray<CSharpFunction> Functions { get; init; }

    public ImmutableArray<CSharpFunctionPointer> FunctionPointers { get; init; }

    public ImmutableArray<CSharpStruct> Structs { get; init; }

    public ImmutableArray<CSharpAliasType> AliasStructs { get; init; }

    public ImmutableArray<CSharpOpaqueType> OpaqueStructs { get; init; }

    public ImmutableArray<CSharpEnum> Enums { get; init; }

    public ImmutableArray<CSharpMacroObject> MacroObjects { get; init; }

    public ImmutableArray<CSharpConstant> Constants { get; init; }
}
