// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Languages.C
{
    public record CPointerFunction : CNode
    {
        public readonly string Name;
        public readonly CType Type;
        public readonly CType ReturnType;
        public readonly ImmutableArray<CPointerFunctionParameter> Parameters;
        public readonly bool IsWrapped;

        internal CPointerFunction(
            string name,
            CCodeLocation codeLocation,
            CType type,
            CType returnType,
            ImmutableArray<CPointerFunctionParameter> parameters,
            bool isWrapped)
            : base(CNodeKind.PointerFunction, codeLocation)
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
