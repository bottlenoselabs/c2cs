// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System.Collections.Immutable;

namespace C2CS.Languages.C
{
    public record ClangEnum : ClangCommon
    {
        public readonly ClangType IntegerType;
        public readonly ImmutableArray<ClangEnumValue> Values;

        internal ClangEnum(
            string name,
            ClangCodeLocation codeLocation,
            ClangType integerType,
            ImmutableArray<ClangEnumValue> values)
            : base(ClangKind.Enum, name, codeLocation)
        {
            IntegerType = integerType;
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
