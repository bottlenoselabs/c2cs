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
        public readonly DateTime DateTime;
        public readonly bool IsSystem;

        public unsafe ClangCodeLocation(CXCursor cursor)
        {
            var location = clang_getCursorLocation(cursor);
            CXFile file;
            uint lineNumber;
            uint columnNumber;
            uint offset;
            clang_getFileLocation(location, &file, &lineNumber, &columnNumber, &offset);

            var handle = (IntPtr) file.Pointer;
            if (handle == IntPtr.Zero)
            {
                FileName = string.Empty;
                FileLineNumber = default;
                DateTime = default;
                IsSystem = default;
            }

            var fileName = clang_getFileName(file);
            var cString = clang_getCString(fileName);
            var fileNamePath = NativeRuntime.MapString(cString);
            FileName = Path.GetFileName(fileNamePath);
            FileLineNumber = (int) lineNumber;
            FileLineColumn = (int) columnNumber;
            var fileTime = clang_getFileTime(file);
            DateTime = new DateTime(1970, 1, 1).AddSeconds(fileTime);
            IsSystem = cursor.IsSystem();
        }

        public override string ToString()
        {
            return $"{FileName}:{FileLineNumber}:{FileLineColumn} {DateTime}";
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
