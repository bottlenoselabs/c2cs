// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System.Collections.Immutable;

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct GenericCodeEnum
    {
        public readonly string Name;
        public readonly GenericCodeInfo Info;
        public readonly GenericCodeType Type;
        public readonly ImmutableArray<GenericCodeValue> Values;

        public GenericCodeEnum(
            string name,
            GenericCodeInfo info,
            GenericCodeType type,
            ImmutableArray<GenericCodeValue> values)
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
