// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System.Collections.Immutable;
using ClangSharp.Interop;

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct ClangEnum
    {
        public readonly string Name;
        public readonly CXCursor Cursor;
        public readonly CXType Type;
        public readonly ImmutableArray<ClangEnumValue> Values;

        public ClangEnum(
            string name,
            CXCursor cursor,
            CXType type,
            ImmutableArray<ClangEnumValue> values)
        {
            Name = name;
            Cursor = cursor;
            Type = type;
            Values = values;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
