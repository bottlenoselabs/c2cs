// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Languages.C
{
    public record ClangFunctionExtern : ClangCommon
    {
        public readonly ClangFunctionExternCallingConvention CallingConvention;
        public readonly ClangType ReturnType;
        public readonly ImmutableArray<ClangFunctionExternParameter> Parameters;

        internal ClangFunctionExtern(
            string name,
            ClangCodeLocation codeLocation,
            ClangFunctionExternCallingConvention callingConvention,
            ClangType returnType,
            ImmutableArray<ClangFunctionExternParameter> parameters)
            : base(ClangKind.FunctionExtern, name, codeLocation)
        {
            CallingConvention = callingConvention;
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
