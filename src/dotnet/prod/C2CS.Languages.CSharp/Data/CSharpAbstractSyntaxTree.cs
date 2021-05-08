// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.CSharp
{
    public readonly struct CSharpAbstractSyntaxTree
    {
        public readonly ImmutableArray<CSharpFunctionExtern> FunctionExterns;

        public readonly ImmutableArray<CSharpFunctionPointer> FunctionPointers;

        public readonly ImmutableArray<CSharpStruct> Structs;

        public readonly ImmutableArray<CSharpOpaqueDataType> OpaqueDataTypes;

        public readonly ImmutableArray<CSharpEnum> Enums;

        public readonly ImmutableArray<CSharpVariable> VariablesExtern;

        public CSharpAbstractSyntaxTree(
            ImmutableArray<CSharpFunctionExtern> functionExterns,
            ImmutableArray<CSharpFunctionPointer> functionPointers,
            ImmutableArray<CSharpStruct> structs,
            ImmutableArray<CSharpOpaqueDataType> opaqueDataTypes,
            ImmutableArray<CSharpEnum> enums,
            ImmutableArray<CSharpVariable> variablesExtern)
        {
            FunctionExterns = functionExterns;
            FunctionPointers = functionPointers;
            Structs = structs;
            OpaqueDataTypes = opaqueDataTypes;
            Enums = enums;
            VariablesExtern = variablesExtern;
        }
    }
}
