// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Immutable;

namespace C2CS.CSharp
{
    public readonly struct CSharpFunctionExtern
    {
        public readonly string Name;
        public readonly string OriginalCodeLocationComment;
        public readonly CSharpType ReturnType;
        public readonly CSharpFunctionExternCallingConvention CallingConvention;
        public readonly ImmutableArray<CSharpFunctionExternParameter> Parameters;

        public CSharpFunctionExtern(
            string name,
            string originalCodeLocationComment,
            CSharpFunctionExternCallingConvention callingConvention,
            CSharpType returnType,
            ImmutableArray<CSharpFunctionExternParameter> parameters)
        {
            Name = name;
            OriginalCodeLocationComment = originalCodeLocationComment;
            ReturnType = returnType;
            CallingConvention = callingConvention;
            Parameters = parameters;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
