// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct GenericCodeFunctionPointer : IComparable<GenericCodeFunctionPointer>
    {
        public readonly string Name;
        public readonly GenericCodeInfo Info;
        public readonly GenericCodeType Type;

        public GenericCodeFunctionPointer(
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

        public int CompareTo(GenericCodeFunctionPointer other)
        {
            return string.Compare(Name, other.Name, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            if (obj is GenericCodeFunctionPointer other)
            {
                return Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public bool Equals(GenericCodeFunctionPointer other)
        {
            return Name.Equals(other.Name, StringComparison.Ordinal);
        }

        public static bool operator ==(GenericCodeFunctionPointer left, GenericCodeFunctionPointer right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GenericCodeFunctionPointer left, GenericCodeFunctionPointer right)
        {
            throw new NotImplementedException();
        }

        public static bool operator <(GenericCodeFunctionPointer left, GenericCodeFunctionPointer right)
        {
            throw new NotImplementedException();
        }

        public static bool operator >(GenericCodeFunctionPointer left, GenericCodeFunctionPointer right)
        {
            throw new NotImplementedException();
        }

        public static bool operator <=(GenericCodeFunctionPointer left, GenericCodeFunctionPointer right)
        {
            throw new NotImplementedException();
        }

        public static bool operator >=(GenericCodeFunctionPointer left, GenericCodeFunctionPointer right)
        {
            throw new NotImplementedException();
        }
    }
}
