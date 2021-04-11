// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

namespace C2CS.Languages.C
{
    public readonly struct ClangCodeLocation : IComparable<ClangCodeLocation>
    {
        public readonly string FileName;
        public readonly int FileLineNumber;
        public readonly DateTime DateTime;

        internal ClangCodeLocation(
            string fileName,
            int fileLineNumber,
            DateTime dateTime)
        {
            FileName = fileName;
            FileLineNumber = fileLineNumber;
            DateTime = dateTime;
        }

        public override string ToString()
        {
            return $"{FileName}:{FileLineNumber} {DateTime}";
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
