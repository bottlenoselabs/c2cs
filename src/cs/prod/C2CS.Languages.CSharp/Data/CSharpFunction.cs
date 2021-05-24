// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.CSharp
{
    public record CSharpFunction : CSharpNode
    {
        public readonly CSharpType ReturnType;
        public readonly CSharpFunctionCallingConvention CallingConvention;
        public readonly ImmutableArray<CSharpFunctionParameter> Parameters;

        public CSharpFunction(
            string name,
            string codeLocationComment,
            CSharpFunctionCallingConvention callingConvention,
            CSharpType returnType,
            ImmutableArray<CSharpFunctionParameter> parameters)
            : base(name, codeLocationComment)
        {
            ReturnType = returnType;
            CallingConvention = callingConvention;
            Parameters = parameters;
        }

        // Required for debugger string with records
        // ReSharper disable once RedundantOverriddenMember
        public override string ToString()
        {
            return base.ToString();
        }
    }
}
