// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System.Collections.Immutable;
using ClangSharp.Interop;

namespace C2CS
{
    public class ClangCodeExploreResult
    {
        public ImmutableArray<CXCursor> Functions { get; set; }

        public ImmutableArray<CXCursor> Records { get; set; }

        public ImmutableArray<CXCursor> Enums { get; set; }

        public ImmutableArray<CXCursor> OpaqueTypes { get; set; }

        public ImmutableArray<CXCursor> ExternalTypes { get; set; }
    }
}
