// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Languages.C
{
    public record ClangPointerFunction : ClangNode
    {
        public readonly string Name;
        public readonly ClangType Type;
        public readonly ClangType ReturnType;
        public readonly ImmutableArray<ClangPointerFunctionParameter> Parameters;
        public readonly bool IsWrapped;

        internal ClangPointerFunction(
            string name,
            ClangCodeLocation codeLocation,
            ClangType type,
            ClangType returnType,
            ImmutableArray<ClangPointerFunctionParameter> parameters,
            bool isWrapped)
            : base(ClangNodeKind.PointerFunction, codeLocation)
        {
            Name = name;
            Type = type;
            ReturnType = returnType;
            Parameters = parameters;
            IsWrapped = isWrapped;
        }

        public override string ToString()
        {
            return $"FunctionPointer {Type.Name} @ {CodeLocation.ToString()}";
        }
    }
}
