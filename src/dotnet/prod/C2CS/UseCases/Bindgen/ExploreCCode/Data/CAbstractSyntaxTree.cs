// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System.Collections.Immutable;
using ClangSharp.Interop;

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct CAbstractSyntaxTree
    {
        public readonly ImmutableArray<CFunction> Functions;

        public readonly ImmutableArray<CStruct> Structs;

        public readonly ImmutableArray<CEnum> Enums;

        public readonly ImmutableArray<COpaqueType> OpaqueTypes;

        public readonly ImmutableArray<CXCursor> ForwardTypes;

        public readonly ImmutableArray<CFunctionPointer> FunctionPointers;

        public readonly ImmutableArray<CXCursor> SystemTypes;

        public readonly ImmutableDictionary<CXCursor, string> NamesByCursor;

        public CAbstractSyntaxTree(
            ImmutableArray<CFunction> functions,
            ImmutableArray<CStruct> structs,
            ImmutableArray<CEnum> enums,
            ImmutableArray<COpaqueType> opaqueTypes,
            ImmutableArray<CXCursor> forwardTypes,
            ImmutableArray<CFunctionPointer> functionPointers,
            ImmutableArray<CXCursor> systemTypes,
            ImmutableDictionary<CXCursor, string> namesByCursor)
        {
            Functions = functions;
            Structs = structs;
            Enums = enums;
            OpaqueTypes = opaqueTypes;
            ForwardTypes = forwardTypes;
            FunctionPointers = functionPointers;
            SystemTypes = systemTypes;
            NamesByCursor = namesByCursor;
        }
    }
}
