// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Contexts.WriteCodeCSharp.Data.Model;

public sealed record CSharpNodes
{
    public ImmutableArray<CSharpFunction> Functions { get; set; }

    public ImmutableArray<CSharpFunctionPointer> FunctionPointers { get; set; }

    public ImmutableArray<CSharpStruct> Structs { get; set; }

    public ImmutableArray<CSharpAliasStruct> AliasStructs { get; set; }

    public ImmutableArray<CSharpOpaqueStruct> OpaqueStructs { get; set; }

    public ImmutableArray<CSharpEnum> Enums { get; set; }

    public ImmutableArray<CSharpConstant> Constants { get; set; }
}
