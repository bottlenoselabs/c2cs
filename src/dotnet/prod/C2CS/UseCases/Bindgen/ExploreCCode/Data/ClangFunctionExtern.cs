// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System.Collections.Immutable;
using ClangSharp.Interop;

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct ClangFunctionExtern
    {
        public readonly string Name;
        public readonly CXCursor Cursor;
        public readonly CXType ReturnType;
        public readonly ImmutableArray<ClangFunctionParameter> Parameters;

        public ClangFunctionExtern(
            string name,
            CXCursor cursor,
            CXType returnType,
            ImmutableArray<ClangFunctionParameter> parameters)
        {
            Name = name;
            Cursor = cursor;
            ReturnType = returnType;
            Parameters = parameters;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
