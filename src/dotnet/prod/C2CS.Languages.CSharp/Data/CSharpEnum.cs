// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System.Collections.Immutable;

namespace C2CS.CSharp
{
    public readonly struct CSharpEnum
    {
        public readonly string Name;
        public readonly string OriginalCodeLocationComment;
        public readonly CSharpType Type;
        public readonly ImmutableArray<CSharpEnumValue> Values;

        public CSharpEnum(
            string name,
            string originalCodeLocationComment,
            CSharpType type,
            ImmutableArray<CSharpEnumValue> values)
        {
            Name = name;
            OriginalCodeLocationComment = originalCodeLocationComment;
            Type = type;
            Values = values;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
