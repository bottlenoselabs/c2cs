// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

namespace C2CS.CSharp
{
    public readonly struct CSharpStructField
    {
        public readonly string Name;
        public readonly CSharpType Type;
        public readonly int Offset;
        public readonly int Padding;

        public CSharpStructField(
            string name,
            CSharpType type,
            int offset,
            int padding)
        {
            Name = name;
            Type = type;
            Offset = offset;
            Padding = padding;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
