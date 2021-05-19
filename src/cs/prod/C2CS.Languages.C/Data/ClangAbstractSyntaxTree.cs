// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Languages.C
{
    public record ClangAbstractSyntaxTree
    {
        public readonly ImmutableArray<ClangFunctionExtern> FunctionExterns;
        public readonly ImmutableArray<ClangFunctionPointer> FunctionPointers;
        public readonly ImmutableArray<ClangRecord> Records;
        public readonly ImmutableArray<ClangEnum> Enums;
        public readonly ImmutableArray<ClangOpaqueType> OpaqueTypes;
        public readonly ImmutableArray<ClangTypedef> Typedefs;
        public readonly ImmutableArray<ClangVariable> Variables;

        public ClangAbstractSyntaxTree(
            ImmutableArray<ClangFunctionExtern> functionExternExterns,
            ImmutableArray<ClangFunctionPointer> functionPointers,
            ImmutableArray<ClangRecord> records,
            ImmutableArray<ClangEnum> enums,
            ImmutableArray<ClangOpaqueType> opaqueTypes,
            ImmutableArray<ClangTypedef> typedefs,
            ImmutableArray<ClangVariable> variables)
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
