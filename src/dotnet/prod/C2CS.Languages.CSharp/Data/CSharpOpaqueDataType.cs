// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

namespace C2CS.CSharp
{
    public readonly struct CSharpOpaqueDataType
    {
        public readonly string Name;
        public readonly string OriginalCodeLocationComment;
        public readonly CSharpType Type;

        public CSharpOpaqueDataType(
            string name,
            string originalCodeLocationComment,
            CSharpType type)
        {
            Name = name;
            OriginalCodeLocationComment = originalCodeLocationComment;
            Type = type;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
