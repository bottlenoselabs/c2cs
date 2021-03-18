// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

namespace C2CS.Languages.C
{
    public readonly struct ClangType
    {
        public readonly string Name;
        public readonly string OriginalName;
        public readonly int SizeOf;
        public readonly int AlignOf;
        public readonly bool IsReadOnly;

        internal ClangType(
            string name,
            string originalName,
            int sizeOf,
            int alignOf,
            bool isReadOnly)
        {
            Name = name;
            OriginalName = originalName;
            SizeOf = sizeOf;
            AlignOf = alignOf;
            IsReadOnly = isReadOnly;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
