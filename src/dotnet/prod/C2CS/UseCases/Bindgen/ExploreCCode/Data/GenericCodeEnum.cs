// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Immutable;

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct GenericCodeEnum : IComparable<GenericCodeEnum>
    {
        public readonly string Name;
        public readonly GenericCodeInfo Info;
        public readonly GenericCodeType Type;
        public readonly ImmutableArray<GenericCodeValue> Values;

        public GenericCodeEnum(
            string name,
            GenericCodeInfo info,
            GenericCodeType type,
            ImmutableArray<GenericCodeValue> values)
        {
            Name = name;
            Info = info;
            Type = type;
            Values = values;
        }

        public override string ToString()
        {
            return Name;
        }

        public int CompareTo(GenericCodeEnum other)
        {
            return string.Compare(Name, other.Name, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            if (obj is GenericCodeEnum other)
            {
                return Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public bool Equals(GenericCodeEnum other)
        {
            return Name.Equals(other.Name, StringComparison.Ordinal);
        }

        public static bool operator ==(GenericCodeEnum left, GenericCodeEnum right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GenericCodeEnum left, GenericCodeEnum right)
        {
            throw new NotImplementedException();
        }

        public static bool operator <(GenericCodeEnum left, GenericCodeEnum right)
        {
            throw new NotImplementedException();
        }

        public static bool operator >(GenericCodeEnum left, GenericCodeEnum right)
        {
            throw new NotImplementedException();
        }

        public static bool operator <=(GenericCodeEnum left, GenericCodeEnum right)
        {
            throw new NotImplementedException();
        }

        public static bool operator >=(GenericCodeEnum left, GenericCodeEnum right)
        {
            throw new NotImplementedException();
        }
    }
}
