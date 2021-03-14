// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using ClangSharp.Interop;

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct ClangFunctionParameter
    {
        public readonly string Name;
        public readonly CXType Type;

        public ClangFunctionParameter(
            string name,
            CXType type)
        {
            Name = name;
            Type = type;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
