// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;

namespace C2CS.Languages.C
{
    public record ClangCommon : IComparable<ClangCommon>
    {
        public readonly ClangKind Kind;
        public readonly string Name;
        public readonly ClangCodeLocation CodeLocation;

        public ClangCommon(
            ClangKind kind,
            string name,
            ClangCodeLocation codeLocation)
        {
            Kind = kind;
            Name = name;
            CodeLocation = codeLocation;
        }

        public override string ToString()
        {
            return $"{Name}:{Kind} <{CodeLocation.ToString()}>";
        }

        public int CompareTo(ClangCommon? other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (ReferenceEquals(null, other))
            {
                return 1;
            }

            // ReSharper disable once JoinDeclarationAndInitializer
            int result;

            result = CodeLocation.CompareTo(other.CodeLocation);
            if (result != 0)
            {
                return result;
            }

            result = string.Compare(Name, other.Name, StringComparison.Ordinal);

            return result;
        }

        public static bool operator <(ClangCommon first, ClangCommon second)
        {
            throw new NotImplementedException();
        }

        public static bool operator >(ClangCommon first, ClangCommon second)
        {
            throw new NotImplementedException();
        }

        public static bool operator >=(ClangCommon first, ClangCommon second)
        {
            throw new NotImplementedException();
        }

        public static bool operator <=(ClangCommon first, ClangCommon second)
        {
            throw new NotImplementedException();
        }
    }
}
