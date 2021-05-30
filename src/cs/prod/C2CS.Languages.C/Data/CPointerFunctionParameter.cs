// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Languages.C
{
    public record CPointerFunctionParameter : CNode
    {
        public readonly string Name;
        public readonly CType Type;

        internal CPointerFunctionParameter(
            string name,
            CCodeLocation codeLocation,
            CType type)
            : base(CNodeKind.PointerFunctionParameter, codeLocation)
        {
            Name = name;
            Type = type;
        }

        public override string ToString()
        {
            return $"FunctionPointerParameter '{Name}': {Type.Name} @ {CodeLocation.ToString()}";
        }
    }
}
