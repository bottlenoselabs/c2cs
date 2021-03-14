// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System.Collections.Immutable;

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct GenericCodeFunctionExtern
    {
        public readonly string Name;
        public readonly GenericCodeInfo Info;
        public readonly GenericCodeType ReturnType;
        public readonly GenericCodeFunctionCallingConvention CallingConvention;
        public readonly ImmutableArray<GenericCodeFunctionParameter> Parameters;

        public GenericCodeFunctionExtern(
            string name,
            GenericCodeInfo info,
            GenericCodeType returnType,
            GenericCodeFunctionCallingConvention callingConvention,
            ImmutableArray<GenericCodeFunctionParameter> parameters)
        {
            Name = name;
            Info = info;
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
