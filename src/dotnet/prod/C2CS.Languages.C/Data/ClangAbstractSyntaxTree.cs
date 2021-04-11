// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System.Collections.Immutable;

namespace C2CS.Languages.C
{
    public readonly struct ClangAbstractSyntaxTree
    {
        public readonly ImmutableArray<ClangFunctionExtern> FunctionExterns;
        public readonly ImmutableArray<ClangFunctionPointer> FunctionPointers;
        public readonly ImmutableArray<ClangRecord> Records;
        public readonly ImmutableArray<ClangEnum> Enums;
        public readonly ImmutableArray<ClangOpaqueDataType> OpaqueDataTypes;
        public readonly ImmutableArray<ClangOpaquePointer> OpaquePointers;
        public readonly ImmutableArray<ClangAliasDataType> AliasDataTypes;

        public ClangAbstractSyntaxTree(
            ImmutableArray<ClangFunctionExtern> functionExternExterns,
            ImmutableArray<ClangFunctionPointer> functionPointers,
            ImmutableArray<ClangRecord> records,
            ImmutableArray<ClangEnum> enums,
            ImmutableArray<ClangOpaqueDataType> opaqueDataTypes,
            ImmutableArray<ClangOpaquePointer> opaquePointers,
            ImmutableArray<ClangAliasDataType> aliasDataTypes)
        {
            FunctionExterns = functionExternExterns;
            FunctionPointers = functionPointers;
            Records = records;
            Enums = enums;
            OpaqueDataTypes = opaqueDataTypes;
            OpaquePointers = opaquePointers;
            AliasDataTypes = aliasDataTypes;
        }
    }
}
