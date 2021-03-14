// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct GenericCodeFunctionParameter
    {
        public readonly string Name;
        public readonly GenericCodeType Type;
        public readonly bool IsReadOnly;

        public GenericCodeFunctionParameter(
            string name,
            GenericCodeType type,
            bool isReadOnly)
        {
            Name = name;
            Type = type;
            IsReadOnly = isReadOnly;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
