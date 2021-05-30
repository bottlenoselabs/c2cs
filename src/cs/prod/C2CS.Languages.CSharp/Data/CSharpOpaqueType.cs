// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.CSharp
{
    public record CSharpOpaqueType : CSharpNode
    {
        public readonly int SizeOf;
        public readonly int AlignOf;

        public CSharpOpaqueType(
            string name,
            string codeLocationComment,
            int sizeOf,
            int alignOf)
            : base(name, codeLocationComment)
        {
            SizeOf = sizeOf;
            AlignOf = alignOf;
        }

        // Required for debugger string with records
        // ReSharper disable once RedundantOverriddenMember
        public override string ToString()
        {
            return base.ToString();
        }
    }
}
