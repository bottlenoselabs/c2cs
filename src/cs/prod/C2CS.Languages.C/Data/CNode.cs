// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

namespace C2CS.Languages.C
{
    public record CNode : IComparable<CNode>
    {
        public readonly CNodeKind Kind;
        public readonly CCodeLocation CodeLocation;

        public CNode(
            CNodeKind kind,
            CCodeLocation codeLocation)
        {
            Kind = kind;
            CodeLocation = codeLocation;
        }

        public int CompareTo(CNode? other)
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

        public static bool operator <(CNode first, CNode second)
        {
            throw new NotImplementedException();
        }

        public static bool operator >(CNode first, CNode second)
        {
            throw new NotImplementedException();
        }

        public static bool operator >=(CNode first, CNode second)
        {
            throw new NotImplementedException();
        }

        public static bool operator <=(CNode first, CNode second)
        {
            throw new NotImplementedException();
        }
    }
}
