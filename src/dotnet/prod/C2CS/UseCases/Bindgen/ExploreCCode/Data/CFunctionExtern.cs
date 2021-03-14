// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System.Collections.Immutable;

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct CFunctionExtern
    {
        public readonly string Name;
        public readonly CInfo Info;
        public readonly CType ReturnType;
        public readonly CFunctionCallingConvention CallingConvention;
        public readonly ImmutableArray<CFunctionParameter> Parameters;

        public CFunctionExtern(
            string name,
            CInfo info,
            CType returnType,
            CFunctionCallingConvention callingConvention,
            ImmutableArray<CFunctionParameter> parameters)
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
