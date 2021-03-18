// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;

namespace C2CS.CSharp
{
    public readonly struct CSharpSystemDataType
    {
        public readonly string Name;
        public readonly string OriginalCodeLocationComment;
        public readonly CSharpType UnderlyingType;

        public CSharpSystemDataType(
            string name,
            string originalCodeLocationComment,
            CSharpType underlyingType)
        {
            Name = name;
            OriginalCodeLocationComment = originalCodeLocationComment;
            UnderlyingType = underlyingType;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
