// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Languages.C
{
    public record ClangFunction : ClangNode
    {
        public readonly string Name;
        public readonly ClangFunctionCallingConvention CallingConvention;
        public readonly ClangType ReturnType;
        public readonly ImmutableArray<ClangFunctionParameter> Parameters;

        internal ClangFunction(
            string name,
            ClangCodeLocation codeLocation,
            ClangFunctionCallingConvention callingConvention,
            ClangType returnType,
            ImmutableArray<ClangFunctionParameter> parameters)
            : base(ClangNodeKind.Function, codeLocation)
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
