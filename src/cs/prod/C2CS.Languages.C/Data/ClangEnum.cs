// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Languages.C
{
    public record ClangEnum : ClangNode
    {
        public readonly ClangType Type;
        public readonly ClangType IntegerType;
        public readonly ImmutableArray<ClangEnumValue> Values;

        internal ClangEnum(
            ClangCodeLocation codeLocation,
            ClangType type,
            ClangType integerType,
            ImmutableArray<ClangEnumValue> values)
            : base(ClangNodeKind.Enum, codeLocation)
        {
            Type = type;
            IntegerType = integerType;
            Values = values;
        }

        public override string ToString()
        {
            return $"Enum '{Type.Name}': {IntegerType.Name} @ {CodeLocation.ToString()}";
        }
    }
}
