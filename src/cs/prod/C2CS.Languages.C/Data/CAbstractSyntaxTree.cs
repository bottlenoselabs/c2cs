// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Languages.C
{
    public record CAbstractSyntaxTree
    {
        public readonly ImmutableArray<CFunction> FunctionExterns;
        public readonly ImmutableArray<CPointerFunction> FunctionPointers;
        public readonly ImmutableArray<CRecord> Records;
        public readonly ImmutableArray<CEnum> Enums;
        public readonly ImmutableArray<COpaqueType> OpaqueTypes;
        public readonly ImmutableArray<CTypedef> Typedefs;
        public readonly ImmutableArray<CVariable> Variables;

        public CAbstractSyntaxTree(
            ImmutableArray<CFunction> functionExternExterns,
            ImmutableArray<CPointerFunction> functionPointers,
            ImmutableArray<CRecord> records,
            ImmutableArray<CEnum> enums,
            ImmutableArray<COpaqueType> opaqueTypes,
            ImmutableArray<CTypedef> typedefs,
            ImmutableArray<CVariable> variables)
        {
            FunctionExterns = functionExternExterns;
            FunctionPointers = functionPointers;
            Records = records;
            Enums = enums;
            OpaqueTypes = opaqueTypes;
            Typedefs = typedefs;
            Variables = variables;
        }
    }
}
