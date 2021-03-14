// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System.Collections.Immutable;

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct CStruct
    {
        public readonly string Name;
        public readonly CInfo Info;
        public readonly CType Type;
        public readonly ImmutableArray<CStructField> Fields;

        public CStruct(
            string name,
            CInfo location,
            CType type,
            ImmutableArray<CStructField> fields)
        {
            Name = name;
            Info = location;
            Type = type;
            Fields = fields;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
