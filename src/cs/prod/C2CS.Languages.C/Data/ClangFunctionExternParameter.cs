// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Languages.C
{
    public record ClangFunctionExternParameter : ClangNode
    {
        public readonly ClangType Type;
        public readonly bool IsFunctionPointer;

        internal ClangFunctionExternParameter(
            string name,
            ClangCodeLocation codeLocation,
            ClangType type,
            bool isFunctionPointer)
            : base(ClangNodeKind.FunctionExternParameter, name, codeLocation)
        {
            Type = type;
            IsFunctionPointer = isFunctionPointer;
        }

        // Required for debugger string with records
        // ReSharper disable once RedundantOverriddenMember
        public override string ToString()
        {
            return base.ToString();
        }
    }
}
