// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System.Collections.Immutable;

namespace C2CS.CSharp
{
    public record CSharpEnum : CSharpCommon
    {
        public readonly CSharpType Type;
        public readonly ImmutableArray<CSharpEnumValue> Values;

        public CSharpEnum(
            string name,
            string originalCodeLocationComment,
            CSharpType type,
            ImmutableArray<CSharpEnumValue> values)
            : base(name, originalCodeLocationComment)
        {
            Type = type;
            Values = values;
        }

        // Required for debugger string with records
        // ReSharper disable once RedundantOverriddenMember
        public override string ToString()
        {
            return base.ToString();
        }
    }
}
