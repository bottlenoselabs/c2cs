// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Languages.C
{
    public record ClangFunctionParameter : ClangNode
    {
        public readonly string Name;
        public readonly ClangType Type;

        internal ClangFunctionParameter(
            string name,
            ClangCodeLocation codeLocation,
            ClangType type)
            : base(ClangNodeKind.FunctionParameter, codeLocation)
        {
            Name = name;
            Type = type;
        }

        public override string ToString()
        {
            return $"FunctionExternParameter '{Name}': {Type.Name} @ {CodeLocation.ToString()}";
        }
    }
}
