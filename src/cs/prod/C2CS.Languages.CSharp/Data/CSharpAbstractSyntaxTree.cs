// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.CSharp
{
    public readonly struct CSharpAbstractSyntaxTree
    {
        public readonly ImmutableArray<CSharpFunction> FunctionExterns;

        public readonly ImmutableArray<CSharpPointerFunction> FunctionPointers;

        public readonly ImmutableArray<CSharpStruct> Structs;

        public readonly ImmutableArray<CSharpTypedef> Typedefs;

        public readonly ImmutableArray<CSharpOpaqueType> OpaqueDataTypes;

        public readonly ImmutableArray<CSharpEnum> Enums;

        public readonly ImmutableArray<CSharpVariable> VariablesExtern;

        public CSharpAbstractSyntaxTree(
            ImmutableArray<CSharpFunction> functionExterns,
            ImmutableArray<CSharpPointerFunction> functionPointers,
            ImmutableArray<CSharpStruct> structs,
            ImmutableArray<CSharpTypedef> typedefs,
            ImmutableArray<CSharpOpaqueType> opaqueDataTypes,
            ImmutableArray<CSharpEnum> enums,
            ImmutableArray<CSharpVariable> variablesExtern)
        {
            FunctionExterns = functionExterns;
            FunctionPointers = functionPointers;
            Structs = structs;
            Typedefs = typedefs;
            OpaqueDataTypes = opaqueDataTypes;
            Enums = enums;
            VariablesExtern = variablesExtern;
        }
    }
}
