// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.UseCases.BindgenCSharp;

public class CSharpAbstractSyntaxTree
{
    public ImmutableArray<CSharpFunction> FunctionExterns { get; init; }

    public ImmutableArray<CSharpFunctionPointer> FunctionPointers { get; init; }

    public ImmutableArray<CSharpStruct> Structs { get; init; }

    public ImmutableArray<CSharpTypedef> Typedefs { get; init; }

    public ImmutableArray<CSharpOpaqueType> OpaqueDataTypes { get; init; }

    public ImmutableArray<CSharpEnum> Enums { get; init; }

    public ImmutableArray<CSharpPseudoEnum> PseudoEnums { get; init; }

    public ImmutableArray<CSharpConstant> Constants { get; init; }
}
