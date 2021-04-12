// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Languages.C
{
    public record ClangFunctionExternParameter : ClangCommon
    {
        public readonly ClangType Type;
        public bool IsReadOnly;
        public bool IsFunctionPointer;

        internal ClangFunctionExternParameter(
            string name,
            ClangCodeLocation codeLocation,
            ClangType type,
            bool isReadOnly,
            bool isFunctionPointer)
            : base(ClangKind.FunctionExternParameter, name, codeLocation)
        {
            Type = type;
            IsReadOnly = isReadOnly;
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
