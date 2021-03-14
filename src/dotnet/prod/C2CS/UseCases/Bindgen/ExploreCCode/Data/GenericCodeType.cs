// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct GenericCodeType
    {
        public readonly string Name;
        public readonly string OriginalName;
        public readonly int ArraySize;
        public readonly GenericCodeLayout Layout;

        public bool IsArray => ArraySize > 0;

        public GenericCodeType(
            string name,
            string originalName,
            int arraySize,
            GenericCodeLayout layout)
        {
            Name = name;
            OriginalName = originalName;
            ArraySize = arraySize;
            Layout = layout;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
