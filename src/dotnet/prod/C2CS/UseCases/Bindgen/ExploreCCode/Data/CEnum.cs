// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System.Collections.Immutable;

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct CEnum
    {
        public readonly string Name;
        public readonly CInfo Info;
        public readonly CType Type;
        public readonly ImmutableArray<CEnumValue> Values;

        public CEnum(
            string name,
            CInfo info,
            CType type,
            ImmutableArray<CEnumValue> values)
        {
            Name = name;
            Info = info;
            Type = type;
            Values = values;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}