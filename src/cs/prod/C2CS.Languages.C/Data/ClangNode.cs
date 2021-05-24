// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

namespace C2CS.Languages.C
{
    public record ClangNode : IComparable<ClangNode>
    {
        public readonly ClangNodeKind Kind;
        public readonly ClangCodeLocation CodeLocation;

        public ClangNode(
            ClangNodeKind kind,
            ClangCodeLocation codeLocation)
        {
            Kind = kind;
            CodeLocation = codeLocation;
        }

        public int CompareTo(ClangNode? other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (ReferenceEquals(null, other))
            {
                return 1;
            }

            var result = CodeLocation.CompareTo(other.CodeLocation);
            return result;
        }

        public static bool operator <(ClangNode first, ClangNode second)
        {
            throw new NotImplementedException();
        }

        public static bool operator >(ClangNode first, ClangNode second)
        {
            throw new NotImplementedException();
        }

        public static bool operator >=(ClangNode first, ClangNode second)
        {
            throw new NotImplementedException();
        }

        public static bool operator <=(ClangNode first, ClangNode second)
        {
            throw new NotImplementedException();
        }
    }
}
