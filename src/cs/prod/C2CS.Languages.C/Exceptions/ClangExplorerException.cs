// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

namespace C2CS.Languages.C
{
    internal class ClangExplorerException : Exception
    {
        public ClangExplorerException(string message)
        {
            Message = message;
        }

        public ClangExplorerException(libclang.CXCursor cursor)
        {
            var cursorName = cursor.GetName();
            var (filePath, lineNumber, lineColumn) = cursor.GetLocation();
            Message = $@"
Unexpected error while exploring Clang header: {cursorName} @ {filePath}:{lineNumber}:{lineColumn}
".Trim();
        }

        public ClangExplorerException(libclang.CXType type, libclang.CXCursor cursor)
            : this(cursor)
        {
            var typeName = type.GetName();
            var (filePath, lineNumber, lineColumn) = cursor.GetLocation();
            Message = $@"
Unexpected error while exploring Clang header: {typeName} @ {filePath}:{lineNumber}:{lineColumn}
".Trim();
        }

        public override string Message { get; }
    }
}
