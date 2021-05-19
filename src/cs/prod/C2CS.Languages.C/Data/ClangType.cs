// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Languages.C
{
    public readonly struct ClangType
    {
        public readonly string Name;
        public readonly string OriginalName;
        public readonly int SizeOf;
        public readonly int AlignOf;
        public readonly int ArraySize;
        public readonly bool IsSystemType;

        internal ClangType(
            string name,
            string originalName,
            int sizeOf,
            int alignOf,
            int arraySize,
            bool isSystemType)
        {
            Name = name;
            OriginalName = originalName;
            SizeOf = sizeOf;
            AlignOf = alignOf;
            ArraySize = arraySize;
            IsSystemType = isSystemType;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
