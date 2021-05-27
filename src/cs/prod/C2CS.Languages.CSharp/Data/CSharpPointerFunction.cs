// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.CSharp
{
    public record CSharpPointerFunction : CSharpNode
    {
        public readonly bool IsBuiltIn;
        public readonly CSharpType ReturnType;
        public readonly ImmutableArray<CSharpPointerFunctionParameter> Parameters;

        public CSharpPointerFunction(
            string name,
            bool isBuiltIn,
            string codeLocationComment,
            CSharpType returnType,
            ImmutableArray<CSharpPointerFunctionParameter> parameters)
            : base(name, codeLocationComment)
        {
            IsBuiltIn = isBuiltIn;
            ReturnType = returnType;
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
