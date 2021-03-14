// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct CForwardType
    {
        public readonly string Name;
        public readonly CInfo Info;
        public readonly CType Type;
        public readonly CType UnderlyingType;

        public CForwardType(
            string name,
            CInfo info,
            CType type,
            CType underlyingType)
        {
            Name = name;
            Info = info;
            Type = type;
            UnderlyingType = underlyingType;
        }
    }
}
