// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct CFunctionPointer
    {
        public readonly string Name;
        public readonly CLocation Location;
        public readonly CType Type;

        public CFunctionPointer(
            string name,
            CLocation location,
            CType type)
        {
            Name = name;
            Location = location;
            Type = type;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
