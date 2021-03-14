// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct GenericCodeForwardType
    {
        public readonly string Name;
        public readonly GenericCodeInfo Info;
        public readonly GenericCodeType Type;
        public readonly GenericCodeType UnderlyingType;

        public GenericCodeForwardType(
            string name,
            GenericCodeInfo info,
            GenericCodeType type,
            GenericCodeType underlyingType)
        {
            Name = name;
            Info = info;
            Type = type;
            UnderlyingType = underlyingType;
        }
    }
}
