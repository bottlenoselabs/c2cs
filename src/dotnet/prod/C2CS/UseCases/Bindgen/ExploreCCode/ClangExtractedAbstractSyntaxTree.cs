// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System.Collections.Immutable;
using ClangSharp.Interop;

namespace C2CS
{
    public struct ClangExtractedAbstractSyntaxTree
    {
        public ImmutableArray<CXCursor> Functions;

        public ImmutableArray<CXCursor> Records;

        public ImmutableArray<CXCursor> Enums;

        public ImmutableArray<CXCursor> OpaqueTypes;

        public ImmutableArray<CXCursor> ForwardTypes;

        public ImmutableArray<CXCursor> FunctionPointers;

        public ImmutableArray<CXCursor> SystemCursors;

        public ImmutableDictionary<CXCursor, string> NamesByCursor;
    }
}
