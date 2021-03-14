// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct GenericCodeStruct : IComparable<GenericCodeStruct>
    {
        public readonly string Name;
        public readonly GenericCodeInfo Info;
        public readonly GenericCodeType Type;
        public readonly ImmutableArray<GenericCodeStructField> Fields;

        public GenericCodeStruct(
            string name,
            GenericCodeInfo location,
            GenericCodeType type,
            ImmutableArray<GenericCodeStructField> fields)
        {
            Name = name;
            Info = location;
            Type = type;
            Fields = fields;
        }

        public override string ToString()
        {
            return Name;
        }

        public int CompareTo(GenericCodeStruct other)
        {
            return string.Compare(Name, other.Name, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            if (obj is GenericCodeStruct other)
            {
                return Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public bool Equals(GenericCodeStruct other)
        {
            return Name.Equals(other.Name, StringComparison.Ordinal);
        }

        public static bool operator ==(GenericCodeStruct left, GenericCodeStruct right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GenericCodeStruct left, GenericCodeStruct right)
        {
            throw new NotImplementedException();
        }

        public static bool operator <(GenericCodeStruct left, GenericCodeStruct right)
        {
            throw new NotImplementedException();
        }

        public static bool operator >(GenericCodeStruct left, GenericCodeStruct right)
        {
            throw new NotImplementedException();
        }

        public static bool operator <=(GenericCodeStruct left, GenericCodeStruct right)
        {
            throw new NotImplementedException();
        }

        public static bool operator >=(GenericCodeStruct left, GenericCodeStruct right)
        {
            throw new NotImplementedException();
        }
    }
}
