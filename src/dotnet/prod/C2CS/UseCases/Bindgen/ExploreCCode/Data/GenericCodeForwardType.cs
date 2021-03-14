// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct GenericCodeForwardType : IComparable<GenericCodeForwardType>
    {
        public readonly string Name;
        public readonly GenericCodeInfo Info;
        public readonly GenericCodeType UnderlyingType;

        public GenericCodeForwardType(
            string name,
            GenericCodeInfo info,
            GenericCodeType underlyingType)
        {
            Name = name;
            Info = info;
            UnderlyingType = underlyingType;
        }

        public override string ToString()
        {
            return Name;
        }

        public int CompareTo(GenericCodeForwardType other)
        {
            return string.Compare(Name, other.Name, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            if (obj is GenericCodeForwardType other)
            {
                return Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public bool Equals(GenericCodeForwardType other)
        {
            return Name.Equals(other.Name, StringComparison.Ordinal);
        }

        public static bool operator ==(GenericCodeForwardType left, GenericCodeForwardType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GenericCodeForwardType left, GenericCodeForwardType right)
        {
            throw new NotImplementedException();
        }

        public static bool operator <(GenericCodeForwardType left, GenericCodeForwardType right)
        {
            throw new NotImplementedException();
        }

        public static bool operator >(GenericCodeForwardType left, GenericCodeForwardType right)
        {
            throw new NotImplementedException();
        }

        public static bool operator <=(GenericCodeForwardType left, GenericCodeForwardType right)
        {
            throw new NotImplementedException();
        }

        public static bool operator >=(GenericCodeForwardType left, GenericCodeForwardType right)
        {
            throw new NotImplementedException();
        }
    }
}
