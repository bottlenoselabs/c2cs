// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Languages.C
{
    public record CEnum : CNode
    {
        public readonly CType Type;
        public readonly CType IntegerType;
        public readonly ImmutableArray<CEnumValue> Values;

        internal CEnum(
            CCodeLocation codeLocation,
            CType type,
            CType integerType,
            ImmutableArray<CEnumValue> values)
            : base(CNodeKind.Enum, codeLocation)
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
