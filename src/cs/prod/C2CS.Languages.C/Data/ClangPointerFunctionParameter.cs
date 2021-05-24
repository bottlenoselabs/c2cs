// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Languages.C
{
    public record ClangPointerFunctionParameter : ClangNode
    {
        public readonly string Name;
        public readonly ClangType Type;

        internal ClangPointerFunctionParameter(
            string name,
            ClangCodeLocation codeLocation,
            ClangType type)
            : base(ClangNodeKind.PointerFunctionParameter, codeLocation)
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
