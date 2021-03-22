// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

namespace C2CS.CSharp
{
    public readonly struct CSharpType
    {
        public readonly string Name;
        public readonly string OriginalName;
        public readonly int SizeOf;
        public readonly int AlignOf;
        public readonly int FixedBufferSize;
        public readonly bool FixedBufferIsWrapped;

        public bool IsArray => FixedBufferSize > 0;

        public CSharpType(
            string name,
            string originalName,
            int sizeOf,
            int alignOf,
            int fixedBufferSize,
            bool fixedBufferIsWrapped)
        {
            Name = name;
            OriginalName = originalName;
            SizeOf = sizeOf;
            AlignOf = alignOf;
            FixedBufferSize = fixedBufferSize;
            FixedBufferIsWrapped = fixedBufferIsWrapped;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
