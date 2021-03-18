// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using ClangSharp.Interop;

namespace C2CS.Languages.C
{
    public record ClangSystemDataType : ClangCommon
    {
        public readonly ClangType UnderlyingType;

        internal ClangSystemDataType(
            string name,
            ClangCodeLocation codeLocation,
            ClangType underlyingType)
            : base(name, codeLocation)
        {
            UnderlyingType = underlyingType;
        }

        // Required for debugger string with records
        // ReSharper disable once RedundantOverriddenMember
        public override string ToString()
        {
            return base.ToString();
        }
    }
}
