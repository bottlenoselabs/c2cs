// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.CSharp
{
    public record CSharpFunctionExternParameter : CSharpCommon
    {
        public readonly CSharpType Type;
        public readonly bool IsReadOnly;
        public readonly bool IsFunctionPointer;

        public CSharpFunctionExternParameter(
            string name,
            string codeLocationComment,
            CSharpType type,
            bool isReadOnly,
            bool isFunctionPointer)
            : base(name, codeLocationComment)
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
