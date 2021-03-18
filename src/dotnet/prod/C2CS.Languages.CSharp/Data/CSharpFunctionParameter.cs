// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

namespace C2CS.CSharp
{
    public record CSharpFunctionParameter : CSharpCommon
    {
        public readonly CSharpType Type;
        public readonly bool IsReadOnly;

        public CSharpFunctionParameter(
            string name,
            string originalCodeLocationComment,
            CSharpType type,
            bool isReadOnly)
            : base(name, originalCodeLocationComment)
        {
            Type = type;
            IsReadOnly = isReadOnly;
        }

        // Required for debugger string with records
        // ReSharper disable once RedundantOverriddenMember
        public override string ToString()
        {
            return base.ToString();
        }
    }
}
