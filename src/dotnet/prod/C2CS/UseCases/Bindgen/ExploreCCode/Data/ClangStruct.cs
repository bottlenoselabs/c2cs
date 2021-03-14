// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System.Collections.Immutable;
using ClangSharp.Interop;

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct ClangStruct
    {
        public readonly string Name;
        public readonly CXType Type;
        public readonly CXCursor Cursor;
        public readonly ImmutableArray<ClangStructField> Fields;

        public ClangStruct(
            string name,
            CXType type,
            CXCursor cursor,
            ImmutableArray<ClangStructField> fields)
        {
            Name = name;
            Type = type;
            Cursor = cursor;
            Fields = fields;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
