// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

namespace C2CS.CSharp
{
    public readonly struct CSharpFunctionParameter
    {
        public readonly string Name;
        public readonly CSharpType Type;
        public readonly bool IsReadOnly;

        public CSharpFunctionParameter(
            string name,
            CSharpType type,
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
