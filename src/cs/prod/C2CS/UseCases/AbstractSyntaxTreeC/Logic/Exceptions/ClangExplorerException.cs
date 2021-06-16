// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using static libclang;

namespace C2CS.UseCases.AbstractSyntaxTreeC
{
    internal class ClangExplorerException : Exception
    {
        public ClangExplorerException(string message)
        {
            Message = message;
        }

        public ClangExplorerException(CXCursor cursor)
        {
            var cursorName = cursor.Name();
            var location = cursor.FileLocation();
            Message = $@"
Unexpected error while exploring Clang header: {cursorName} @ {location.Path}:{location.LineNumber}:{location.LineColumn}
".Trim();
        }

        public ClangExplorerException(CXType type, CXCursor cursor)
            : this(cursor)
        {
            var typeName = type.Name();
            var location = cursor.FileLocation();
            Message = $@"
Unexpected error while exploring Clang header: {typeName} @ {location.Path}:{location.LineNumber}:{location.LineColumn}
".Trim();
        }

        public override string Message { get; }
    }
}
