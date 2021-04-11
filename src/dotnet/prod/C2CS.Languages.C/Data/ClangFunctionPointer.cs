// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System.Collections.Immutable;

namespace C2CS.Languages.C
{
    public record ClangFunctionPointer : ClangCommon
    {
        public readonly int PointerSize;
        public readonly ClangType ReturnType;
        public readonly ImmutableArray<ClangFunctionPointerParameter> Parameters;

        internal ClangFunctionPointer(
            string name,
            ClangCodeLocation codeLocation,
            int pointerSize,
            ClangType returnType,
            ImmutableArray<ClangFunctionPointerParameter> parameters)
            : base(ClangKind.FunctionPointer, name, codeLocation)
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
