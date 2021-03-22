// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

namespace C2CS.CSharp
{
    public record CSharpStructField : CSharpCommon
    {
        public readonly string OriginalName;
        public readonly CSharpType Type;
        public readonly int Offset;
        public readonly int Padding;

        public CSharpStructField(
            string name,
            string originalName,
            string codeLocationComment,
            CSharpType type,
            int offset,
            int padding)
            : base(name, codeLocationComment)
        {
            OriginalName = originalName;
            Type = type;
            Offset = offset;
            Padding = padding;
        }

        // Required for debugger string with records
        // ReSharper disable once RedundantOverriddenMember
        public override string ToString()
        {
            return base.ToString();
        }
    }
}
