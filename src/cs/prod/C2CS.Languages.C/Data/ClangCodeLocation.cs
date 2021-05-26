// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO;
using static libclang;

namespace C2CS.Languages.C
{
    public readonly struct ClangCodeLocation : IComparable<ClangCodeLocation>
    {
        public readonly string FileName;
        public readonly int FileLineNumber;
        public readonly int FileLineColumn;
        public readonly DateTime? DateTime;
        public readonly bool IsSystem;

        public unsafe ClangCodeLocation(CXCursor cursor, bool useFullPath = false, bool useDateTime = true)
        {
            if (cursor.kind == CXCursorKind.CXCursor_NoDeclFound || cursor.kind == CXCursorKind.CXCursor_FirstInvalid)
            {
                Console.WriteLine();
            }

            var location = clang_getCursorLocation(cursor);
            CXFile file;
            ulong lineNumber;
            ulong columnNumber;
            ulong offset;

            clang_getFileLocation(location, &file, &lineNumber, &columnNumber, &offset);

            var handle = (IntPtr) file.Data;
            if (handle == IntPtr.Zero)
            {
                FileName = string.Empty;
                FileLineNumber = default;
                DateTime = default;
                IsSystem = default;
            }

            var fileName = clang_getFileName(file);
            var fileNamePathC = clang_getCString(fileName);
            var fileNamePath = NativeRuntime.AllocateString(fileNamePathC);

            FileName = useFullPath ? fileNamePath : Path.GetFileName(fileNamePath);
            FileLineNumber = (int) lineNumber;
            FileLineColumn = (int) columnNumber;

            if (useDateTime)
            {
                var fileTime = clang_getFileTime(file);
                DateTime = new DateTime(1970, 1, 1).AddSeconds(fileTime);
            }
            else
            {
                DateTime = null;
            }

            IsSystem = cursor.IsSystem();
        }

        public override string ToString()
        {
            return DateTime != null ?
                $"{FileName}:{FileLineNumber}:{FileLineColumn} {DateTime}" :
                $"{FileName}:{FileLineNumber}:{FileLineColumn}";
        }

        public bool Equals(ClangCodeLocation other)
        {
            return FileName == other.FileName && FileLineNumber == other.FileLineNumber && DateTime.Equals(other.DateTime);
        }

        public override bool Equals(object? obj)
        {
            return obj is ClangCodeLocation other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FileName, FileLineNumber, DateTime);
        }

        public int CompareTo(ClangCodeLocation other)
        {
            // ReSharper disable once JoinDeclarationAndInitializer
            int result;

            result = string.Compare(FileName, other.FileName, StringComparison.Ordinal);
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (result != 0)
            {
                return result;
            }

            result = FileLineNumber.CompareTo(other.FileLineNumber);

            return result;
        }

        public static bool operator ==(ClangCodeLocation first, ClangCodeLocation second)
        {
            return first.Equals(second);
        }

        public static bool operator !=(ClangCodeLocation first, ClangCodeLocation second)
        {
            return !(first == second);
        }

        public static bool operator <(ClangCodeLocation first, ClangCodeLocation second)
        {
            throw new NotImplementedException();
        }

        public static bool operator >(ClangCodeLocation first, ClangCodeLocation second)
        {
            throw new NotImplementedException();
        }

        public static bool operator >=(ClangCodeLocation first, ClangCodeLocation second)
        {
            throw new NotImplementedException();
        }

        public static bool operator <=(ClangCodeLocation first, ClangCodeLocation second)
        {
            throw new NotImplementedException();
        }
    }
}
