// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct CStructField
    {
        public readonly string Name;
        public readonly CType Type;
        public readonly int Offset;
        public readonly int Padding;

        public CStructField(
            string name,
            CType type,
            int offset)
        {
            Name = name;
            Type = type;
            Offset = offset;
            Padding = 0;
        }

        public CStructField(CStructField previous, int padding)
        {
            Name = previous.Name;
            Type = previous.Type;
            Offset = previous.Offset;
            Padding = padding;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
