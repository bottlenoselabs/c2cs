// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

namespace C2CS.CSharp
{
    public readonly struct CSharpType
    {
        public readonly string Name;
        public readonly string OriginalName;
        public readonly int ArraySize;
        public readonly int SizeOf;
        public readonly int AlignOf;

        public bool IsArray => ArraySize > 0;

        public CSharpType(
            string name,
            string originalName,
            int arraySize,
            int sizeOf,
            int alignOf)
        {
            Name = name;
            OriginalName = originalName;
            ArraySize = arraySize;
            SizeOf = sizeOf;
            AlignOf = alignOf;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
