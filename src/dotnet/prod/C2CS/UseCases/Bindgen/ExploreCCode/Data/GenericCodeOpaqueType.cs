// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct GenericCodeOpaqueType : IComparable<GenericCodeOpaqueType>
    {
        public readonly string Name;
        public readonly GenericCodeInfo Info;
        public readonly GenericCodeType Type;

        public GenericCodeOpaqueType(
            string name,
            GenericCodeInfo info,
            GenericCodeType type)
        {
            Name = name;
            Info = info;
            Type = type;
        }

        public override string ToString()
        {
            return Name;
        }

        public int CompareTo(GenericCodeOpaqueType other)
        {
            return string.Compare(Name, other.Name, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            if (obj is GenericCodeOpaqueType other)
            {
                return Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public bool Equals(GenericCodeOpaqueType other)
        {
            return Name.Equals(other.Name, StringComparison.Ordinal);
        }

        public static bool operator ==(GenericCodeOpaqueType left, GenericCodeOpaqueType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GenericCodeOpaqueType left, GenericCodeOpaqueType right)
        {
            throw new NotImplementedException();
        }

        public static bool operator <(GenericCodeOpaqueType left, GenericCodeOpaqueType right)
        {
            throw new NotImplementedException();
        }

        public static bool operator >(GenericCodeOpaqueType left, GenericCodeOpaqueType right)
        {
            throw new NotImplementedException();
        }

        public static bool operator <=(GenericCodeOpaqueType left, GenericCodeOpaqueType right)
        {
            throw new NotImplementedException();
        }

        public static bool operator >=(GenericCodeOpaqueType left, GenericCodeOpaqueType right)
        {
            throw new NotImplementedException();
        }
    }
}
