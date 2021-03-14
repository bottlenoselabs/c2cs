// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Immutable;
using ClangSharp.Interop;

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct GenericCodeAbstractSyntaxTree
    {
        public readonly ImmutableArray<GenericCodeFunctionExtern> Functions;

        public readonly ImmutableArray<GenericCodeStruct> Structs;

        public readonly ImmutableArray<GenericCodeEnum> Enums;

        public readonly ImmutableArray<GenericCodeOpaqueType> OpaqueTypes;

        public readonly ImmutableArray<GenericCodeForwardType> ForwardTypes;

        public readonly ImmutableArray<GenericCodeFunctionPointer> FunctionPointers;

        public readonly ImmutableArray<CXCursor> SystemTypes;

        public readonly ImmutableDictionary<CXCursor, string> NamesByCursor;

        public GenericCodeAbstractSyntaxTree(
            ImmutableArray<GenericCodeFunctionExtern> functions,
            ImmutableArray<GenericCodeStruct> structs,
            ImmutableArray<GenericCodeEnum> enums,
            ImmutableArray<GenericCodeOpaqueType> opaqueTypes,
            ImmutableArray<GenericCodeForwardType> forwardTypes,
            ImmutableArray<GenericCodeFunctionPointer> functionPointers,
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
