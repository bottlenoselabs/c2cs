// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using ClangSharp.Interop;

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct ClangRecord
    {
        public readonly string Name;
        public readonly CXType Type;
        public readonly CXCursor Cursor;
        public readonly CXCursor UnderlyingCursor;

        public ClangRecord(
            string name,
            CXType type,
            CXCursor cursor,
            CXCursor underlyingCursor)
        {
            Name = name;
            Type = type;
            Cursor = cursor;
            UnderlyingCursor = underlyingCursor;
        }
    }
}