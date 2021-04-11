// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System.Collections.Immutable;

namespace C2CS.CSharp
{
    public record CSharpFunctionPointer : CSharpCommon
    {
        public readonly int PointerSize;
        public readonly CSharpType ReturnType;
        public readonly ImmutableArray<CSharpFunctionPointerParameter> Parameters;

        public CSharpFunctionPointer(
            string name,
            string codeLocationComment,
            int pointerSize,
            CSharpType returnType,
            ImmutableArray<CSharpFunctionPointerParameter> parameters)
            : base(name, codeLocationComment)
        {
            PointerSize = pointerSize;
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
