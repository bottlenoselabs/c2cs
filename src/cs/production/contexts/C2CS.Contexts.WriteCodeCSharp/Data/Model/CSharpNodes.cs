// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Contexts.WriteCodeCSharp.Data.Model;

public sealed record CSharpNodes
{
    public ImmutableArray<CSharpFunction> Functions { get; init; }

    public ImmutableArray<CSharpFunctionPointer> FunctionPointers { get; init; }

    public ImmutableArray<CSharpStruct> Structs { get; init; }

    public ImmutableArray<CSharpAliasStruct> AliasStructs { get; init; }

    public ImmutableArray<CSharpOpaqueStruct> OpaqueStructs { get; init; }

    public ImmutableArray<CSharpEnum> Enums { get; init; }

    public ImmutableArray<CSharpMacroObject> MacroObjects { get; init; }

    public ImmutableArray<CSharpEnumConstant> EnumConstants { get; init; }
}
