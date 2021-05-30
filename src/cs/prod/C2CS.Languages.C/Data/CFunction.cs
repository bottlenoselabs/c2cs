// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Languages.C
{
    public record CFunction : CNode
    {
        public readonly string Name;
        public readonly CFunctionCallingConvention CallingConvention;
        public readonly CType ReturnType;
        public readonly ImmutableArray<CFunctionParameter> Parameters;

        internal CFunction(
            string name,
            CCodeLocation codeLocation,
            CFunctionCallingConvention callingConvention,
            CType returnType,
            ImmutableArray<CFunctionParameter> parameters)
            : base(CNodeKind.Function, codeLocation)
        {
            Name = name;
            CallingConvention = callingConvention;
            ReturnType = returnType;
            Parameters = parameters;
        }

        public override string ToString()
        {
            return $"FunctionExtern '{Name}' @ {CodeLocation.ToString()}";
        }
    }
}
